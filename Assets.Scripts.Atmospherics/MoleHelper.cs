using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Atmospherics;

public static class MoleHelper
{
	public static readonly Mole Invalid = default(Mole);

	public const float DELTA_RATIO_FOR_NETWORK_UPDATE = 0.01f;

	private static readonly Dictionary<Chemistry.GasType, double> InWorldVolumeMultipliers = new Dictionary<Chemistry.GasType, double>();

	private const double DEFAULT_INWORLD_VOLUME_MULTIPLIER = 50.0;

	public static void Initialize()
	{
		CacheInWorldVolumeMultipliers();
	}

	public static double GetInWorldVolumeMultiplier(Chemistry.GasType gasType)
	{
		return InWorldVolumeMultipliers[gasType];
	}

	private static void CacheInWorldVolumeMultipliers()
	{
		for (int i = 0; i < EnumCollections.GasTypes.Values.Length; i++)
		{
			Chemistry.GasType gasType = EnumCollections.GasTypes.Values[i];
			InWorldVolumeMultipliers.Add(gasType, CalculateWorldVolumeMultiplier(gasType));
		}
	}

	public static double CalculateWorldVolumeMultiplier(Chemistry.GasType gasType)
	{
		if (Mole.MatterState(gasType) != AtmosphereHelper.MatterState.Liquid)
		{
			return 1.0;
		}
		if (!CanEvaporate(gasType))
		{
			return 50.0;
		}
		MoleQuantity moleQuantity = IdealGas.Quantity(Mole.MinimumLiquidPressureAtMaxTemperature(gasType), Chemistry.GridVolume, Mole.MaxLiquidTemperature(gasType));
		VolumeLitres volumeLitres = Chemistry.GridVolume - Atmosphere.GetMinimumGasVolume(AtmosphereHelper.AtmosphereMode.World);
		VolumeLitres volumeLitres2 = Mole.MolarVolume(gasType) * moleQuantity.ToDouble();
		double val = (volumeLitres / volumeLitres2).ToDouble();
		return Math.Min(50.0, val);
	}

	public static string GetMoleDescription(Chemistry.GasType gasType)
	{
		return gasType switch
		{
			Chemistry.GasType.Oxygen => InterfaceStrings.MoleOxygenDescription, 
			Chemistry.GasType.Nitrogen => InterfaceStrings.MoleNitrogenDescription, 
			Chemistry.GasType.CarbonDioxide => InterfaceStrings.MoleCarbonDioxideDescription, 
			Chemistry.GasType.Methane => InterfaceStrings.MoleMethaneDescription, 
			Chemistry.GasType.Pollutant => InterfaceStrings.MolePollutantDescription, 
			Chemistry.GasType.Water => InterfaceStrings.MoleWaterDescription, 
			Chemistry.GasType.PollutedWater => InterfaceStrings.MolePollutedWaterDescription, 
			Chemistry.GasType.NitrousOxide => InterfaceStrings.MoleNitrousOxideDescription, 
			Chemistry.GasType.LiquidNitrogen => InterfaceStrings.MoleLiquidNitrogenDescription, 
			Chemistry.GasType.LiquidOxygen => InterfaceStrings.MoleLiquidOxygenDescription, 
			Chemistry.GasType.LiquidMethane => InterfaceStrings.MoleLiquidMethaneDescription, 
			Chemistry.GasType.Steam => InterfaceStrings.MoleSteamDescription, 
			Chemistry.GasType.LiquidCarbonDioxide => InterfaceStrings.MoleLiquidCarbonDioxideDescription, 
			Chemistry.GasType.LiquidPollutant => InterfaceStrings.MoleLiquidPollutantDescription, 
			Chemistry.GasType.LiquidNitrousOxide => InterfaceStrings.MoleLiquidNitrousOxideDescription, 
			Chemistry.GasType.Hydrogen => InterfaceStrings.MoleHydrogenDescription, 
			Chemistry.GasType.LiquidHydrogen => InterfaceStrings.MoleLiquidHydrogenDescription, 
			Chemistry.GasType.Hydrazine => InterfaceStrings.MoleHydrazineDescription, 
			Chemistry.GasType.LiquidHydrazine => InterfaceStrings.MoleLiquidHydrazineDescription, 
			Chemistry.GasType.LiquidAlcohol => InterfaceStrings.MoleLiquidAlcoholDescription, 
			Chemistry.GasType.Helium => InterfaceStrings.MoleHeliumDescription, 
			Chemistry.GasType.LiquidSodiumChloride => InterfaceStrings.MoleLiquidSodiumChlorideDescription, 
			Chemistry.GasType.Silanol => InterfaceStrings.MoleSilanolDescription, 
			Chemistry.GasType.LiquidSilanol => InterfaceStrings.MoleLiquidSilanolDescription, 
			Chemistry.GasType.HydrochloricAcid => InterfaceStrings.MoleHydrochloricAcidDescription, 
			Chemistry.GasType.LiquidHydrochloricAcid => InterfaceStrings.MoleLiquidHydrochloricAcidDescription, 
			Chemistry.GasType.Ozone => InterfaceStrings.MoleOzoneDescription, 
			Chemistry.GasType.LiquidOzone => InterfaceStrings.MoleLiquidOzoneDescription, 
			_ => throw new ArgumentOutOfRangeException("gasType", gasType, null), 
		};
	}

