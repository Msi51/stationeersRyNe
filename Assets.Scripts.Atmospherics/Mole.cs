using System;
using Assets.Scripts.Networking;
using Assets.Scripts.UI;
using Assets.Scripts.Util;

namespace Assets.Scripts.Atmospherics;

public struct Mole
{
	public bool ReadOnly;

	private MoleEnergy _energyCached;

	private MoleQuantity _quantityCached;

	private MoleEnergy _energy;

	private MoleQuantity _quantity;

	public bool IsCachable;

	public const double FUSION_TO_VAPORIZATION_LATENT_HEAT_DENOMINATOR = 5.0;

	private static readonly MoleQuantity LowStateChangeQuantityBound = new MoleQuantity(0.1);

	private MoleEnergy _lastEnergyDirtied;

	private MoleQuantity _lastQuantityDirtied;

	public const double OVERFLOW_PENALTY_RATIO = 0.9;

	public Chemistry.GasType Type { get; }

	public bool QuantityDirty { get; private set; }

	public bool EnergyDirty { get; private set; }

	public bool IsValid { get; }

	public double LatentHeatOfFusion => LatentHeatOfVaporization() / 5.0;

	public string DisplayName => Localization.GetName(Type);

	public HeatCapacity HeatCapacity => new HeatCapacity(SpecificHeat(), Quantity);

	public MoleEnergy Energy
	{
		get
		{
			if (IsCachable && (!AtmosphereHelper.CanWriteAccess || NetworkManager.IsClient))
			{
				return _energyCached;
			}
			return _energy;
		}
		set
		{
			if (value.IsNaN())
			{
				MoleHelper.LogMessage("mole set Energy NaN").Forget();
			}
			else if (value.IsInfinity())
			{
				MoleHelper.LogMessage("mole set Energy Infinity").Forget();
			}
			else if (!value.IsDenormal() && !ReadOnly)
			{
				_energy = value;
			}
		}
	}

	public MoleQuantity Quantity
	{
		get
		{
			if (IsCachable && (!AtmosphereHelper.CanWriteAccess || NetworkManager.IsClient))
			{
				return _quantityCached;
			}
			return _quantity;
		}
		set
		{
			if (!ReadOnly)
			{
				_quantity = value;
			}
		}
	}

	public TemperatureKelvin Temperature
	{
		get
		{
			if (!IsValid)
			{
				return TemperatureKelvin.Zero;
			}
			return IdealGas.Temperature(Energy, HeatCapacity);
		}
	}

	public VolumeLitres Volume
	{
		get
		{
			double num = Quantity.ToDouble();
			if (!(num <= 0.0))
			{
				return MolarVolume() * num;
			}
			return VolumeLitres.Zero;
		}
	}

	public double EnthalpyMultiplier => Type switch
	{
		Chemistry.GasType.Oxygen => 1.0, 
		Chemistry.GasType.LiquidOxygen => 1.0, 
		Chemistry.GasType.NitrousOxide => 2.0, 
		Chemistry.GasType.LiquidNitrousOxide => 2.0, 
		Chemistry.GasType.Ozone => 2.0, 
		Chemistry.GasType.LiquidOzone => 2.0, 
		_ => 1.0, 
	};

	public TemperatureKelvin AutoIgnitionOffset => Type switch
	{
		Chemistry.GasType.Oxygen => TemperatureKelvin.Zero, 
		Chemistry.GasType.LiquidOxygen => TemperatureKelvin.Zero, 
		Chemistry.GasType.NitrousOxide => Chemistry.AutoIgnitionOffsetNitrogenDioxide, 
		Chemistry.GasType.LiquidNitrousOxide => Chemistry.AutoIgnitionOffsetNitrogenDioxide, 
		Chemistry.GasType.Ozone => Chemistry.AutoIgnitionOffsetOzone, 
		Chemistry.GasType.LiquidOzone => Chemistry.AutoIgnitionOffsetOzone, 
		_ => TemperatureKelvin.Zero, 
	};

	public TemperatureKelvin AutoIgnitionTemperature => Type switch
	{
		Chemistry.GasType.Methane => Chemistry.AutoIgnitionMethane, 
		Chemistry.GasType.LiquidMethane => Chemistry.AutoIgnitionMethane, 
		Chemistry.GasType.Hydrogen => Chemistry.AutoIgnitionHydrogen, 
		Chemistry.GasType.LiquidHydrogen => Chemistry.AutoIgnitionHydrogen, 
		Chemistry.GasType.LiquidAlcohol => Chemistry.AutoIgnitionAlcohol, 
		Chemistry.GasType.Hydrazine => Chemistry.AutoIgnitionHydrazine, 
		Chemistry.GasType.LiquidHydrazine => Chemistry.AutoIgnitionHydrazine, 
		_ => throw new NotImplementedException($"PureIce Prefab not implemented for {Type}"), 
	};

	public int PureIcePrefabHash => Type switch
	{
		Chemistry.GasType.Oxygen => PrefabHashmap.ItemPureIceOxygen, 
		Chemistry.GasType.LiquidOxygen => PrefabHashmap.ItemPureIceLiquidOxygen, 
		Chemistry.GasType.Nitrogen => PrefabHashmap.ItemPureIceNitrogen, 
		Chemistry.GasType.LiquidNitrogen => PrefabHashmap.ItemPureIceLiquidNitrogen, 
		Chemistry.GasType.Pollutant => PrefabHashmap.ItemPureIcePollutant, 
		Chemistry.GasType.LiquidPollutant => PrefabHashmap.ItemPureIceLiquidPollutant, 
		Chemistry.GasType.Steam => PrefabHashmap.ItemPureIceSteam, 
		Chemistry.GasType.Water => PrefabHashmap.ItemPureIce, 
		Chemistry.GasType.PollutedWater => PrefabHashmap.ItemPureIcePollutedWater, 
		Chemistry.GasType.CarbonDioxide => PrefabHashmap.ItemPureIceCarbonDioxide, 
		Chemistry.GasType.LiquidCarbonDioxide => PrefabHashmap.ItemPureIceLiquidCarbonDioxide, 
		Chemistry.GasType.Methane => PrefabHashmap.ItemPureIceVolatiles, 
		Chemistry.GasType.LiquidMethane => PrefabHashmap.ItemPureIceLiquidVolatiles, 
		Chemistry.GasType.NitrousOxide => PrefabHashmap.ItemPureIceNitrous, 
		Chemistry.GasType.LiquidNitrousOxide => PrefabHashmap.ItemPureIceLiquidNitrous, 
		Chemistry.GasType.Hydrogen => PrefabHashmap.ItemPureIceHydrogen, 
		Chemistry.GasType.LiquidHydrogen => PrefabHashmap.ItemPureIceLiquidHydrogen, 
		Chemistry.GasType.Hydrazine => PrefabHashmap.ItemPureIceHydrazine, 
		Chemistry.GasType.LiquidHydrazine => PrefabHashmap.ItemPureIceLiquidHydrazine, 
		Chemistry.GasType.LiquidAlcohol => PrefabHashmap.ItemPureIceLiquidAlcohol, 
		Chemistry.GasType.LiquidSodiumChloride => PrefabHashmap.ItemPureIceLiquidSodiumChloride, 
		Chemistry.GasType.Silanol => PrefabHashmap.ItemPureIceSilanol, 
		Chemistry.GasType.LiquidSilanol => PrefabHashmap.ItemPureIceLiquidSilanol, 
		Chemistry.GasType.HydrochloricAcid => PrefabHashmap.ItemPureIceHydrochloricAcid, 
		Chemistry.GasType.LiquidHydrochloricAcid => PrefabHashmap.ItemPureIceLiquidHydrochloricAcid, 
		Chemistry.GasType.Ozone => PrefabHashmap.ItemPureIceOzone, 
		Chemistry.GasType.LiquidOzone => PrefabHashmap.ItemPureIceLiquidOzone, 
		_ => throw new NotImplementedException($"PureIce Prefab not implemented for {Type}"), 
	};