	public static Mole Generate(string mole)
	{
		Chemistry.GasType gasType = EnumCollections.GasTypes.Get(mole);
		if (gasType == Chemistry.GasType.Air || gasType == Chemistry.GasType.Fuel || gasType == Chemistry.GasType.Undefined)
		{
			return Invalid;
		}
		return Mole.Create(gasType);
	}

	public static TemperatureKelvin EvaporationTemperature(Chemistry.GasType gasType, PressurekPa pressure)
	{
		if (gasType == Chemistry.GasType.Undefined)
		{
			throw new ArgumentOutOfRangeException("gasType", gasType, null);
		}
		return new TemperatureKelvin(Math.Pow(pressure.ToDouble() / EvaporationCoefficientA(gasType), 1.0 / EvaporationCoefficientB(gasType)));
	}

	public static PressurekPa EvaporationPressure(Chemistry.GasType gasType, TemperatureKelvin temperature)
	{
		if (gasType == Chemistry.GasType.Undefined)
		{
			throw new ArgumentOutOfRangeException("gasType", gasType, null);
		}
		return new PressurekPa(EvaporationCoefficientA(gasType) * Math.Pow(temperature.ToDouble(), EvaporationCoefficientB(gasType)));
	}

	private static double EvaporationCoefficientA(Chemistry.GasType gasType)
	{
		switch (gasType)
		{
		case Chemistry.GasType.Oxygen:
		case Chemistry.GasType.LiquidOxygen:
			return 2.6854996004E-11;
		case Chemistry.GasType.Nitrogen:
		case Chemistry.GasType.LiquidNitrogen:
			return 5.5757107833E-07;
		case Chemistry.GasType.CarbonDioxide:
		case Chemistry.GasType.LiquidCarbonDioxide:
			return 1.579573E-26;
		case Chemistry.GasType.Methane:
		case Chemistry.GasType.LiquidMethane:
			return 5.863496734E-15;
		case Chemistry.GasType.Pollutant:
		case Chemistry.GasType.LiquidPollutant:
			return 2.079033884;
		case Chemistry.GasType.Water:
		case Chemistry.GasType.Steam:
			return 3.8782059839E-19;
		case Chemistry.GasType.PollutedWater:
			return 4E-20;
		case Chemistry.GasType.NitrousOxide:
		case Chemistry.GasType.LiquidNitrousOxide:
			return 0.065353501531;
		case Chemistry.GasType.Hydrogen:
		case Chemistry.GasType.LiquidHydrogen:
			return 3.18041E-05;
		case Chemistry.GasType.Hydrazine:
		case Chemistry.GasType.LiquidHydrazine:
			return 8E-22;
		case Chemistry.GasType.LiquidAlcohol:
			return 9E-20;
		case Chemistry.GasType.Helium:
			return 0.0;
		case Chemistry.GasType.LiquidSodiumChloride:
			return 6.211737044295E-08;
		case Chemistry.GasType.Silanol:
		case Chemistry.GasType.LiquidSilanol:
			return 0.48388429552357676;
		case Chemistry.GasType.HydrochloricAcid:
		case Chemistry.GasType.LiquidHydrochloricAcid:
			return 1E-21;
		case Chemistry.GasType.Ozone:
		case Chemistry.GasType.LiquidOzone:
			return 0.22905767012828845;
		default:
			throw new ArgumentOutOfRangeException("gasType", gasType, null);
		}
	}