	public void Undirty()
	{
		QuantityDirty = false;
		EnergyDirty = false;
	}

	public TemperatureKelvin FreezingTemperature()
	{
		return FreezingTemperature(Type);
	}

	public static TemperatureKelvin FreezingTemperature(Chemistry.GasType type)
	{
		return type switch
		{
			Chemistry.GasType.Undefined => TemperatureKelvin.Zero, 
			Chemistry.GasType.Oxygen => Chemistry.TRIPLE_POINT_TEMPERATURE_OXYGEN_K, 
			Chemistry.GasType.Nitrogen => Chemistry.FREEZING_TEMPERATURE_NITROGEN_K, 
			Chemistry.GasType.CarbonDioxide => Chemistry.TRIPLE_POINT_TEMPERATURE_CARBON_DIOXIDE_K, 
			Chemistry.GasType.Methane => Chemistry.TRIPLE_POINT_TEMPERATURE_METHANE_K, 
			Chemistry.GasType.Pollutant => Chemistry.TRIPLE_POINT_TEMPERATURE_SULFUR_DIOXIDE_K, 
			Chemistry.GasType.Water => Chemistry.TRIPLE_POINT_TEMPERATURE_WATER_K, 
			Chemistry.GasType.PollutedWater => Chemistry.TRIPLE_POINT_TEMPERATURE_POLLUTED_WATER_K, 
			Chemistry.GasType.NitrousOxide => Chemistry.TRIPLE_POINT_TEMPERATURE_NITROGEN_DIOXIDE_K, 
			Chemistry.GasType.LiquidNitrogen => Chemistry.FREEZING_TEMPERATURE_NITROGEN_K, 
			Chemistry.GasType.LiquidOxygen => Chemistry.TRIPLE_POINT_TEMPERATURE_OXYGEN_K, 
			Chemistry.GasType.LiquidMethane => Chemistry.TRIPLE_POINT_TEMPERATURE_METHANE_K, 
			Chemistry.GasType.Steam => Chemistry.TRIPLE_POINT_TEMPERATURE_WATER_K, 
			Chemistry.GasType.LiquidCarbonDioxide => Chemistry.TRIPLE_POINT_TEMPERATURE_CARBON_DIOXIDE_K, 
			Chemistry.GasType.LiquidPollutant => Chemistry.TRIPLE_POINT_TEMPERATURE_SULFUR_DIOXIDE_K, 
			Chemistry.GasType.LiquidNitrousOxide => Chemistry.TRIPLE_POINT_TEMPERATURE_NITROGEN_DIOXIDE_K, 
			Chemistry.GasType.Hydrogen => Chemistry.TRIPLE_POINT_TEMPERATURE_HYDROGEN_K, 
			Chemistry.GasType.LiquidHydrogen => Chemistry.TRIPLE_POINT_TEMPERATURE_HYDROGEN_K, 
			Chemistry.GasType.Hydrazine => Chemistry.FREEZING_POINT_HYDRAZINE, 
			Chemistry.GasType.LiquidHydrazine => Chemistry.FREEZING_POINT_HYDRAZINE, 
			Chemistry.GasType.LiquidAlcohol => Chemistry.FREEZING_POINT_ETHANOL, 
			Chemistry.GasType.Helium => Chemistry.FREEZING_POINT_HELIUM, 
			Chemistry.GasType.LiquidSodiumChloride => Chemistry.FREEZING_POINT_SODIUMCHLORIDE, 
			Chemistry.GasType.Silanol => Chemistry.FREEZING_POINT_SILANOL, 
			Chemistry.GasType.LiquidSilanol => Chemistry.FREEZING_POINT_SILANOL, 
			Chemistry.GasType.HydrochloricAcid => Chemistry.FREEZING_POINT_HYDROCHLORIC_ACID, 
			Chemistry.GasType.LiquidHydrochloricAcid => Chemistry.FREEZING_POINT_HYDROCHLORIC_ACID, 
			Chemistry.GasType.Ozone => Chemistry.FREEZING_POINT_OZONE, 
			Chemistry.GasType.LiquidOzone => Chemistry.FREEZING_POINT_OZONE, 
			_ => TemperatureKelvin.Zero, 
		};
	}

	public TemperatureKelvin BoilingPoint()
	{
		return BoilingPoint(Type);
	}

	public static TemperatureKelvin BoilingPoint(Chemistry.GasType type)
	{
		return MoleHelper.EvaporationTemperature(type, Chemistry.OneAtmosphere);
	}

	public PressurekPa MinLiquidPressure()
	{
		return MinLiquidPressure(Type);
	}

	public static PressurekPa MinLiquidPressure(Chemistry.GasType type)
	{
		return RocketMath.Max(val2: new PressurekPa(type switch
		{
			Chemistry.GasType.Undefined => 0.0, 
			Chemistry.GasType.Oxygen => 6.300000190734863, 
			Chemistry.GasType.Nitrogen => 6.300000190734863, 
			Chemistry.GasType.CarbonDioxide => 517.0, 
			Chemistry.GasType.Methane => 6.300000190734863, 
			Chemistry.GasType.Pollutant => 1800.0, 
			Chemistry.GasType.Water => 6.300000190734863, 
			Chemistry.GasType.PollutedWater => 6.300000190734863, 
			Chemistry.GasType.NitrousOxide => 800.0, 
			Chemistry.GasType.LiquidNitrogen => 6.300000190734863, 
			Chemistry.GasType.LiquidOxygen => 6.300000190734863, 
			Chemistry.GasType.LiquidMethane => 6.300000190734863, 
			Chemistry.GasType.Steam => 6.300000190734863, 
			Chemistry.GasType.LiquidCarbonDioxide => 517.0, 
			Chemistry.GasType.LiquidPollutant => 1800.0, 
			Chemistry.GasType.LiquidNitrousOxide => 800.0, 
			Chemistry.GasType.Hydrogen => 6.300000190734863, 
			Chemistry.GasType.LiquidHydrogen => 6.300000190734863, 
			Chemistry.GasType.Hydrazine => 6.3, 
			Chemistry.GasType.LiquidHydrazine => 6.3, 
			Chemistry.GasType.LiquidAlcohol => 6.3, 
			Chemistry.GasType.Helium => 0.0, 
			Chemistry.GasType.LiquidSodiumChloride => 6.3, 
			Chemistry.GasType.Silanol => 516.0, 
			Chemistry.GasType.LiquidSilanol => 516.0, 
			Chemistry.GasType.HydrochloricAcid => 6.3, 
			Chemistry.GasType.LiquidHydrochloricAcid => 6.3, 
			Chemistry.GasType.Ozone => 250.0, 
			Chemistry.GasType.LiquidOzone => 250.0, 
			_ => 0.0, 
		}), val1: Chemistry.ArmstrongLimit);
	}

	public TemperatureKelvin MaxLiquidTemperature()
	{
		return MaxLiquidTemperature(Type);
	}

	public static TemperatureKelvin MaxLiquidTemperature(Chemistry.GasType type)
	{
		return type switch
		{
			Chemistry.GasType.Undefined => TemperatureKelvin.Zero, 
			Chemistry.GasType.Oxygen => Chemistry.CRITICAL_TEMPERATURE_OXYGEN_K, 
			Chemistry.GasType.Nitrogen => Chemistry.CRITICAL_TEMPERATURE_NITROGEN_K, 
			Chemistry.GasType.CarbonDioxide => Chemistry.CRITICAL_TEMPERATURE_CARBON_DIOXIDE_K, 
			Chemistry.GasType.Methane => Chemistry.CRITICAL_TEMPERATURE_METHANE_K, 
			Chemistry.GasType.Pollutant => Chemistry.CRITICAL_TEMPERATURE_SULFUR_DIOXIDE_K, 
			Chemistry.GasType.Water => Chemistry.CRITICAL_TEMPERATURE_WATER_K, 
			Chemistry.GasType.PollutedWater => Chemistry.CRITICAL_TEMPERATURE_POLLUTED_WATER_K, 
			Chemistry.GasType.NitrousOxide => Chemistry.CRITICAL_TEMPERATURE_NITROGEN_DIOXIDE_K, 
			Chemistry.GasType.LiquidNitrogen => Chemistry.CRITICAL_TEMPERATURE_NITROGEN_K, 
			Chemistry.GasType.LiquidOxygen => Chemistry.CRITICAL_TEMPERATURE_OXYGEN_K, 
			Chemistry.GasType.LiquidMethane => Chemistry.CRITICAL_TEMPERATURE_METHANE_K, 
			Chemistry.GasType.Steam => Chemistry.CRITICAL_TEMPERATURE_WATER_K, 
			Chemistry.GasType.LiquidCarbonDioxide => Chemistry.CRITICAL_TEMPERATURE_CARBON_DIOXIDE_K, 
			Chemistry.GasType.LiquidPollutant => Chemistry.CRITICAL_TEMPERATURE_SULFUR_DIOXIDE_K, 
			Chemistry.GasType.LiquidNitrousOxide => Chemistry.CRITICAL_TEMPERATURE_NITROGEN_DIOXIDE_K, 
			Chemistry.GasType.Hydrogen => Chemistry.CRITICAL_TEMPERATURE_HYDROGEN_K, 
			Chemistry.GasType.LiquidHydrogen => Chemistry.CRITICAL_TEMPERATURE_HYDROGEN_K, 
			Chemistry.GasType.Hydrazine => Chemistry.CRITICAL_TEMPERATURE_HYDRAZINE_K, 
			Chemistry.GasType.LiquidHydrazine => Chemistry.CRITICAL_TEMPERATURE_HYDRAZINE_K, 
			Chemistry.GasType.LiquidAlcohol => Chemistry.CRITICAL_TEMPERATURE_ETHANOL_K, 
			Chemistry.GasType.LiquidSodiumChloride => Chemistry.CRITICAL_TEMPERATURE_SODIUMCHLORIDE, 
			Chemistry.GasType.Helium => Chemistry.CRITICAL_TEMPERATURE_HELIUM_K, 
			Chemistry.GasType.Silanol => Chemistry.CRITICAL_TEMPERATURE_SILANOL_K, 
			Chemistry.GasType.LiquidSilanol => Chemistry.CRITICAL_TEMPERATURE_SILANOL_K, 
			Chemistry.GasType.HydrochloricAcid => Chemistry.CRITICAL_TEMPERATURE_HYDROCHLORIC_ACID_K, 
			Chemistry.GasType.LiquidHydrochloricAcid => Chemistry.CRITICAL_TEMPERATURE_HYDROCHLORIC_ACID_K, 
			Chemistry.GasType.Ozone => Chemistry.CRITICAL_TEMPERATURE_OZONE_K, 
			Chemistry.GasType.LiquidOzone => Chemistry.CRITICAL_TEMPERATURE_OZONE_K, 
			_ => TemperatureKelvin.Zero, 
		};
	}

	public PressurekPa MinimumLiquidPressureAtMaxTemperature()
	{
		return MinimumLiquidPressureAtMaxTemperature(Type);
	}

	public static PressurekPa MinimumLiquidPressureAtMaxTemperature(Chemistry.GasType type)
	{
		return new PressurekPa(type switch
		{
			Chemistry.GasType.Undefined => 0.0, 
			Chemistry.GasType.Oxygen => 6000.0, 
			Chemistry.GasType.Nitrogen => 6000.0, 
			Chemistry.GasType.CarbonDioxide => 6000.0, 
			Chemistry.GasType.Methane => 6000.0, 
			Chemistry.GasType.Pollutant => 6000.0, 
			Chemistry.GasType.Water => 6000.0, 
			Chemistry.GasType.PollutedWater => 6000.0, 
			Chemistry.GasType.NitrousOxide => 2000.0, 
			Chemistry.GasType.LiquidNitrogen => 6000.0, 
			Chemistry.GasType.LiquidOxygen => 6000.0, 
			Chemistry.GasType.LiquidMethane => 6000.0, 
			Chemistry.GasType.Steam => 6000.0, 
			Chemistry.GasType.LiquidCarbonDioxide => 6000.0, 
			Chemistry.GasType.LiquidPollutant => 6000.0, 
			Chemistry.GasType.LiquidNitrousOxide => 2000.0, 
			Chemistry.GasType.Hydrogen => 6000.0, 
			Chemistry.GasType.LiquidHydrogen => 6000.0, 
			Chemistry.GasType.Hydrazine => 6000.0, 
			Chemistry.GasType.LiquidHydrazine => 6000.0, 
			Chemistry.GasType.LiquidAlcohol => 1000.0, 
			Chemistry.GasType.Helium => 515.0, 
			Chemistry.GasType.LiquidSodiumChloride => 515.0, 
			Chemistry.GasType.Silanol => 6000.0, 
			Chemistry.GasType.LiquidSilanol => 6000.0, 
			Chemistry.GasType.HydrochloricAcid => 1000.0, 
			Chemistry.GasType.LiquidHydrochloricAcid => 1000.0, 
			Chemistry.GasType.Ozone => 6000.0, 
			Chemistry.GasType.LiquidOzone => 6000.0, 
			_ => 0.0, 
		});
	}

	public VolumeLitres MolarVolume()
	{
		return MolarVolume(Type);
	}