	private static double EvaporationCoefficientB(Chemistry.GasType gasType)
	{
		switch (gasType)
		{
		case Chemistry.GasType.Oxygen:
		case Chemistry.GasType.LiquidOxygen:
			return 6.49214937325;
		case Chemistry.GasType.Nitrogen:
		case Chemistry.GasType.LiquidNitrogen:
			return 4.40221368946;
		case Chemistry.GasType.CarbonDioxide:
		case Chemistry.GasType.LiquidCarbonDioxide:
			return 12.195837931;
		case Chemistry.GasType.Methane:
		case Chemistry.GasType.LiquidMethane:
			return 7.8643601035;
		case Chemistry.GasType.Pollutant:
		case Chemistry.GasType.LiquidPollutant:
			return 1.31202194555;
		case Chemistry.GasType.Water:
		case Chemistry.GasType.Steam:
			return 7.90030107708;
		case Chemistry.GasType.PollutedWater:
			return 8.27025711260823;
		case Chemistry.GasType.NitrousOxide:
		case Chemistry.GasType.LiquidNitrousOxide:
			return 1.70297431874;
		case Chemistry.GasType.Hydrogen:
		case Chemistry.GasType.LiquidHydrogen:
			return 4.4843872973;
		case Chemistry.GasType.Hydrazine:
		case Chemistry.GasType.LiquidHydrazine:
			return 9.15642808045339;
		case Chemistry.GasType.LiquidAlcohol:
			return 8.391884446078986;
		case Chemistry.GasType.Helium:
			return 0.0;
		case Chemistry.GasType.LiquidSodiumChloride:
			return 2.8774143233482707;
		case Chemistry.GasType.Silanol:
		case Chemistry.GasType.LiquidSilanol:
			return 1.4041336082044964;
		case Chemistry.GasType.HydrochloricAcid:
		case Chemistry.GasType.LiquidHydrochloricAcid:
			return 9.108844460789863;
		case Chemistry.GasType.Ozone:
		case Chemistry.GasType.LiquidOzone:
			return 1.7787987515586163;
		default:
			throw new ArgumentOutOfRangeException("gasType", gasType, null);
		}
	}

	public static bool CanEvaporate(Chemistry.GasType gasType)
	{
		switch (gasType)
		{
		case Chemistry.GasType.Undefined:
		case Chemistry.GasType.Oxygen:
		case Chemistry.GasType.Nitrogen:
		case Chemistry.GasType.CarbonDioxide:
		case Chemistry.GasType.Methane:
		case Chemistry.GasType.Pollutant:
		case Chemistry.GasType.NitrousOxide:
		case Chemistry.GasType.Steam:
		case Chemistry.GasType.Hydrogen:
		case Chemistry.GasType.Hydrazine:
		case Chemistry.GasType.Helium:
		case Chemistry.GasType.Silanol:
		case Chemistry.GasType.HydrochloricAcid:
		case Chemistry.GasType.Ozone:
			return false;
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
			return true;
		default:
			throw new ArgumentOutOfRangeException("gasType", gasType, null);
		}
	}

	public static bool CanCondense(Chemistry.GasType gasType)
	{
		switch (gasType)
		{
		case Chemistry.GasType.Undefined:
			return false;
		case Chemistry.GasType.Oxygen:
		case Chemistry.GasType.Nitrogen:
		case Chemistry.GasType.CarbonDioxide:
		case Chemistry.GasType.Methane:
		case Chemistry.GasType.Pollutant:
		case Chemistry.GasType.NitrousOxide:
		case Chemistry.GasType.Steam:
		case Chemistry.GasType.Hydrogen:
		case Chemistry.GasType.Hydrazine:
		case Chemistry.GasType.Silanol:
		case Chemistry.GasType.HydrochloricAcid:
		case Chemistry.GasType.Ozone:
			return true;
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
		case Chemistry.GasType.Helium:
		case Chemistry.GasType.LiquidSodiumChloride:
		case Chemistry.GasType.LiquidSilanol:
		case Chemistry.GasType.LiquidHydrochloricAcid:
		case Chemistry.GasType.LiquidOzone:
			return false;
		default:
			throw new ArgumentOutOfRangeException("gasType", gasType, null);
		}
	}