	public static VolumeLitres MolarVolume(Chemistry.GasType molarType)
	{
		return new VolumeLitres(molarType switch
		{
			Chemistry.GasType.Undefined => 0.0, 
			Chemistry.GasType.Oxygen => 0.0, 
			Chemistry.GasType.Nitrogen => 0.0, 
			Chemistry.GasType.CarbonDioxide => 0.0, 
			Chemistry.GasType.Methane => 0.0, 
			Chemistry.GasType.Pollutant => 0.0, 
			Chemistry.GasType.Water => 0.018, 
			Chemistry.GasType.PollutedWater => 0.018, 
			Chemistry.GasType.NitrousOxide => 0.0, 
			Chemistry.GasType.LiquidNitrogen => 0.0348, 
			Chemistry.GasType.LiquidOxygen => 0.03, 
			Chemistry.GasType.LiquidMethane => 0.04, 
			Chemistry.GasType.LiquidCarbonDioxide => 0.04, 
			Chemistry.GasType.LiquidPollutant => 0.04, 
			Chemistry.GasType.LiquidNitrousOxide => 0.026, 
			Chemistry.GasType.Steam => 0.0, 
			Chemistry.GasType.Hydrogen => 0.0, 
			Chemistry.GasType.LiquidHydrogen => 0.028, 
			Chemistry.GasType.Hydrazine => 0.0, 
			Chemistry.GasType.LiquidHydrazine => 0.03, 
			Chemistry.GasType.LiquidAlcohol => 0.058, 
			Chemistry.GasType.Helium => 0.0, 
			Chemistry.GasType.LiquidSodiumChloride => 0.04, 
			Chemistry.GasType.Silanol => 0.0, 
			Chemistry.GasType.LiquidSilanol => 0.16, 
			Chemistry.GasType.HydrochloricAcid => 0.0, 
			Chemistry.GasType.LiquidHydrochloricAcid => 0.028, 
			Chemistry.GasType.Ozone => 0.0, 
			Chemistry.GasType.LiquidOzone => 0.026, 
			_ => 0.0, 
		});
	}

	public double MolarMass()
	{
		return MolarMass(Type);
	}

	public static double MolarMass(Chemistry.GasType type)
	{
		return type switch
		{
			Chemistry.GasType.Undefined => 0.0, 
			Chemistry.GasType.Oxygen => 16.0, 
			Chemistry.GasType.Nitrogen => 64.0, 
			Chemistry.GasType.CarbonDioxide => 44.0, 
			Chemistry.GasType.Methane => 16.0, 
			Chemistry.GasType.Pollutant => 28.0, 
			Chemistry.GasType.Water => 108.0, 
			Chemistry.GasType.PollutedWater => 108.0, 
			Chemistry.GasType.NitrousOxide => 46.0, 
			Chemistry.GasType.LiquidNitrogen => 64.0, 
			Chemistry.GasType.LiquidOxygen => 16.0, 
			Chemistry.GasType.LiquidMethane => 16.0, 
			Chemistry.GasType.Steam => 108.0, 
			Chemistry.GasType.LiquidCarbonDioxide => 44.0, 
			Chemistry.GasType.LiquidPollutant => 28.0, 
			Chemistry.GasType.LiquidNitrousOxide => 46.0, 
			Chemistry.GasType.Hydrogen => 2.0, 
			Chemistry.GasType.LiquidHydrogen => 2.0, 
			Chemistry.GasType.Hydrazine => 32.0, 
			Chemistry.GasType.LiquidHydrazine => 32.0, 
			Chemistry.GasType.LiquidAlcohol => 18.0, 
			Chemistry.GasType.Helium => 4.0, 
			Chemistry.GasType.LiquidSodiumChloride => 101.0, 
			Chemistry.GasType.Silanol => 166.0, 
			Chemistry.GasType.LiquidSilanol => 166.0, 
			Chemistry.GasType.HydrochloricAcid => 36.0, 
			Chemistry.GasType.LiquidHydrochloricAcid => 36.0, 
			Chemistry.GasType.Ozone => 24.0, 
			Chemistry.GasType.LiquidOzone => 24.0, 
			_ => 0.0, 
		};
	}

	public double GetMass()
	{
		return MolarMass() * Quantity.ToDouble();
	}

	public VolumeLitres GetVolume()
	{
		return new VolumeLitres(MolarVolume(Type).ToDouble() * Quantity.ToDouble());
	}

	public double LatentHeatOfVaporization()
	{
		return LatentHeatOfVaporization(Type);
	}

	public static double LatentHeatOfVaporization(Chemistry.GasType type)
	{
		return type switch
		{
			Chemistry.GasType.Undefined => 0.0, 
			Chemistry.GasType.Oxygen => 800.0, 
			Chemistry.GasType.Nitrogen => 500.0, 
			Chemistry.GasType.CarbonDioxide => 600.0, 
			Chemistry.GasType.Methane => 1000.0, 
			Chemistry.GasType.Pollutant => 2000.0, 
			Chemistry.GasType.Water => 8000.0, 
			Chemistry.GasType.PollutedWater => 8000.0, 
			Chemistry.GasType.NitrousOxide => 4000.0, 
			Chemistry.GasType.LiquidNitrogen => 500.0, 
			Chemistry.GasType.LiquidOxygen => 800.0, 
			Chemistry.GasType.LiquidMethane => 1000.0, 
			Chemistry.GasType.Steam => 8000.0, 
			Chemistry.GasType.LiquidCarbonDioxide => 600.0, 
			Chemistry.GasType.LiquidPollutant => 2000.0, 
			Chemistry.GasType.LiquidNitrousOxide => 4000.0, 
			Chemistry.GasType.Hydrogen => 200.0, 
			Chemistry.GasType.LiquidHydrogen => 200.0, 
			Chemistry.GasType.Hydrazine => 4000.0, 
			Chemistry.GasType.LiquidHydrazine => 4000.0, 
			Chemistry.GasType.LiquidAlcohol => 2000.0, 
			Chemistry.GasType.Helium => 0.0, 
			Chemistry.GasType.LiquidSodiumChloride => 16000.0, 
			Chemistry.GasType.Silanol => 10000.0, 
			Chemistry.GasType.LiquidSilanol => 10000.0, 
			Chemistry.GasType.HydrochloricAcid => 1000.0, 
			Chemistry.GasType.LiquidHydrochloricAcid => 1000.0, 
			Chemistry.GasType.Ozone => 1000.0, 
			Chemistry.GasType.LiquidOzone => 1000.0, 
			_ => 0.0, 
		};
	}

	public static SpecificHeat SpecificHeat(Chemistry.GasType gasType)
	{
		return new SpecificHeat(gasType switch
		{
			Chemistry.GasType.Undefined => 12.0, 
			Chemistry.GasType.Oxygen => 21.1, 
			Chemistry.GasType.Nitrogen => 20.6, 
			Chemistry.GasType.CarbonDioxide => 28.2, 
			Chemistry.GasType.Methane => 20.4, 
			Chemistry.GasType.Pollutant => 24.8, 
			Chemistry.GasType.Water => 72.0, 
			Chemistry.GasType.PollutedWater => 64.0, 
			Chemistry.GasType.NitrousOxide => 37.2, 
			Chemistry.GasType.LiquidNitrogen => 20.6, 
			Chemistry.GasType.LiquidOxygen => 21.1, 
			Chemistry.GasType.LiquidMethane => 20.4, 
			Chemistry.GasType.Steam => 72.0, 
			Chemistry.GasType.LiquidCarbonDioxide => 28.2, 
			Chemistry.GasType.LiquidPollutant => 24.8, 
			Chemistry.GasType.LiquidNitrousOxide => 37.2, 
			Chemistry.GasType.Hydrogen => 20.4, 
			Chemistry.GasType.LiquidHydrogen => 20.4, 
			Chemistry.GasType.Hydrazine => 48.4, 
			Chemistry.GasType.LiquidHydrazine => 48.4, 
			Chemistry.GasType.LiquidAlcohol => 33.0, 
			Chemistry.GasType.Helium => 20.8, 
			Chemistry.GasType.LiquidSodiumChloride => 130.0, 
			Chemistry.GasType.Silanol => 101.0, 
			Chemistry.GasType.LiquidSilanol => 101.0, 
			Chemistry.GasType.HydrochloricAcid => 37.0, 
			Chemistry.GasType.LiquidHydrochloricAcid => 37.0, 
			Chemistry.GasType.Ozone => 38.6, 
			Chemistry.GasType.LiquidOzone => 38.6, 
			_ => 0.0, 
		});
	}

	public SpecificHeat SpecificHeat()
	{
		return SpecificHeat(Type);
	}

	public double Enthalpy()
	{
		return Enthalpy(Type);
	}

	public static double Enthalpy(Chemistry.GasType gasType)
	{
		return gasType switch
		{
			Chemistry.GasType.Methane => 286000.0, 
			Chemistry.GasType.LiquidMethane => 286000.0, 
			Chemistry.GasType.Hydrogen => 306000.0, 
			Chemistry.GasType.LiquidHydrogen => 306000.0, 
			Chemistry.GasType.Hydrazine => 306000.0, 
			Chemistry.GasType.LiquidHydrazine => 306000.0, 
			Chemistry.GasType.LiquidAlcohol => 566000.0, 
			_ => 0.0, 
		};
	}

	public static Mole Create(Chemistry.GasType gasType)
	{
		return new Mole(gasType, MoleQuantity.Zero, MoleEnergy.Zero);
	}

	public Mole(Chemistry.GasType gasType, MoleQuantity quantity, MoleEnergy energy)
	{
		if (gasType == Chemistry.GasType.Undefined)
		{
			throw new ArgumentException("Cannot Create a mole of GasType Undefined.");
		}
		if (quantity.IsNaN())
		{
			quantity = MoleQuantity.Zero;
		}
		if (energy.IsNaN())
		{
			energy = MoleEnergy.Zero;
		}
		IsValid = true;
		ReadOnly = false;
		IsCachable = false;
		Type = gasType;
		_energy = energy;
		_energyCached = energy;
		_quantity = quantity;
		_quantityCached = quantity;
		QuantityDirty = true;
		EnergyDirty = true;
		_lastEnergyDirtied = MoleEnergy.Zero;
		_lastQuantityDirtied = MoleQuantity.Zero;
	}

	public double HeatCapacityRatio()
	{
		switch (Type)
		{
		case Chemistry.GasType.Undefined:
		case Chemistry.GasType.Helium:
			return Chemistry.MonatomicDegreesOfFreedom;
		case Chemistry.GasType.Oxygen:
		case Chemistry.GasType.Nitrogen:
		case Chemistry.GasType.CarbonDioxide:
		case Chemistry.GasType.Methane:
		case Chemistry.GasType.Water:
		case Chemistry.GasType.NitrousOxide:
		case Chemistry.GasType.LiquidNitrogen:
		case Chemistry.GasType.LiquidOxygen:
		case Chemistry.GasType.LiquidMethane:
		case Chemistry.GasType.Steam:
		case Chemistry.GasType.LiquidCarbonDioxide:
		case Chemistry.GasType.LiquidNitrousOxide:
		case Chemistry.GasType.Hydrogen:
		case Chemistry.GasType.LiquidHydrogen:
		case Chemistry.GasType.PollutedWater:
		case Chemistry.GasType.Ozone:
		case Chemistry.GasType.LiquidOzone:
			return Chemistry.TriatomicDegreesOfFreedom;
		case Chemistry.GasType.Pollutant:
		case Chemistry.GasType.LiquidPollutant:
		case Chemistry.GasType.Hydrazine:
		case Chemistry.GasType.LiquidHydrazine:
		case Chemistry.GasType.LiquidAlcohol:
		case Chemistry.GasType.LiquidSodiumChloride:
		case Chemistry.GasType.Silanol:
		case Chemistry.GasType.LiquidSilanol:
		case Chemistry.GasType.HydrochloricAcid:
		case Chemistry.GasType.LiquidHydrochloricAcid:
			return Chemistry.PolyatomicDegreesOfFreedom;
		default:
			return Chemistry.TriatomicDegreesOfFreedom;
		}
	}

	public double ThermalEfficiency()
	{
		switch (Type)
		{
		case Chemistry.GasType.Hydrogen:
		case Chemistry.GasType.Helium:
			return 0.18;
		case Chemistry.GasType.Methane:
			return 0.15;
		case Chemistry.GasType.Oxygen:
		case Chemistry.GasType.Nitrogen:
		case Chemistry.GasType.NitrousOxide:
		case Chemistry.GasType.Ozone:
			return 0.12;
		case Chemistry.GasType.Undefined:
		case Chemistry.GasType.CarbonDioxide:
		case Chemistry.GasType.Hydrazine:
			return 0.08;
		case Chemistry.GasType.Pollutant:
		case Chemistry.GasType.Steam:
		case Chemistry.GasType.Silanol:
		case Chemistry.GasType.HydrochloricAcid:
			return 0.05;
		case Chemistry.GasType.Water:
		case Chemistry.GasType.LiquidNitrogen:
		case Chemistry.GasType.LiquidOxygen:
		case Chemistry.GasType.LiquidMethane:
		case Chemistry.GasType.LiquidCarbonDioxide:
		case Chemistry.GasType.LiquidPollutant:
		case Chemistry.GasType.LiquidNitrousOxide:
		case Chemistry.GasType.LiquidHydrogen:
		case Chemistry.GasType.PollutedWater:
		case Chemistry.GasType.LiquidHydrazine:
		case Chemistry.GasType.LiquidAlcohol:
		case Chemistry.GasType.LiquidSodiumChloride:
		case Chemistry.GasType.LiquidSilanol:
		case Chemistry.GasType.LiquidHydrochloricAcid:
		case Chemistry.GasType.LiquidOzone:
			return 0.0;
		default:
			return 0.03;
		}
	}

	public AtmosphereHelper.MatterState MatterState()
	{
		return MatterState(Type);
	}

	public static AtmosphereHelper.MatterState MatterState(Chemistry.GasType gasType)
	{
		switch (gasType)
		{
		case Chemistry.GasType.Water:
		case Chemistry.GasType.LiquidNitrogen:
		case Chemistry.GasType.LiquidOxygen:
		case Chemistry.GasType.LiquidMethane:
		case Chemistry.GasType.LiquidCarbonDioxide:
		case Chemistry.GasType.LiquidPollutant:
		case Chemistry.GasType.LiquidNitrousOxide:
		case Chemistry.GasType.LiquidHydrogen:
		case Chemistry.GasType.PollutedWater:
		case Chemistry.GasType.LiquidHydrazine:
		case Chemistry.GasType.LiquidAlcohol:
		case Chemistry.GasType.LiquidSodiumChloride:
		case Chemistry.GasType.LiquidSilanol:
		case Chemistry.GasType.LiquidHydrochloricAcid:
		case Chemistry.GasType.LiquidOzone:
			return AtmosphereHelper.MatterState.Liquid;
		default:
			return AtmosphereHelper.MatterState.Gas;
		case Chemistry.GasType.Undefined:
		case Chemistry.GasType.Air:
		case Chemistry.GasType.Fuel:
			return AtmosphereHelper.MatterState.None;
		}
	}