	public static Chemistry.GasType EvaporationType(Chemistry.GasType fromType)
	{
		switch (fromType)
		{
		case Chemistry.GasType.LiquidNitrogen:
			return Chemistry.GasType.Nitrogen;
		case Chemistry.GasType.Water:
		case Chemistry.GasType.PollutedWater:
			return Chemistry.GasType.Steam;
		case Chemistry.GasType.LiquidOxygen:
			return Chemistry.GasType.Oxygen;
		case Chemistry.GasType.LiquidMethane:
			return Chemistry.GasType.Methane;
		case Chemistry.GasType.LiquidCarbonDioxide:
			return Chemistry.GasType.CarbonDioxide;
		case Chemistry.GasType.LiquidPollutant:
			return Chemistry.GasType.Pollutant;
		case Chemistry.GasType.LiquidNitrousOxide:
			return Chemistry.GasType.NitrousOxide;
		case Chemistry.GasType.LiquidHydrogen:
			return Chemistry.GasType.Hydrogen;
		case Chemistry.GasType.LiquidHydrazine:
			return Chemistry.GasType.Hydrazine;
		case Chemistry.GasType.LiquidAlcohol:
			return Chemistry.GasType.Methane;
		case Chemistry.GasType.LiquidSodiumChloride:
			return Chemistry.GasType.Pollutant;
		case Chemistry.GasType.LiquidSilanol:
			return Chemistry.GasType.Silanol;
		case Chemistry.GasType.LiquidHydrochloricAcid:
			return Chemistry.GasType.HydrochloricAcid;
		case Chemistry.GasType.LiquidOzone:
			return Chemistry.GasType.Ozone;
		default:
			return Chemistry.GasType.Undefined;
		}
	}

	public static double EvaporationRatio(Chemistry.GasType fromType)
	{
		return fromType switch
		{
			Chemistry.GasType.LiquidAlcohol => 34.0 / 55.0, 
			Chemistry.GasType.LiquidSodiumChloride => 62.0 / 325.0, 
			_ => 1.0, 
		};
	}

	public static Chemistry.GasType CondensationType(Chemistry.GasType fromType)
	{
		return fromType switch
		{
			Chemistry.GasType.Nitrogen => Chemistry.GasType.LiquidNitrogen, 
			Chemistry.GasType.Oxygen => Chemistry.GasType.LiquidOxygen, 
			Chemistry.GasType.Methane => Chemistry.GasType.LiquidMethane, 
			Chemistry.GasType.Steam => Chemistry.GasType.Water, 
			Chemistry.GasType.CarbonDioxide => Chemistry.GasType.LiquidCarbonDioxide, 
			Chemistry.GasType.Pollutant => Chemistry.GasType.LiquidPollutant, 
			Chemistry.GasType.NitrousOxide => Chemistry.GasType.LiquidNitrousOxide, 
			Chemistry.GasType.Hydrogen => Chemistry.GasType.LiquidHydrogen, 
			Chemistry.GasType.Hydrazine => Chemistry.GasType.LiquidHydrazine, 
			Chemistry.GasType.Silanol => Chemistry.GasType.LiquidSilanol, 
			Chemistry.GasType.HydrochloricAcid => Chemistry.GasType.LiquidHydrochloricAcid, 
			Chemistry.GasType.Ozone => Chemistry.GasType.LiquidOzone, 
			_ => Chemistry.GasType.Undefined, 
		};
	}

	public static Chemistry.GasType FreezeType(Chemistry.GasType fromType)
	{
		Chemistry.GasType gasType = CondensationType(fromType);
		if (gasType != Chemistry.GasType.Undefined)
		{
			return gasType;
		}
		return fromType;
	}

	public static async UniTaskVoid LogMessage(string errorMessage)
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		ConsoleWindow.PrintError("error atmos thread: " + errorMessage);
	}
}