	public Mole ChangeState(PressurekPa pressure, VolumeLitres volume, TemperatureKelvin offset, bool preventAboluteZeroEvaporation = true)
	{
		if (Quantity < Chemistry.MINIMUM_QUANTITY_MOLES)
		{
			return MoleHelper.Invalid;
		}
		switch (MatterState())
		{
		case AtmosphereHelper.MatterState.Liquid:
		{
			if (!MoleHelper.CanEvaporate(Type))
			{
				return MoleHelper.Invalid;
			}
			TemperatureKelvin temperatureKelvin = EvaporationTemperatureClamped(pressure) + offset;
			PressurekPa pressurekPa = EvaporationPressureClamped(Temperature);
			TemperatureKelvin temperatureKelvin2 = new TemperatureKelvin(FreezingTemperature().ToDouble() / 2.0);
			if (Temperature < FreezingTemperature() && preventAboluteZeroEvaporation)
			{
				TemperatureKelvin temperatureKelvin3 = RocketMath.Max(temperatureKelvin2, Temperature);
				pressurekPa = new PressurekPa(RocketMath.MapToScale(temperatureKelvin2.ToDouble(), FreezingTemperature().ToDouble(), Chemistry.ArmstrongLimit.ToDouble(), MinLiquidPressure().ToDouble(), temperatureKelvin3.ToDouble()));
			}
			PressurekPa pressurekPa2 = pressurekPa - pressure;
			if (Temperature <= temperatureKelvin)
			{
				if (pressure > pressurekPa)
				{
					return MoleHelper.Invalid;
				}
				if (!(Temperature > temperatureKelvin2))
				{
					return MoleHelper.Invalid;
				}
				temperatureKelvin = RocketMath.Lerp(Temperature, new TemperatureKelvin(Temperature.ToDouble() - 10.0), (pressurekPa2 / MinLiquidPressure()).ToDouble());
				temperatureKelvin = RocketMath.Max(temperatureKelvin, temperatureKelvin2);
			}
			MoleQuantity maxQuantity = Quantity;
			if (pressure < pressurekPa)
			{
				maxQuantity = RocketMath.Max(MoleQuantity.Zero, IdealGas.Quantity(pressurekPa2, volume, Temperature));
			}
			MoleEnergy moleEnergy2 = IdealGas.Energy(Temperature - temperatureKelvin, SpecificHeat(), Quantity);
			if (Temperature < new TemperatureKelvin(FreezingTemperature().ToDouble() + 1.0) && Temperature > new TemperatureKelvin(temperatureKelvin2.ToDouble() + 1.0) && preventAboluteZeroEvaporation)
			{
				moleEnergy2 = new MoleEnergy((double)GameManager.GameTickSpeedSeconds * SpecificHeat().ToDouble() * Quantity.ToDouble());
			}
			if (Temperature > MaxLiquidTemperature())
			{
				moleEnergy2 = RocketMath.Max(IdealGas.Energy(Temperature - MaxLiquidTemperature(), SpecificHeat(), Quantity), moleEnergy2);
				maxQuantity = Quantity;
			}
			if (moleEnergy2 < new MoleEnergy(Chemistry.MINIMUM_QUANTITY_MOLES.ToDouble() * LatentHeatOfVaporization()))
			{
				if (Quantity < Chemistry.MINIMUM_WORLD_VALID_TOTAL_MOLES)
				{
					return StateChangeLiquid(moleEnergy2, 1.0, maxQuantity);
				}
				return MoleHelper.Invalid;
			}
			return StateChangeLiquid(moleEnergy2, 0.1, maxQuantity);
		}
		case AtmosphereHelper.MatterState.Gas:
		{
			if (!MoleHelper.CanCondense(Type))
			{
				return MoleHelper.Invalid;
			}
			if (pressure < MinLiquidPressure())
			{
				return MoleHelper.Invalid;
			}
			TemperatureKelvin temperatureKelvin = EvaporationTemperatureClamped(pressure) + offset;
			if (Temperature >= temperatureKelvin)
			{
				return MoleHelper.Invalid;
			}
			MoleEnergy moleEnergy = IdealGas.Energy(temperatureKelvin - Temperature, SpecificHeat(), Quantity);
			if (moleEnergy < new MoleEnergy(Chemistry.MINIMUM_QUANTITY_MOLES.ToDouble() * LatentHeatOfVaporization()))
			{
				if (Quantity <= Chemistry.MINIMUM_WORLD_VALID_TOTAL_MOLES)
				{
					return StateChangeGas(moleEnergy, 1.0);
				}
				return MoleHelper.Invalid;
			}
			return StateChangeGas(moleEnergy, 0.1);
		}
		default:
			return MoleHelper.Invalid;
		}
	}

	public Mole FreezeGlobalMoles(TemperatureKelvin offset, MoleQuantity max)
	{
		TemperatureKelvin temperatureKelvin = FreezingTemperature() + offset;
		if (Temperature >= temperatureKelvin)
		{
			return MoleHelper.Invalid;
		}
		double latentHeat = ((MatterState() == AtmosphereHelper.MatterState.Liquid) ? LatentHeatOfFusion : (LatentHeatOfVaporization() + LatentHeatOfFusion));
		MoleQuantity moleQuantity = RocketMath.Min(Quantity, max);
		MoleEnergy moleEnergy = IdealGas.Energy(moleQuantity, latentHeat);
		MoleEnergy moleEnergy2 = Energy * (moleQuantity / Quantity).ToDouble();
		MoleEnergy moleEnergy3 = Energy - moleEnergy2 + moleEnergy;
		MoleQuantity quantity = RocketMath.Max(Quantity - moleQuantity, MoleQuantity.Zero);
		MoleEnergy moleEnergy4 = MoleEnergy.Zero;
		if (moleEnergy3 < MoleEnergy.Zero)
		{
			moleEnergy4 = moleEnergy3;
			moleEnergy3 = MoleEnergy.Zero;
		}
		Set(quantity, moleEnergy3);
		return new Mole(MoleHelper.FreezeType(Type), moleQuantity, moleEnergy2 + moleEnergy4);
	}

	public Mole MeltGlobalLiquidMoles(TemperatureKelvin offset, MoleQuantity max)
	{
		if (MatterState() != AtmosphereHelper.MatterState.Liquid)
		{
			throw new NotImplementedException("MeltGlobalLiquidMoles only supports melting to liquid");
		}
		TemperatureKelvin temperatureKelvin = FreezingTemperature() + offset;
		if (Temperature < temperatureKelvin)
		{
			return MoleHelper.Invalid;
		}
		MoleEnergy moleEnergy = IdealGas.Energy(Temperature - temperatureKelvin, SpecificHeat(), Quantity);
		if (moleEnergy <= MoleEnergy.Zero)
		{
			return MoleHelper.Invalid;
		}
		MoleQuantity moleQuantity = RocketMath.Min(Quantity, max);
		MoleQuantity moleQuantity2 = IdealGas.Quantity(moleEnergy, LatentHeatOfVaporization());
		double num = 1.0;
		if (moleQuantity2 > moleQuantity)
		{
			num = (moleQuantity2 / moleQuantity).ToDouble();
		}
		moleQuantity2 /= num;
		moleEnergy /= num;
		MoleEnergy moleEnergy2 = Energy * (moleQuantity2 / Quantity).ToDouble();
		MoleEnergy moleEnergy3 = Energy - moleEnergy2 - moleEnergy;
		MoleQuantity quantity = RocketMath.Max(Quantity - moleQuantity2, MoleQuantity.Zero);
		MoleEnergy moleEnergy4 = MoleEnergy.Zero;
		if (moleEnergy3 < MoleEnergy.Zero)
		{
			moleEnergy4 = moleEnergy3;
			moleEnergy3 = MoleEnergy.Zero;
		}
		Set(quantity, moleEnergy3);
		return new Mole(Type, moleQuantity2, moleEnergy2 + moleEnergy4);
	}

	public GasItem.StateSymbolType CheckChangeState(PressurekPa pressure)
	{
		if (Quantity < Chemistry.MINIMUM_QUANTITY_MOLES)
		{
			return GasItem.StateSymbolType.none;
		}
		GasItem.StateSymbolType stateSymbolType = GasItem.StateSymbolType.none;
		switch (MatterState())
		{
		case AtmosphereHelper.MatterState.Liquid:
		{
			if (!MoleHelper.CanEvaporate(Type))
			{
				return stateSymbolType | GasItem.StateSymbolType.none;
			}
			TemperatureKelvin temperatureKelvin = EvaporationTemperatureClamped(pressure);
			PressurekPa pressurekPa = EvaporationPressureClamped(Temperature);
			PressurekPa pressurekPa2 = pressurekPa - pressure;
			if (Temperature <= temperatureKelvin)
			{
				if (pressure > pressurekPa)
				{
					return stateSymbolType | GasItem.StateSymbolType.none;
				}
				double t = (pressurekPa2 / MinLiquidPressure()).ToDouble();
				temperatureKelvin = RocketMath.Lerp(Temperature, new TemperatureKelvin(Temperature.ToDouble() - 10.0), t);
			}
			if (IdealGas.Energy(Temperature - temperatureKelvin, SpecificHeat(), Quantity) < IdealGas.Energy(Chemistry.MINIMUM_QUANTITY_MOLES, LatentHeatOfVaporization()))
			{
				if (Quantity < Chemistry.MINIMUM_WORLD_VALID_TOTAL_MOLES)
				{
					return stateSymbolType | GasItem.StateSymbolType.evaporation;
				}
				return stateSymbolType | GasItem.StateSymbolType.none;
			}
			return stateSymbolType | GasItem.StateSymbolType.evaporation;
		}
		case AtmosphereHelper.MatterState.Gas:
		{
			if (!MoleHelper.CanCondense(Type))
			{
				return stateSymbolType | GasItem.StateSymbolType.none;
			}
			if (pressure < MinLiquidPressure())
			{
				return stateSymbolType | GasItem.StateSymbolType.none;
			}
			TemperatureKelvin temperatureKelvin = EvaporationTemperatureClamped(pressure);
			if (Temperature >= temperatureKelvin)
			{
				return GasItem.StateSymbolType.none;
			}
			if (IdealGas.Energy(temperatureKelvin - Temperature, SpecificHeat(), Quantity) < IdealGas.Energy(Chemistry.MINIMUM_QUANTITY_MOLES, LatentHeatOfVaporization()))
			{
				if (Quantity <= Chemistry.MINIMUM_WORLD_VALID_TOTAL_MOLES)
				{
					return stateSymbolType | GasItem.StateSymbolType.condensation;
				}
				return stateSymbolType | GasItem.StateSymbolType.none;
			}
			return stateSymbolType | GasItem.StateSymbolType.condensation;
		}
		default:
			return stateSymbolType | GasItem.StateSymbolType.none;
		}
	}

	private Mole StateChangeGas(MoleEnergy deficitEnergy, double ratio)
	{
		MoleEnergy val = IdealGas.Energy(Quantity, LatentHeatOfVaporization());
		deficitEnergy = RocketMath.Min(deficitEnergy, val);
		MoleQuantity moleQuantity = IdealGas.Quantity(deficitEnergy, LatentHeatOfVaporization());
		double num = 1.0;
		if (moleQuantity > Quantity)
		{
			num = (moleQuantity / Quantity).ToDouble();
		}
		if (moleQuantity < new MoleQuantity(0.1))
		{
			ratio = 0.5;
		}
		moleQuantity /= num;
		deficitEnergy /= num;
		moleQuantity *= ratio;
		deficitEnergy *= ratio;
		MoleEnergy moleEnergy = Energy * (moleQuantity / Quantity).ToDouble();
		MoleEnergy moleEnergy2 = Energy - moleEnergy + deficitEnergy;
		MoleQuantity quantity = RocketMath.Max(Quantity - moleQuantity, MoleQuantity.Zero);
		MoleEnergy moleEnergy3 = MoleEnergy.Zero;
		if (moleEnergy2 < MoleEnergy.Zero)
		{
			moleEnergy3 = moleEnergy2;
			moleEnergy2 = MoleEnergy.Zero;
		}
		Set(quantity, moleEnergy2);
		return new Mole(MoleHelper.CondensationType(Type), moleQuantity, moleEnergy + moleEnergy3);
	}

	private Mole StateChangeLiquid(MoleEnergy energyForStateChange, double ratio, MoleQuantity maxQuantity)
	{
		if (energyForStateChange <= MoleEnergy.Zero)
		{
			return MoleHelper.Invalid;
		}
		maxQuantity = RocketMath.Min(maxQuantity, Quantity);
		MoleQuantity moleQuantity = IdealGas.Quantity(energyForStateChange, LatentHeatOfVaporization());
		double num = 1.0;
		if (moleQuantity > maxQuantity)
		{
			num = (moleQuantity / maxQuantity).ToDouble();
		}
		if (moleQuantity < LowStateChangeQuantityBound)
		{
			ratio = 0.5;
		}
		moleQuantity /= num;
		energyForStateChange /= num;
		moleQuantity *= ratio;
		energyForStateChange *= ratio;
		MoleEnergy moleEnergy = Energy * (moleQuantity / Quantity).ToDouble();
		MoleEnergy moleEnergy2 = Energy - moleEnergy - energyForStateChange;
		MoleQuantity moleQuantity2 = RocketMath.Max(Quantity - moleQuantity, MoleQuantity.Zero);
		MoleEnergy moleEnergy3 = MoleEnergy.Zero;
		if (moleEnergy2 < MoleEnergy.Zero)
		{
			moleEnergy3 = moleEnergy2;
			moleEnergy2 = MoleEnergy.Zero;
		}
		if (moleQuantity2 > MoleQuantity.Zero && moleEnergy2 <= MoleEnergy.Zero)
		{
			MoleHelper.LogMessage("ERROR: State Change Function attempted to Set Energy value to zero").Forget();
			return MoleHelper.Invalid;
		}
		if (moleQuantity2 <= MoleQuantity.Zero && moleEnergy2 > MoleEnergy.Zero)
		{
			MoleHelper.LogMessage("ERROR: State Change Function attempted to Set Quantity value to zero").Forget();
			return MoleHelper.Invalid;
		}
		Set(moleQuantity2, moleEnergy2);
		return new Mole(MoleHelper.EvaporationType(Type), moleQuantity, moleEnergy * MoleHelper.EvaporationRatio(Type) + moleEnergy3);
	}

	public bool WillFreeze()
	{
		if (Quantity < Chemistry.MINIMUM_QUANTITY_MOLES)
		{
			return false;
		}
		return Temperature <= FreezingTemperature();
	}

	public TemperatureKelvin EvaporationTemperatureClamped(PressurekPa pressure)
	{
		pressure = RocketMath.Clamp(pressure, MinLiquidPressure(), MinimumLiquidPressureAtMaxTemperature());
		return RocketMath.Clamp(MoleHelper.EvaporationTemperature(Type, pressure), FreezingTemperature(), MaxLiquidTemperature());
	}

	public PressurekPa EvaporationPressureClamped(TemperatureKelvin temperature)
	{
		temperature = RocketMath.Clamp(temperature, FreezingTemperature(), MaxLiquidTemperature());
		return RocketMath.Clamp(MoleHelper.EvaporationPressure(Type, temperature), MinLiquidPressure(), MinimumLiquidPressureAtMaxTemperature());
	}

	public void Clear()
	{
		Quantity = MoleQuantity.Zero;
		Energy = MoleEnergy.Zero;
		QuantityDirty = true;
		EnergyDirty = true;
		UpdateCache();
	}

	public void Cleanup()
	{
		if (Quantity < Chemistry.MINIMUM_QUANTITY_MOLES)
		{
			Clear();
		}
	}

	public void UpdateCache(DirtyMoleTolerance dirtyMoleTolerance = DirtyMoleTolerance.OnePercent)
	{
		_energyCached = _energy;
		if (NetworkManager.IsServer && !RocketMath.Approximately(_energyCached, _lastEnergyDirtied, dirtyMoleTolerance))
		{
			EnergyDirty = true;
			_lastEnergyDirtied = _energyCached;
		}
		_quantityCached = _quantity;
		if (NetworkManager.IsServer && !RocketMath.Approximately(_quantityCached, _lastQuantityDirtied, dirtyMoleTolerance))
		{
			QuantityDirty = true;
			_lastQuantityDirtied = _quantityCached;
		}
	}

	public void Add(MoleQuantity quantity, MoleEnergy energy)
	{
		if (IsValid && !quantity.IsDenormalOrNegative() && !energy.IsDenormalOrNegative())
		{
			if (quantity.IsNaN())
			{
				MoleHelper.LogMessage("add mole quantity NaN").Forget();
			}
			else if (energy.IsNaN())
			{
				MoleHelper.LogMessage("add mole energy NaN").Forget();
			}
			else if (MoleQuantity.MaxValue - Quantity < quantity)
			{
				Quantity = new MoleQuantity(MoleQuantity.MaxValue.ToDouble() * 0.9);
				MoleHelper.LogMessage("add mole quantity overflow").Forget();
			}
			else if (MoleEnergy.MaxValue - Energy < energy)
			{
				Energy = new MoleEnergy(MoleEnergy.MaxValue.ToDouble() * 0.9);
				MoleHelper.LogMessage("add mole energy overflow").Forget();
			}
			else
			{
				Quantity += quantity;
				Energy += energy;
			}
		}
	}

	public void AddTwentyDegreeC(MoleQuantity quantity)
	{
		MoleEnergy energy = IdealGas.Energy(Chemistry.Temperature.TwentyDegrees, SpecificHeat(), quantity);
		Add(quantity, energy);
	}

	public void AddAtTemperature(TemperatureKelvin temperature, MoleQuantity quantity)
	{
		MoleEnergy energy = IdealGas.Energy(temperature, SpecificHeat(), quantity);
		Add(quantity, energy);
	}

	public void Add(Mole mole)
	{
		if (mole.IsValid)
		{
			Add(mole.Quantity, mole.Energy);
		}
	}

	public Mole Remove(MoleQuantity removedMoles)
	{
		removedMoles = RocketMath.Min(Quantity, removedMoles);
		if (Quantity.IsNaN())
		{
			MoleHelper.LogMessage("remove mole quantity NaN").Forget();
			return MoleHelper.Invalid;
		}
		if (removedMoles.IsNaN())
		{
			return MoleHelper.Invalid;
		}
		if (Quantity.IsDenormalToNegative())
		{
			return MoleHelper.Invalid;
		}
		Mole result = Create(Type);
		MoleEnergy moleEnergy = Energy * (removedMoles / Quantity).ToDouble();
		Quantity -= removedMoles;
		Energy -= moleEnergy;
		result.Add(removedMoles, moleEnergy);
		return result;
	}

	public void Set(Mole newMole)
	{
		if (newMole.IsValid)
		{
			Set(newMole.Quantity, newMole.Energy);
		}
	}

	public void Set(MoleQuantity quantity, MoleEnergy energy)
	{
		if (quantity.IsDenormal() || quantity < MoleQuantity.Zero)
		{
			quantity = new MoleQuantity(0.0);
		}
		if (energy.IsDenormal() || energy < MoleEnergy.Zero)
		{
			energy = new MoleEnergy(0.0);
		}
		Quantity = quantity;
		Energy = energy;
	}

	public void Set(MoleQuantity quantity)
	{
		Set(quantity, MoleEnergy.Zero);
	}

	public void Scale(double scaleFactor)
	{
		if (scaleFactor.IsDenormal())
		{
			scaleFactor = 0.0;
		}
		if (double.IsNaN(scaleFactor))
		{
			MoleHelper.LogMessage("ERROR: Can't scale mole to NaN. Mole has been reset").Forget();
			scaleFactor = 0.0;
		}
		Quantity *= scaleFactor;
		Energy *= scaleFactor;
	}

	public void Split(double divideBy)
	{
		if (!divideBy.IsDenormalOrZero() && !double.IsNaN(divideBy))
		{
			Quantity /= divideBy;
			Energy /= divideBy;
		}
	}

	public void Lerp(ref Mole target, double t)
	{
		MoleQuantity quantity = Quantity;
		MoleEnergy energy = Energy;
		Quantity = RocketMath.Lerp(Quantity, target.Quantity, t);
		Energy = RocketMath.Lerp(Energy, target.Energy, t);
		MoleQuantity moleQuantity = Quantity - quantity;
		MoleEnergy moleEnergy = Energy - energy;
		target.Set(target.Quantity - moleQuantity, target.Energy - moleEnergy);
	}

	public bool IsNaN()
	{
		if (!_energyCached.IsNaN() && !_quantityCached.IsNaN() && !_energy.IsNaN())
		{
			return _quantity.IsNaN();
		}
		return true;
	}

	public bool CanFreeze()
	{
		return CanFreeze(Type);
	}

	public static bool CanFreeze(Chemistry.GasType gasType)
	{
		if (gasType == Chemistry.GasType.Helium)
		{
			return false;
		}
		return true;
	}
}
