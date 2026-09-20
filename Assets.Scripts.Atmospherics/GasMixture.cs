using System;
using System.Collections.Generic;
using Assets.Scripts.Genetics;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Reagents;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Atmospherics;

public struct GasMixture : ITradable, IEvaluable
{
	public Mole Oxygen;

	public Mole Nitrogen;

	public Mole CarbonDioxide;

	public Mole Methane;

	public Mole Pollutant;

	public Mole Water;

	public Mole PollutedWater;

	public Mole NitrousOxide;

	public Mole LiquidNitrogen;

	public Mole LiquidOxygen;

	public Mole LiquidMethane;

	public Mole Steam;

	public Mole LiquidCarbonDioxide;

	public Mole LiquidPollutant;

	public Mole LiquidNitrousOxide;

	public Mole Hydrogen;

	public Mole LiquidHydrogen;

	public Mole Hydrazine;

	public Mole LiquidHydrazine;

	public Mole LiquidAlcohol;

	public Mole Helium;

	public Mole LiquidSodiumChloride;

	public Mole Silanol;

	public Mole LiquidSilanol;

	public Mole HydrochloricAcid;

	public Mole LiquidHydrochloricAcid;

	public Mole Ozone;

	public Mole LiquidOzone;

	private bool _isValid;

	private bool _isCachable;

	private TemperatureKelvin _temperatureCached;

	public static MoleQuantity MinCombustionMoles = MoleQuantity.One;

	public bool IsValid => _isValid;

	public HeatCapacity HeatCapacity => Oxygen.HeatCapacity + Nitrogen.HeatCapacity + CarbonDioxide.HeatCapacity + Methane.HeatCapacity + Pollutant.HeatCapacity + Water.HeatCapacity + PollutedWater.HeatCapacity + NitrousOxide.HeatCapacity + LiquidNitrogen.HeatCapacity + LiquidOxygen.HeatCapacity + LiquidMethane.HeatCapacity + Steam.HeatCapacity + LiquidCarbonDioxide.HeatCapacity + LiquidPollutant.HeatCapacity + LiquidNitrousOxide.HeatCapacity + Hydrogen.HeatCapacity + LiquidHydrogen.HeatCapacity + Hydrazine.HeatCapacity + LiquidHydrazine.HeatCapacity + LiquidAlcohol.HeatCapacity + Helium.HeatCapacity + LiquidSodiumChloride.HeatCapacity + Silanol.HeatCapacity + LiquidSilanol.HeatCapacity + HydrochloricAcid.HeatCapacity + LiquidHydrochloricAcid.HeatCapacity + Ozone.HeatCapacity + LiquidOzone.HeatCapacity;

	public MoleQuantity TotalToxins => MoleQuantity.Zero + Methane.Quantity + Pollutant.Quantity + LiquidMethane.Quantity + LiquidPollutant.Quantity + PollutedWater.Quantity + Hydrazine.Quantity + LiquidHydrazine.Quantity + HydrochloricAcid.Quantity + LiquidHydrochloricAcid.Quantity + Silanol.Quantity + LiquidSilanol.Quantity;

	public MoleQuantity TotalHypergolics => Hydrazine.Quantity + LiquidHydrazine.Quantity;

	public MoleQuantity TotalFuel => MoleQuantity.Zero + Methane.Quantity + LiquidMethane.Quantity + Hydrogen.Quantity + LiquidHydrogen.Quantity + LiquidAlcohol.Quantity;

	public MoleQuantity TotalOxidiser => MoleQuantity.Zero + Oxygen.Quantity + LiquidOxygen.Quantity + NitrousOxide.Quantity + LiquidNitrousOxide.Quantity + Ozone.Quantity + LiquidOzone.Quantity;

	public MoleEnergy TotalEnergy
	{
		get
		{
			return Oxygen.Energy + CarbonDioxide.Energy + Nitrogen.Energy + Methane.Energy + Pollutant.Energy + Water.Energy + PollutedWater.Energy + NitrousOxide.Energy + LiquidNitrogen.Energy + LiquidOxygen.Energy + LiquidMethane.Energy + Steam.Energy + LiquidCarbonDioxide.Energy + LiquidPollutant.Energy + LiquidNitrousOxide.Energy + Hydrogen.Energy + LiquidHydrogen.Energy + Hydrazine.Energy + LiquidHydrazine.Energy + LiquidAlcohol.Energy + Helium.Energy + LiquidSodiumChloride.Energy + Silanol.Energy + LiquidSilanol.Energy + HydrochloricAcid.Energy + LiquidHydrochloricAcid.Energy + Ozone.Energy + LiquidOzone.Energy;
		}
		set
		{
			HeatCapacity heatCapacity = HeatCapacity;
			if (!heatCapacity.IsDenormalToNegative() && !heatCapacity.IsNaN())
			{
				if (value.IsDenormalOrNegative())
				{
					value = new MoleEnergy(0.0);
				}
				Oxygen.Energy = new MoleEnergy(value.ToDouble() * (Oxygen.HeatCapacity / heatCapacity).ToDouble());
				CarbonDioxide.Energy = new MoleEnergy(value.ToDouble() * (CarbonDioxide.HeatCapacity / heatCapacity).ToDouble());
				Nitrogen.Energy = new MoleEnergy(value.ToDouble() * (Nitrogen.HeatCapacity / heatCapacity).ToDouble());
				Methane.Energy = new MoleEnergy(value.ToDouble() * (Methane.HeatCapacity / heatCapacity).ToDouble());
				Pollutant.Energy = new MoleEnergy(value.ToDouble() * (Pollutant.HeatCapacity / heatCapacity).ToDouble());
				Water.Energy = new MoleEnergy(value.ToDouble() * (Water.HeatCapacity / heatCapacity).ToDouble());
				PollutedWater.Energy = new MoleEnergy(value.ToDouble() * (PollutedWater.HeatCapacity / heatCapacity).ToDouble());
				NitrousOxide.Energy = new MoleEnergy(value.ToDouble() * (NitrousOxide.HeatCapacity / heatCapacity).ToDouble());
				LiquidNitrogen.Energy = new MoleEnergy(value.ToDouble() * (LiquidNitrogen.HeatCapacity / heatCapacity).ToDouble());
				LiquidOxygen.Energy = new MoleEnergy(value.ToDouble() * (LiquidOxygen.HeatCapacity / heatCapacity).ToDouble());
				LiquidMethane.Energy = new MoleEnergy(value.ToDouble() * (LiquidMethane.HeatCapacity / heatCapacity).ToDouble());
				Steam.Energy = new MoleEnergy(value.ToDouble() * (Steam.HeatCapacity / heatCapacity).ToDouble());
				LiquidCarbonDioxide.Energy = new MoleEnergy(value.ToDouble() * (LiquidCarbonDioxide.HeatCapacity / heatCapacity).ToDouble());
				LiquidPollutant.Energy = new MoleEnergy(value.ToDouble() * (LiquidPollutant.HeatCapacity / heatCapacity).ToDouble());
				LiquidNitrousOxide.Energy = new MoleEnergy(value.ToDouble() * (LiquidNitrousOxide.HeatCapacity / heatCapacity).ToDouble());
				Hydrogen.Energy = new MoleEnergy(value.ToDouble() * (Hydrogen.HeatCapacity / heatCapacity).ToDouble());
				LiquidHydrogen.Energy = new MoleEnergy(value.ToDouble() * (LiquidHydrogen.HeatCapacity / heatCapacity).ToDouble());
				Hydrazine.Energy = new MoleEnergy(value.ToDouble() * (Hydrazine.HeatCapacity / heatCapacity).ToDouble());
				LiquidHydrazine.Energy = new MoleEnergy(value.ToDouble() * (LiquidHydrazine.HeatCapacity / heatCapacity).ToDouble());
				LiquidAlcohol.Energy = new MoleEnergy(value.ToDouble() * (LiquidAlcohol.HeatCapacity / heatCapacity).ToDouble());
				Helium.Energy = new MoleEnergy(value.ToDouble() * (Helium.HeatCapacity / heatCapacity).ToDouble());
				LiquidSodiumChloride.Energy = new MoleEnergy(value.ToDouble() * (LiquidSodiumChloride.HeatCapacity / heatCapacity).ToDouble());
				Silanol.Energy = new MoleEnergy(value.ToDouble() * (Silanol.HeatCapacity / heatCapacity).ToDouble());
				LiquidSilanol.Energy = new MoleEnergy(value.ToDouble() * (LiquidSilanol.HeatCapacity / heatCapacity).ToDouble());
				HydrochloricAcid.Energy = new MoleEnergy(value.ToDouble() * (HydrochloricAcid.HeatCapacity / heatCapacity).ToDouble());
				LiquidHydrochloricAcid.Energy = new MoleEnergy(value.ToDouble() * (LiquidHydrochloricAcid.HeatCapacity / heatCapacity).ToDouble());
				Ozone.Energy = new MoleEnergy(value.ToDouble() * (Ozone.HeatCapacity / heatCapacity).ToDouble());
				LiquidOzone.Energy = new MoleEnergy(value.ToDouble() * (LiquidOzone.HeatCapacity / heatCapacity).ToDouble());
			}
		}
	}

	public MoleQuantity GetTotalMolesGassesAndLiquids => GetTotalMoles(AtmosphereHelper.MatterState.All);

	public MoleQuantity GetTotalMolesLiquids => GetTotalMoles(AtmosphereHelper.MatterState.Liquid);

	public MoleQuantity GetTotalMolesGasses => GetTotalMoles(AtmosphereHelper.MatterState.Gas);

	public TemperatureKelvin Temperature
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _temperatureCached;
			}
			if (HeatCapacity.IsDenormalToNegative())
			{
				return TemperatureKelvin.Zero;
			}
			return IdealGas.Temperature(TotalEnergy, HeatCapacity);
		}
	}

	public bool IsCachable
	{
		get
		{
			return _isCachable;
		}
		set
		{
			_isCachable = value;
			Oxygen.IsCachable = value;
			CarbonDioxide.IsCachable = value;
			Nitrogen.IsCachable = value;
			Methane.IsCachable = value;
			Water.IsCachable = value;
			PollutedWater.IsCachable = value;
			Pollutant.IsCachable = value;
			NitrousOxide.IsCachable = value;
			LiquidNitrogen.IsCachable = value;
			LiquidOxygen.IsCachable = value;
			LiquidMethane.IsCachable = value;
			Steam.IsCachable = value;
			LiquidCarbonDioxide.IsCachable = value;
			LiquidPollutant.IsCachable = value;
			LiquidNitrousOxide.IsCachable = value;
			Hydrogen.IsCachable = value;
			LiquidHydrogen.IsCachable = value;
			Hydrazine.IsCachable = value;
			LiquidHydrazine.IsCachable = value;
			LiquidAlcohol.IsCachable = value;
			Helium.IsCachable = value;
			LiquidSodiumChloride.IsCachable = value;
			Silanol.IsCachable = value;
			LiquidSilanol.IsCachable = value;
			HydrochloricAcid.IsCachable = value;
			LiquidHydrochloricAcid.IsCachable = value;
			Ozone.IsCachable = value;
			LiquidOzone.IsCachable = value;
		}
	}

	public float GetQuantity => GetTotalMolesGassesAndLiquids.ToFloat();

	public float GetTradableQuantity => GetQuantity;

	public VolumeLitres VolumeLiquids => Water.Volume + PollutedWater.Volume + LiquidNitrogen.Volume + LiquidOxygen.Volume + LiquidMethane.Volume + LiquidCarbonDioxide.Volume + LiquidPollutant.Volume + LiquidNitrousOxide.Volume + LiquidHydrogen.Volume + LiquidHydrazine.Volume + LiquidAlcohol.Volume + LiquidSodiumChloride.Volume + LiquidSilanol.Volume + LiquidHydrochloricAcid.Volume + LiquidOzone.Volume;

	public MoleQuantity TotalInertMoles => Nitrogen.Quantity + CarbonDioxide.Quantity + Steam.Quantity + Pollutant.Quantity + LiquidNitrogen.Quantity + LiquidCarbonDioxide.Quantity + Water.Quantity + LiquidPollutant.Quantity + PollutedWater.Quantity + Helium.Quantity + Silanol.Quantity + HydrochloricAcid.Quantity + LiquidSodiumChloride.Quantity + LiquidSilanol.Quantity + LiquidHydrochloricAcid.Quantity;

	public GasAction CreateGasAction(Mole mole)
	{
		if (mole.Quantity.Equals(MoleQuantity.Zero))
		{
			return null;
		}
		return new GasAction
		{
			Type = mole.Type,
			Moles = mole.Quantity.ToFloat(),
			Energy = mole.Energy.ToFloat()
		};
	}

	public static void CreateGasActions(ref List<ActionData> actionList, GasMixture gasMixture)
	{
		if (!gasMixture.GetTotalMolesGassesAndLiquids.Equals(MoleQuantity.Zero))
		{
			if (!gasMixture.Oxygen.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.Oxygen));
			}
			if (!gasMixture.Nitrogen.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.Nitrogen));
			}
			if (!gasMixture.CarbonDioxide.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.CarbonDioxide));
			}
			if (!gasMixture.Methane.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.Methane));
			}
			if (!gasMixture.Pollutant.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.Pollutant));
			}
			if (!gasMixture.Water.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.Water));
			}
			if (!gasMixture.PollutedWater.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.PollutedWater));
			}
			if (!gasMixture.NitrousOxide.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.NitrousOxide));
			}
			if (!gasMixture.LiquidNitrogen.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.LiquidNitrogen));
			}
			if (!gasMixture.LiquidOxygen.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.LiquidOxygen));
			}
			if (!gasMixture.LiquidMethane.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.LiquidMethane));
			}
			if (!gasMixture.Steam.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.Steam));
			}
			if (!gasMixture.LiquidCarbonDioxide.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.LiquidCarbonDioxide));
			}
			if (!gasMixture.LiquidPollutant.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.LiquidPollutant));
			}
			if (!gasMixture.LiquidNitrousOxide.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.LiquidNitrousOxide));
			}
			if (!gasMixture.Hydrogen.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.Hydrogen));
			}
			if (!gasMixture.LiquidHydrogen.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.LiquidHydrogen));
			}
			if (!gasMixture.Hydrazine.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.Hydrazine));
			}
			if (!gasMixture.LiquidHydrazine.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.LiquidHydrazine));
			}
			if (!gasMixture.LiquidAlcohol.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.LiquidAlcohol));
			}
			if (!gasMixture.Helium.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.Helium));
			}
			if (!gasMixture.LiquidSodiumChloride.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.LiquidSodiumChloride));
			}
			if (!gasMixture.Silanol.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.Silanol));
			}
			if (!gasMixture.LiquidSilanol.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.LiquidSilanol));
			}
			if (!gasMixture.HydrochloricAcid.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.HydrochloricAcid));
			}
			if (!gasMixture.LiquidHydrochloricAcid.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.LiquidHydrochloricAcid));
			}
			if (!gasMixture.Ozone.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.Ozone));
			}
			if (!gasMixture.LiquidOzone.Quantity.Equals(MoleQuantity.Zero))
			{
				actionList.Add(gasMixture.CreateGasAction(gasMixture.LiquidOzone));
			}
		}
	}

	public GasMixture CheckForIceFormation(PressurekPa pressure, MoleQuantity minFrozenMolarQuantity)
	{
		GasMixture result = GasMixtureHelper.Create();
		if (pressure >= Chemistry.ArmstrongLimit)
		{
			if (Oxygen.WillFreeze() && Oxygen.Quantity >= minFrozenMolarQuantity)
			{
				result.Add(Oxygen);
			}
			if (Nitrogen.WillFreeze() && Nitrogen.Quantity >= minFrozenMolarQuantity)
			{
				result.Add(Nitrogen);
			}
			if (CarbonDioxide.WillFreeze() && CarbonDioxide.Quantity >= minFrozenMolarQuantity)
			{
				result.Add(CarbonDioxide);
			}
			if (Methane.WillFreeze() && Methane.Quantity >= minFrozenMolarQuantity)
			{
				result.Add(Methane);
			}
			if (Pollutant.WillFreeze() && Pollutant.Quantity >= minFrozenMolarQuantity)
			{
				result.Add(Pollutant);
			}
			if (NitrousOxide.WillFreeze() && NitrousOxide.Quantity >= minFrozenMolarQuantity)
			{
				result.Add(NitrousOxide);
			}
			if (Steam.WillFreeze() && Steam.Quantity >= minFrozenMolarQuantity)
			{
				result.Add(Steam);
			}
			if (Hydrogen.WillFreeze() && Hydrogen.Quantity >= minFrozenMolarQuantity)
			{
				result.Add(Hydrogen);
			}
			if (Hydrazine.WillFreeze() && Hydrazine.Quantity >= minFrozenMolarQuantity)
			{
				result.Add(Hydrazine);
			}
			if (Silanol.WillFreeze() && Silanol.Quantity >= minFrozenMolarQuantity)
			{
				result.Add(Silanol);
			}
			if (HydrochloricAcid.WillFreeze() && HydrochloricAcid.Quantity >= minFrozenMolarQuantity)
			{
				result.Add(HydrochloricAcid);
			}
			if (Ozone.WillFreeze() && Ozone.Quantity >= minFrozenMolarQuantity)
			{
				result.Add(Ozone);
			}
		}
		if (Water.WillFreeze() && Water.Quantity >= minFrozenMolarQuantity)
		{
			result.Add(Water);
		}
		if (PollutedWater.WillFreeze() && PollutedWater.Quantity >= minFrozenMolarQuantity)
		{
			result.Add(PollutedWater);
		}
		if (LiquidNitrogen.WillFreeze() && LiquidNitrogen.Quantity >= minFrozenMolarQuantity)
		{
			result.Add(LiquidNitrogen);
		}
		if (LiquidOxygen.WillFreeze() && LiquidOxygen.Quantity >= minFrozenMolarQuantity)
		{
			result.Add(LiquidOxygen);
		}
		if (LiquidMethane.WillFreeze() && LiquidMethane.Quantity >= minFrozenMolarQuantity)
		{
			result.Add(LiquidMethane);
		}
		if (LiquidCarbonDioxide.WillFreeze() && LiquidCarbonDioxide.Quantity >= minFrozenMolarQuantity)
		{
			result.Add(LiquidCarbonDioxide);
		}
		if (LiquidPollutant.WillFreeze() && LiquidPollutant.Quantity >= minFrozenMolarQuantity)
		{
			result.Add(LiquidPollutant);
		}
		if (LiquidNitrousOxide.WillFreeze() && LiquidNitrousOxide.Quantity >= minFrozenMolarQuantity)
		{
			result.Add(LiquidNitrousOxide);
		}
		if (LiquidHydrogen.WillFreeze() && LiquidHydrogen.Quantity >= minFrozenMolarQuantity)
		{
			result.Add(LiquidHydrogen);
		}
		if (LiquidHydrazine.WillFreeze() && LiquidHydrazine.Quantity >= minFrozenMolarQuantity)
		{
			result.Add(LiquidHydrazine);
		}
		if (LiquidAlcohol.WillFreeze() && LiquidAlcohol.Quantity >= minFrozenMolarQuantity)
		{
			result.Add(LiquidAlcohol);
		}
		if (LiquidSodiumChloride.WillFreeze() && LiquidSodiumChloride.Quantity >= minFrozenMolarQuantity)
		{
			result.Add(LiquidSodiumChloride);
		}
		if (LiquidSilanol.WillFreeze() && LiquidSilanol.Quantity >= minFrozenMolarQuantity)
		{
			result.Add(LiquidSilanol);
		}
		if (LiquidHydrochloricAcid.WillFreeze() && LiquidHydrochloricAcid.Quantity >= minFrozenMolarQuantity)
		{
			result.Add(LiquidHydrochloricAcid);
		}
		if (LiquidOzone.WillFreeze() && LiquidOzone.Quantity >= minFrozenMolarQuantity)
		{
			result.Add(LiquidOzone);
		}
		return result;
	}

	public GasMixture CheckForFreezing(PressurekPa pressure)
	{
		return CheckForIceFormation(pressure, MoleQuantity.Zero);
	}

	public uint GasQuantitiesDirtied()
	{
		uint num = 0u;
		if (Oxygen.QuantityDirty)
		{
			num |= 1;
		}
		if (Nitrogen.QuantityDirty)
		{
			num |= 2;
		}
		if (CarbonDioxide.QuantityDirty)
		{
			num |= 4;
		}
		if (Methane.QuantityDirty)
		{
			num |= 8;
		}
		if (Pollutant.QuantityDirty)
		{
			num |= 0x10;
		}
		if (Water.QuantityDirty)
		{
			num |= 0x20;
		}
		if (PollutedWater.QuantityDirty)
		{
			num |= 0x10000;
		}
		if (NitrousOxide.QuantityDirty)
		{
			num |= 0x40;
		}
		if (LiquidNitrogen.QuantityDirty)
		{
			num |= 0x80;
		}
		if (LiquidOxygen.QuantityDirty)
		{
			num |= 0x100;
		}
		if (LiquidMethane.QuantityDirty)
		{
			num |= 0x200;
		}
		if (Steam.QuantityDirty)
		{
			num |= 0x400;
		}
		if (LiquidCarbonDioxide.QuantityDirty)
		{
			num |= 0x800;
		}
		if (LiquidPollutant.QuantityDirty)
		{
			num |= 0x1000;
		}
		if (LiquidNitrousOxide.QuantityDirty)
		{
			num |= 0x2000;
		}
		if (Hydrogen.QuantityDirty)
		{
			num |= 0x4000;
		}
		if (LiquidHydrogen.QuantityDirty)
		{
			num |= 0x8000;
		}
		if (Hydrazine.QuantityDirty)
		{
			num |= 0x20000;
		}
		if (LiquidHydrazine.QuantityDirty)
		{
			num |= 0x40000;
		}
		if (LiquidAlcohol.QuantityDirty)
		{
			num |= 0x80000;
		}
		if (Helium.QuantityDirty)
		{
			num |= 0x100000;
		}
		if (LiquidSodiumChloride.QuantityDirty)
		{
			num |= 0x200000;
		}
		if (Silanol.QuantityDirty)
		{
			num |= 0x400000;
		}
		if (LiquidSilanol.QuantityDirty)
		{
			num |= 0x800000;
		}
		if (HydrochloricAcid.QuantityDirty)
		{
			num |= 0x1000000;
		}
		if (LiquidHydrochloricAcid.QuantityDirty)
		{
			num |= 0x2000000;
		}
		if (Ozone.QuantityDirty)
		{
			num |= 0x4000000;
		}
		if (LiquidOzone.QuantityDirty)
		{
			num |= 0x8000000;
		}
		return num;
	}

	public void UndirtyMoles()
	{
		Oxygen.Undirty();
		Nitrogen.Undirty();
		CarbonDioxide.Undirty();
		Methane.Undirty();
		Pollutant.Undirty();
		Water.Undirty();
		PollutedWater.Undirty();
		NitrousOxide.Undirty();
		LiquidNitrogen.Undirty();
		LiquidOxygen.Undirty();
		LiquidMethane.Undirty();
		Steam.Undirty();
		LiquidCarbonDioxide.Undirty();
		LiquidPollutant.Undirty();
		LiquidNitrousOxide.Undirty();
		Hydrogen.Undirty();
		LiquidHydrogen.Undirty();
		Hydrazine.Undirty();
		LiquidHydrazine.Undirty();
		LiquidAlcohol.Undirty();
		Helium.Undirty();
		LiquidSodiumChloride.Undirty();
		Silanol.Undirty();
		LiquidSilanol.Undirty();
		HydrochloricAcid.Undirty();
		LiquidHydrochloricAcid.Undirty();
		Ozone.Undirty();
		LiquidOzone.Undirty();
	}

	public bool EnergyDirty()
	{
		if (GasQuantitiesDirtied() == 0 && !Oxygen.EnergyDirty && !Nitrogen.EnergyDirty && !CarbonDioxide.EnergyDirty && !Methane.EnergyDirty && !Pollutant.EnergyDirty && !Water.EnergyDirty && !PollutedWater.EnergyDirty && !NitrousOxide.EnergyDirty && !LiquidNitrogen.EnergyDirty && !LiquidOxygen.EnergyDirty && !LiquidMethane.EnergyDirty && !Steam.EnergyDirty && !LiquidCarbonDioxide.EnergyDirty && !LiquidPollutant.EnergyDirty && !LiquidNitrousOxide.EnergyDirty && !Hydrogen.EnergyDirty && !LiquidHydrogen.EnergyDirty && !Hydrazine.EnergyDirty && !LiquidHydrazine.EnergyDirty && !LiquidAlcohol.EnergyDirty && !Helium.EnergyDirty && !LiquidSodiumChloride.EnergyDirty && !Silanol.EnergyDirty && !LiquidSilanol.EnergyDirty && !HydrochloricAcid.EnergyDirty && !LiquidHydrochloricAcid.EnergyDirty && !Ozone.EnergyDirty)
		{
			return LiquidOzone.EnergyDirty;
		}
		return true;
	}

	public GasMixture(MoleQuantity none)
	{
		if (none > MoleQuantity.Zero)
		{
			throw new Exception("Mole Quantity must be zero, when creating an empty gas mix");
		}
		_isValid = true;
		Oxygen = Mole.Create(Chemistry.GasType.Oxygen);
		Nitrogen = Mole.Create(Chemistry.GasType.Nitrogen);
		CarbonDioxide = Mole.Create(Chemistry.GasType.CarbonDioxide);
		Methane = Mole.Create(Chemistry.GasType.Methane);
		Pollutant = Mole.Create(Chemistry.GasType.Pollutant);
		Water = Mole.Create(Chemistry.GasType.Water);
		PollutedWater = Mole.Create(Chemistry.GasType.PollutedWater);
		NitrousOxide = Mole.Create(Chemistry.GasType.NitrousOxide);
		LiquidNitrogen = Mole.Create(Chemistry.GasType.LiquidNitrogen);
		LiquidOxygen = Mole.Create(Chemistry.GasType.LiquidOxygen);
		LiquidMethane = Mole.Create(Chemistry.GasType.LiquidMethane);
		Steam = Mole.Create(Chemistry.GasType.Steam);
		LiquidCarbonDioxide = Mole.Create(Chemistry.GasType.LiquidCarbonDioxide);
		LiquidPollutant = Mole.Create(Chemistry.GasType.LiquidPollutant);
		LiquidNitrousOxide = Mole.Create(Chemistry.GasType.LiquidNitrousOxide);
		Hydrogen = Mole.Create(Chemistry.GasType.Hydrogen);
		LiquidHydrogen = Mole.Create(Chemistry.GasType.LiquidHydrogen);
		Hydrazine = Mole.Create(Chemistry.GasType.Hydrazine);
		LiquidHydrazine = Mole.Create(Chemistry.GasType.LiquidHydrazine);
		LiquidAlcohol = Mole.Create(Chemistry.GasType.LiquidAlcohol);
		Helium = Mole.Create(Chemistry.GasType.Helium);
		LiquidSodiumChloride = Mole.Create(Chemistry.GasType.LiquidSodiumChloride);
		Silanol = Mole.Create(Chemistry.GasType.Silanol);
		LiquidSilanol = Mole.Create(Chemistry.GasType.LiquidSilanol);
		HydrochloricAcid = Mole.Create(Chemistry.GasType.HydrochloricAcid);
		LiquidHydrochloricAcid = Mole.Create(Chemistry.GasType.LiquidHydrochloricAcid);
		Ozone = Mole.Create(Chemistry.GasType.Ozone);
		LiquidOzone = Mole.Create(Chemistry.GasType.LiquidOzone);
		_isCachable = false;
		_temperatureCached = TemperatureKelvin.Zero;
	}

	public GasMixture(MoleMixture moleMixture)
	{
		_isValid = true;
		_isCachable = false;
		_temperatureCached = TemperatureKelvin.Zero;
		Oxygen = new Mole(Chemistry.GasType.Oxygen, new MoleQuantity(moleMixture.Oxygen), MoleEnergy.Zero);
		Nitrogen = new Mole(Chemistry.GasType.Nitrogen, new MoleQuantity(moleMixture.Nitrogen), MoleEnergy.Zero);
		CarbonDioxide = new Mole(Chemistry.GasType.CarbonDioxide, new MoleQuantity(moleMixture.CarbonDioxide), MoleEnergy.Zero);
		Methane = new Mole(Chemistry.GasType.Methane, new MoleQuantity(moleMixture.Methane), MoleEnergy.Zero);
		Pollutant = new Mole(Chemistry.GasType.Pollutant, new MoleQuantity(moleMixture.Pollutant), MoleEnergy.Zero);
		Water = new Mole(Chemistry.GasType.Water, new MoleQuantity(moleMixture.Water), MoleEnergy.Zero);
		PollutedWater = new Mole(Chemistry.GasType.PollutedWater, new MoleQuantity(moleMixture.PollutedWater), MoleEnergy.Zero);
		NitrousOxide = new Mole(Chemistry.GasType.NitrousOxide, new MoleQuantity(moleMixture.NitrousOxide), MoleEnergy.Zero);
		LiquidNitrogen = new Mole(Chemistry.GasType.LiquidNitrogen, new MoleQuantity(moleMixture.LiquidNitrogen), MoleEnergy.Zero);
		LiquidOxygen = new Mole(Chemistry.GasType.LiquidOxygen, new MoleQuantity(moleMixture.LiquidOxygen), MoleEnergy.Zero);
		LiquidMethane = new Mole(Chemistry.GasType.LiquidMethane, new MoleQuantity(moleMixture.LiquidMethane), MoleEnergy.Zero);
		Steam = new Mole(Chemistry.GasType.Steam, new MoleQuantity(moleMixture.Steam), MoleEnergy.Zero);
		LiquidCarbonDioxide = new Mole(Chemistry.GasType.LiquidCarbonDioxide, new MoleQuantity(moleMixture.LiquidCarbonDioxide), MoleEnergy.Zero);
		LiquidPollutant = new Mole(Chemistry.GasType.LiquidPollutant, new MoleQuantity(moleMixture.LiquidPollutant), MoleEnergy.Zero);
		LiquidNitrousOxide = new Mole(Chemistry.GasType.LiquidNitrousOxide, new MoleQuantity(moleMixture.LiquidNitrousOxide), MoleEnergy.Zero);
		Hydrogen = new Mole(Chemistry.GasType.Hydrogen, new MoleQuantity(moleMixture.Hydrogen), MoleEnergy.Zero);
		LiquidHydrogen = new Mole(Chemistry.GasType.LiquidHydrogen, new MoleQuantity(moleMixture.LiquidHydrogen), MoleEnergy.Zero);
		Hydrazine = new Mole(Chemistry.GasType.Hydrazine, new MoleQuantity(moleMixture.Hydrazine), MoleEnergy.Zero);
		LiquidHydrazine = new Mole(Chemistry.GasType.LiquidHydrazine, new MoleQuantity(moleMixture.LiquidHydrazine), MoleEnergy.Zero);
		LiquidAlcohol = new Mole(Chemistry.GasType.LiquidAlcohol, new MoleQuantity(moleMixture.LiquidAlcohol), MoleEnergy.Zero);
		Helium = new Mole(Chemistry.GasType.Helium, new MoleQuantity(moleMixture.Helium), MoleEnergy.Zero);
		LiquidSodiumChloride = new Mole(Chemistry.GasType.LiquidSodiumChloride, new MoleQuantity(moleMixture.LiquidSodiumChloride), MoleEnergy.Zero);
		Silanol = new Mole(Chemistry.GasType.Silanol, new MoleQuantity(moleMixture.Silanol), MoleEnergy.Zero);
		LiquidSilanol = new Mole(Chemistry.GasType.LiquidSilanol, new MoleQuantity(moleMixture.LiquidSilanol), MoleEnergy.Zero);
		HydrochloricAcid = new Mole(Chemistry.GasType.HydrochloricAcid, new MoleQuantity(moleMixture.HydrochloricAcid), MoleEnergy.Zero);
		LiquidHydrochloricAcid = new Mole(Chemistry.GasType.LiquidHydrochloricAcid, new MoleQuantity(moleMixture.LiquidHydrochloricAcid), MoleEnergy.Zero);
		Ozone = new Mole(Chemistry.GasType.Ozone, new MoleQuantity(moleMixture.Ozone), MoleEnergy.Zero);
		LiquidOzone = new Mole(Chemistry.GasType.LiquidOzone, new MoleQuantity(moleMixture.LiquidOzone), MoleEnergy.Zero);
	}

	public GasMixture(GasMixture gasMixture)
	{
		_isValid = gasMixture.IsValid;
		_isCachable = (_isCachable = gasMixture.IsCachable);
		_temperatureCached = gasMixture._temperatureCached;
		Oxygen = gasMixture.Oxygen;
		Nitrogen = gasMixture.Nitrogen;
		CarbonDioxide = gasMixture.CarbonDioxide;
		Methane = gasMixture.Methane;
		Pollutant = gasMixture.Pollutant;
		Water = gasMixture.Water;
		PollutedWater = gasMixture.PollutedWater;
		NitrousOxide = gasMixture.NitrousOxide;
		LiquidNitrogen = gasMixture.LiquidNitrogen;
		LiquidOxygen = gasMixture.LiquidOxygen;
		LiquidMethane = gasMixture.LiquidMethane;
		Steam = gasMixture.Steam;
		LiquidCarbonDioxide = gasMixture.LiquidCarbonDioxide;
		LiquidPollutant = gasMixture.LiquidPollutant;
		LiquidNitrousOxide = gasMixture.LiquidNitrousOxide;
		Hydrogen = gasMixture.Hydrogen;
		LiquidHydrogen = gasMixture.LiquidHydrogen;
		Hydrazine = gasMixture.Hydrazine;
		LiquidHydrazine = gasMixture.LiquidHydrazine;
		LiquidAlcohol = gasMixture.LiquidAlcohol;
		Helium = gasMixture.Helium;
		LiquidSodiumChloride = gasMixture.LiquidSodiumChloride;
		Silanol = gasMixture.Silanol;
		LiquidSilanol = gasMixture.LiquidSilanol;
		HydrochloricAcid = gasMixture.HydrochloricAcid;
		LiquidHydrochloricAcid = gasMixture.LiquidHydrochloricAcid;
		Ozone = gasMixture.Ozone;
		LiquidOzone = gasMixture.LiquidOzone;
	}

	public GasMixture(Mole mole)
	{
		_isValid = mole.IsValid;
		_isCachable = false;
		_temperatureCached = TemperatureKelvin.Zero;
		Oxygen = new Mole(Chemistry.GasType.Oxygen, MoleQuantity.Zero, MoleEnergy.Zero);
		Nitrogen = new Mole(Chemistry.GasType.Nitrogen, MoleQuantity.Zero, MoleEnergy.Zero);
		CarbonDioxide = new Mole(Chemistry.GasType.CarbonDioxide, MoleQuantity.Zero, MoleEnergy.Zero);
		Methane = new Mole(Chemistry.GasType.Methane, MoleQuantity.Zero, MoleEnergy.Zero);
		Pollutant = new Mole(Chemistry.GasType.Pollutant, MoleQuantity.Zero, MoleEnergy.Zero);
		Water = new Mole(Chemistry.GasType.Water, MoleQuantity.Zero, MoleEnergy.Zero);
		PollutedWater = new Mole(Chemistry.GasType.PollutedWater, MoleQuantity.Zero, MoleEnergy.Zero);
		NitrousOxide = new Mole(Chemistry.GasType.NitrousOxide, MoleQuantity.Zero, MoleEnergy.Zero);
		LiquidNitrogen = new Mole(Chemistry.GasType.LiquidNitrogen, MoleQuantity.Zero, MoleEnergy.Zero);
		LiquidOxygen = new Mole(Chemistry.GasType.LiquidOxygen, MoleQuantity.Zero, MoleEnergy.Zero);
		LiquidMethane = new Mole(Chemistry.GasType.LiquidMethane, MoleQuantity.Zero, MoleEnergy.Zero);
		Steam = new Mole(Chemistry.GasType.Steam, MoleQuantity.Zero, MoleEnergy.Zero);
		LiquidCarbonDioxide = new Mole(Chemistry.GasType.LiquidCarbonDioxide, MoleQuantity.Zero, MoleEnergy.Zero);
		LiquidPollutant = new Mole(Chemistry.GasType.LiquidPollutant, MoleQuantity.Zero, MoleEnergy.Zero);
		LiquidNitrousOxide = new Mole(Chemistry.GasType.LiquidNitrousOxide, MoleQuantity.Zero, MoleEnergy.Zero);
		Hydrogen = new Mole(Chemistry.GasType.Hydrogen, MoleQuantity.Zero, MoleEnergy.Zero);
		LiquidHydrogen = new Mole(Chemistry.GasType.LiquidHydrogen, MoleQuantity.Zero, MoleEnergy.Zero);
		Hydrazine = new Mole(Chemistry.GasType.Hydrazine, MoleQuantity.Zero, MoleEnergy.Zero);
		LiquidHydrazine = new Mole(Chemistry.GasType.LiquidHydrazine, MoleQuantity.Zero, MoleEnergy.Zero);
		LiquidAlcohol = new Mole(Chemistry.GasType.LiquidAlcohol, MoleQuantity.Zero, MoleEnergy.Zero);
		Helium = new Mole(Chemistry.GasType.Helium, MoleQuantity.Zero, MoleEnergy.Zero);
		LiquidSodiumChloride = new Mole(Chemistry.GasType.LiquidSodiumChloride, MoleQuantity.Zero, MoleEnergy.Zero);
		Silanol = new Mole(Chemistry.GasType.Silanol, MoleQuantity.Zero, MoleEnergy.Zero);
		LiquidSilanol = new Mole(Chemistry.GasType.LiquidSilanol, MoleQuantity.Zero, MoleEnergy.Zero);
		HydrochloricAcid = new Mole(Chemistry.GasType.HydrochloricAcid, MoleQuantity.Zero, MoleEnergy.Zero);
		LiquidHydrochloricAcid = new Mole(Chemistry.GasType.LiquidHydrochloricAcid, MoleQuantity.Zero, MoleEnergy.Zero);
		Ozone = new Mole(Chemistry.GasType.Ozone, MoleQuantity.Zero, MoleEnergy.Zero);
		LiquidOzone = new Mole(Chemistry.GasType.LiquidOzone, MoleQuantity.Zero, MoleEnergy.Zero);
		switch (mole.Type)
		{
		case Chemistry.GasType.Undefined:
			_isValid = false;
			break;
		case Chemistry.GasType.Oxygen:
			Oxygen.Set(mole);
			break;
		case Chemistry.GasType.Nitrogen:
			Nitrogen.Set(mole);
			break;
		case Chemistry.GasType.CarbonDioxide:
			CarbonDioxide.Set(mole);
			break;
		case Chemistry.GasType.Methane:
			Methane.Set(mole);
			break;
		case Chemistry.GasType.Pollutant:
			Pollutant.Set(mole);
			break;
		case Chemistry.GasType.Water:
			Water.Set(mole);
			break;
		case Chemistry.GasType.PollutedWater:
			PollutedWater.Set(mole);
			break;
		case Chemistry.GasType.NitrousOxide:
			NitrousOxide.Set(mole);
			break;
		case Chemistry.GasType.LiquidNitrogen:
			LiquidNitrogen.Set(mole);
			break;
		case Chemistry.GasType.LiquidOxygen:
			LiquidOxygen.Set(mole);
			break;
		case Chemistry.GasType.LiquidMethane:
			LiquidMethane.Set(mole);
			break;
		case Chemistry.GasType.Steam:
			Steam.Set(mole);
			break;
		case Chemistry.GasType.LiquidCarbonDioxide:
			LiquidCarbonDioxide.Set(mole);
			break;
		case Chemistry.GasType.LiquidPollutant:
			LiquidPollutant.Set(mole);
			break;
		case Chemistry.GasType.LiquidNitrousOxide:
			LiquidNitrousOxide.Set(mole);
			break;
		case Chemistry.GasType.Hydrogen:
			Hydrogen.Set(mole);
			break;
		case Chemistry.GasType.LiquidHydrogen:
			LiquidHydrogen.Set(mole);
			break;
		case Chemistry.GasType.Hydrazine:
			Hydrazine.Set(mole);
			break;
		case Chemistry.GasType.LiquidHydrazine:
			LiquidHydrazine.Set(mole);
			break;
		case Chemistry.GasType.LiquidAlcohol:
			LiquidAlcohol.Set(mole);
			break;
		case Chemistry.GasType.Helium:
			Helium.Set(mole);
			break;
		case Chemistry.GasType.LiquidSodiumChloride:
			LiquidSodiumChloride.Set(mole);
			break;
		case Chemistry.GasType.Silanol:
			Silanol.Set(mole);
			break;
		case Chemistry.GasType.LiquidSilanol:
			LiquidSilanol.Set(mole);
			break;
		case Chemistry.GasType.HydrochloricAcid:
			HydrochloricAcid.Set(mole);
			break;
		case Chemistry.GasType.LiquidHydrochloricAcid:
			LiquidHydrochloricAcid.Set(mole);
			break;
		case Chemistry.GasType.Ozone:
			Ozone.Set(mole);
			break;
		case Chemistry.GasType.LiquidOzone:
			LiquidOzone.Set(mole);
			break;
		default:
			_isValid = false;
			break;
		}
	}

	public void Reset()
	{
		Oxygen.Clear();
		Nitrogen.Clear();
		CarbonDioxide.Clear();
		Methane.Clear();
		Pollutant.Clear();
		Water.Clear();
		PollutedWater.Clear();
		NitrousOxide.Clear();
		LiquidNitrogen.Clear();
		LiquidOxygen.Clear();
		LiquidMethane.Clear();
		Steam.Clear();
		LiquidCarbonDioxide.Clear();
		LiquidPollutant.Clear();
		LiquidNitrousOxide.Clear();
		Hydrogen.Clear();
		LiquidHydrogen.Clear();
		Hydrazine.Clear();
		LiquidHydrazine.Clear();
		LiquidAlcohol.Clear();
		Helium.Clear();
		LiquidSodiumChloride.Clear();
		Silanol.Clear();
		LiquidSilanol.Clear();
		HydrochloricAcid.Clear();
		LiquidHydrochloricAcid.Clear();
		Ozone.Clear();
		LiquidOzone.Clear();
	}

	public Mole GetMoleValue(Chemistry.GasType gasType)
	{
		return gasType switch
		{
			Chemistry.GasType.Undefined => MoleHelper.Invalid, 
			Chemistry.GasType.Oxygen => Oxygen, 
			Chemistry.GasType.Nitrogen => Nitrogen, 
			Chemistry.GasType.CarbonDioxide => CarbonDioxide, 
			Chemistry.GasType.Methane => Methane, 
			Chemistry.GasType.Pollutant => Pollutant, 
			Chemistry.GasType.Water => Water, 
			Chemistry.GasType.PollutedWater => PollutedWater, 
			Chemistry.GasType.NitrousOxide => NitrousOxide, 
			Chemistry.GasType.LiquidNitrogen => LiquidNitrogen, 
			Chemistry.GasType.LiquidOxygen => LiquidOxygen, 
			Chemistry.GasType.LiquidMethane => LiquidMethane, 
			Chemistry.GasType.Steam => Steam, 
			Chemistry.GasType.LiquidCarbonDioxide => LiquidCarbonDioxide, 
			Chemistry.GasType.LiquidPollutant => LiquidPollutant, 
			Chemistry.GasType.LiquidNitrousOxide => LiquidNitrousOxide, 
			Chemistry.GasType.Hydrogen => Hydrogen, 
			Chemistry.GasType.LiquidHydrogen => LiquidHydrogen, 
			Chemistry.GasType.Hydrazine => Hydrazine, 
			Chemistry.GasType.LiquidHydrazine => LiquidHydrazine, 
			Chemistry.GasType.LiquidAlcohol => LiquidAlcohol, 
			Chemistry.GasType.Helium => Helium, 
			Chemistry.GasType.LiquidSodiumChloride => LiquidSodiumChloride, 
			Chemistry.GasType.Silanol => Silanol, 
			Chemistry.GasType.LiquidSilanol => LiquidSilanol, 
			Chemistry.GasType.HydrochloricAcid => HydrochloricAcid, 
			Chemistry.GasType.LiquidHydrochloricAcid => LiquidHydrochloricAcid, 
			Chemistry.GasType.Ozone => Ozone, 
			Chemistry.GasType.LiquidOzone => LiquidOzone, 
			_ => MoleHelper.Invalid, 
		};
	}

	public void SetMoleValue(Chemistry.GasType gasType, MoleQuantity quantity, MoleEnergy energy)
	{
		switch (gasType)
		{
		case Chemistry.GasType.Oxygen:
			Oxygen.Set(quantity, energy);
			break;
		case Chemistry.GasType.Nitrogen:
			Nitrogen.Set(quantity, energy);
			break;
		case Chemistry.GasType.CarbonDioxide:
			CarbonDioxide.Set(quantity, energy);
			break;
		case Chemistry.GasType.Methane:
			Methane.Set(quantity, energy);
			break;
		case Chemistry.GasType.Pollutant:
			Pollutant.Set(quantity, energy);
			break;
		case Chemistry.GasType.Water:
			Water.Set(quantity, energy);
			break;
		case Chemistry.GasType.PollutedWater:
			PollutedWater.Set(quantity, energy);
			break;
		case Chemistry.GasType.NitrousOxide:
			NitrousOxide.Set(quantity, energy);
			break;
		case Chemistry.GasType.LiquidNitrogen:
			LiquidNitrogen.Set(quantity, energy);
			break;
		case Chemistry.GasType.LiquidOxygen:
			LiquidOxygen.Set(quantity, energy);
			break;
		case Chemistry.GasType.LiquidMethane:
			LiquidMethane.Set(quantity, energy);
			break;
		case Chemistry.GasType.Steam:
			Steam.Set(quantity, energy);
			break;
		case Chemistry.GasType.LiquidCarbonDioxide:
			LiquidCarbonDioxide.Set(quantity, energy);
			break;
		case Chemistry.GasType.LiquidPollutant:
			LiquidPollutant.Set(quantity, energy);
			break;
		case Chemistry.GasType.LiquidNitrousOxide:
			LiquidNitrousOxide.Set(quantity, energy);
			break;
		case Chemistry.GasType.Hydrogen:
			Hydrogen.Set(quantity, energy);
			break;
		case Chemistry.GasType.LiquidHydrogen:
			LiquidHydrogen.Set(quantity, energy);
			break;
		case Chemistry.GasType.Hydrazine:
			Hydrazine.Set(quantity, energy);
			break;
		case Chemistry.GasType.LiquidHydrazine:
			LiquidHydrazine.Set(quantity, energy);
			break;
		case Chemistry.GasType.LiquidAlcohol:
			LiquidAlcohol.Set(quantity, energy);
			break;
		case Chemistry.GasType.Helium:
			Helium.Set(quantity, energy);
			break;
		case Chemistry.GasType.LiquidSodiumChloride:
			LiquidSodiumChloride.Set(quantity, energy);
			break;
		case Chemistry.GasType.Silanol:
			Silanol.Set(quantity, energy);
			break;
		case Chemistry.GasType.LiquidSilanol:
			LiquidSilanol.Set(quantity, energy);
			break;
		case Chemistry.GasType.HydrochloricAcid:
			HydrochloricAcid.Set(quantity, energy);
			break;
		case Chemistry.GasType.LiquidHydrochloricAcid:
			LiquidHydrochloricAcid.Set(quantity, energy);
			break;
		case Chemistry.GasType.Ozone:
			Ozone.Set(quantity, energy);
			break;
		case Chemistry.GasType.LiquidOzone:
			LiquidOzone.Set(quantity, energy);
			break;
		default:
			throw new ArgumentOutOfRangeException("gasType", gasType, null);
		}
	}

	public float ThermalEfficiency()
	{
		MoleQuantity getTotalMolesGassesAndLiquids = GetTotalMolesGassesAndLiquids;
		if (getTotalMolesGassesAndLiquids <= Chemistry.MINIMUM_VALID_TOTAL_MOLES)
		{
			return 0f;
		}
		return (float)(Oxygen.ThermalEfficiency() * (Oxygen.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Nitrogen.ThermalEfficiency() * (Nitrogen.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + CarbonDioxide.ThermalEfficiency() * (CarbonDioxide.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Methane.ThermalEfficiency() * (Methane.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Pollutant.ThermalEfficiency() * (Pollutant.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Water.ThermalEfficiency() * (Water.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + PollutedWater.ThermalEfficiency() * (PollutedWater.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + NitrousOxide.ThermalEfficiency() * (NitrousOxide.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidNitrogen.ThermalEfficiency() * (LiquidNitrogen.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidOxygen.ThermalEfficiency() * (LiquidOxygen.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidMethane.ThermalEfficiency() * (LiquidMethane.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Steam.ThermalEfficiency() * (Steam.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidCarbonDioxide.ThermalEfficiency() * (LiquidCarbonDioxide.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidPollutant.ThermalEfficiency() * (LiquidPollutant.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidNitrousOxide.ThermalEfficiency() * (LiquidNitrousOxide.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Hydrogen.ThermalEfficiency() * (Hydrogen.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidHydrogen.ThermalEfficiency() * (LiquidHydrogen.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Hydrazine.ThermalEfficiency() * (Hydrazine.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidHydrazine.ThermalEfficiency() * (LiquidHydrazine.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidAlcohol.ThermalEfficiency() * (LiquidAlcohol.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Helium.ThermalEfficiency() * (Helium.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidSodiumChloride.ThermalEfficiency() * (LiquidSodiumChloride.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Silanol.ThermalEfficiency() * (Silanol.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidSilanol.ThermalEfficiency() * (LiquidSilanol.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + HydrochloricAcid.ThermalEfficiency() * (HydrochloricAcid.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidHydrochloricAcid.ThermalEfficiency() * (LiquidHydrochloricAcid.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Ozone.ThermalEfficiency() * (Ozone.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidOzone.ThermalEfficiency() * (LiquidOzone.Quantity / getTotalMolesGassesAndLiquids).ToDouble());
	}

	public void SetReadOnly(bool isReadOnly)
	{
		Oxygen.ReadOnly = isReadOnly;
		Nitrogen.ReadOnly = isReadOnly;
		CarbonDioxide.ReadOnly = isReadOnly;
		Methane.ReadOnly = isReadOnly;
		Pollutant.ReadOnly = isReadOnly;
		Water.ReadOnly = isReadOnly;
		PollutedWater.ReadOnly = isReadOnly;
		NitrousOxide.ReadOnly = isReadOnly;
		LiquidNitrogen.ReadOnly = isReadOnly;
		LiquidOxygen.ReadOnly = isReadOnly;
		LiquidMethane.ReadOnly = isReadOnly;
		Steam.ReadOnly = isReadOnly;
		LiquidCarbonDioxide.ReadOnly = isReadOnly;
		LiquidPollutant.ReadOnly = isReadOnly;
		LiquidNitrousOxide.ReadOnly = isReadOnly;
		Hydrogen.ReadOnly = isReadOnly;
		LiquidHydrogen.ReadOnly = isReadOnly;
		Hydrazine.ReadOnly = isReadOnly;
		LiquidHydrazine.ReadOnly = isReadOnly;
		LiquidAlcohol.ReadOnly = isReadOnly;
		Helium.ReadOnly = isReadOnly;
		LiquidSodiumChloride.ReadOnly = isReadOnly;
		Silanol.ReadOnly = isReadOnly;
		LiquidSilanol.ReadOnly = isReadOnly;
		HydrochloricAcid.ReadOnly = isReadOnly;
		LiquidHydrochloricAcid.ReadOnly = isReadOnly;
		Ozone.ReadOnly = isReadOnly;
		LiquidOzone.ReadOnly = isReadOnly;
	}

	public MoleEnergy GasStateEnergy()
	{
		return new MoleEnergy(Oxygen.Quantity.ToDouble() * Oxygen.LatentHeatOfVaporization() + CarbonDioxide.Quantity.ToDouble() * CarbonDioxide.LatentHeatOfVaporization() + Nitrogen.Quantity.ToDouble() * Nitrogen.LatentHeatOfVaporization() + Methane.Quantity.ToDouble() * Methane.LatentHeatOfVaporization() + Pollutant.Quantity.ToDouble() * Pollutant.LatentHeatOfVaporization() + NitrousOxide.Quantity.ToDouble() * NitrousOxide.LatentHeatOfVaporization() + Steam.Quantity.ToDouble() * Steam.LatentHeatOfVaporization() + Hydrogen.Quantity.ToDouble() * Hydrogen.LatentHeatOfVaporization() + Hydrazine.Quantity.ToDouble() * Hydrazine.LatentHeatOfVaporization() + Helium.Quantity.ToDouble() * Helium.LatentHeatOfVaporization() + Silanol.Quantity.ToDouble() * Silanol.LatentHeatOfVaporization() + HydrochloricAcid.Quantity.ToDouble() * HydrochloricAcid.LatentHeatOfVaporization() + Ozone.Quantity.ToDouble() * Ozone.LatentHeatOfVaporization());
	}

	public void EqualiseInternalEnergy()
	{
		TotalEnergy = TotalEnergy;
	}

	public MoleQuantity GetTotalMoles(AtmosphereHelper.MatterState matterState)
	{
		MoleQuantity zero = MoleQuantity.Zero;
		switch (matterState)
		{
		case AtmosphereHelper.MatterState.Liquid:
			zero += Water.Quantity;
			zero += PollutedWater.Quantity;
			zero += LiquidNitrogen.Quantity;
			zero += LiquidOxygen.Quantity;
			zero += LiquidMethane.Quantity;
			zero += LiquidCarbonDioxide.Quantity;
			zero += LiquidPollutant.Quantity;
			zero += LiquidNitrousOxide.Quantity;
			zero += LiquidHydrogen.Quantity;
			zero += LiquidHydrazine.Quantity;
			zero += LiquidAlcohol.Quantity;
			zero += LiquidSodiumChloride.Quantity;
			zero += LiquidSilanol.Quantity;
			zero += LiquidHydrochloricAcid.Quantity;
			zero += LiquidOzone.Quantity;
			break;
		case AtmosphereHelper.MatterState.Gas:
			zero += Oxygen.Quantity;
			zero += Nitrogen.Quantity;
			zero += CarbonDioxide.Quantity;
			zero += Methane.Quantity;
			zero += Pollutant.Quantity;
			zero += NitrousOxide.Quantity;
			zero += Steam.Quantity;
			zero += Hydrogen.Quantity;
			zero += Hydrazine.Quantity;
			zero += Helium.Quantity;
			zero += Silanol.Quantity;
			zero += HydrochloricAcid.Quantity;
			zero += Ozone.Quantity;
			break;
		case AtmosphereHelper.MatterState.All:
			zero += GetTotalMoles(AtmosphereHelper.MatterState.Liquid);
			zero += GetTotalMoles(AtmosphereHelper.MatterState.Gas);
			break;
		}
		return zero;
	}

	public void UpdateCache(DirtyMoleTolerance tolerance = DirtyMoleTolerance.OnePercent)
	{
		_temperatureCached = Temperature;
		Oxygen.UpdateCache(tolerance);
		CarbonDioxide.UpdateCache(tolerance);
		Nitrogen.UpdateCache(tolerance);
		Methane.UpdateCache(tolerance);
		Water.UpdateCache(tolerance);
		PollutedWater.UpdateCache(tolerance);
		Pollutant.UpdateCache(tolerance);
		NitrousOxide.UpdateCache(tolerance);
		LiquidNitrogen.UpdateCache(tolerance);
		LiquidOxygen.UpdateCache(tolerance);
		LiquidMethane.UpdateCache(tolerance);
		Steam.UpdateCache(tolerance);
		LiquidCarbonDioxide.UpdateCache(tolerance);
		LiquidPollutant.UpdateCache(tolerance);
		LiquidNitrousOxide.UpdateCache(tolerance);
		Hydrogen.UpdateCache(tolerance);
		LiquidHydrogen.UpdateCache(tolerance);
		Hydrazine.UpdateCache(tolerance);
		LiquidHydrazine.UpdateCache(tolerance);
		LiquidAlcohol.UpdateCache(tolerance);
		Helium.UpdateCache(tolerance);
		LiquidSodiumChloride.UpdateCache(tolerance);
		Silanol.UpdateCache(tolerance);
		LiquidSilanol.UpdateCache(tolerance);
		HydrochloricAcid.UpdateCache(tolerance);
		LiquidHydrochloricAcid.UpdateCache(tolerance);
		Ozone.UpdateCache(tolerance);
		LiquidOzone.UpdateCache(tolerance);
	}

	public bool SetGene(Gene gene, float value)
	{
		return false;
	}

	public void Add(GasMixture newGasMix, AtmosphereHelper.MatterState matterState = AtmosphereHelper.MatterState.All)
	{
		if (newGasMix.IsValid && !(newGasMix.GetTotalMoles(matterState) <= MoleQuantity.Zero) && !newGasMix.GetTotalMoles(matterState).IsNaN())
		{
			switch (matterState)
			{
			case AtmosphereHelper.MatterState.Liquid:
				AddLiquids(newGasMix);
				break;
			case AtmosphereHelper.MatterState.Gas:
				AddGasses(newGasMix);
				break;
			case AtmosphereHelper.MatterState.All:
				AddGasses(newGasMix);
				AddLiquids(newGasMix);
				break;
			}
		}
	}

	private void AddGasses(GasMixture newGasMix)
	{
		if (newGasMix.IsValid && !(newGasMix.GetTotalMolesGasses <= MoleQuantity.Zero) && !newGasMix.GetTotalMolesGasses.IsNaN())
		{
			Oxygen.Add(newGasMix.Oxygen);
			CarbonDioxide.Add(newGasMix.CarbonDioxide);
			Nitrogen.Add(newGasMix.Nitrogen);
			Methane.Add(newGasMix.Methane);
			Pollutant.Add(newGasMix.Pollutant);
			NitrousOxide.Add(newGasMix.NitrousOxide);
			Steam.Add(newGasMix.Steam);
			Hydrogen.Add(newGasMix.Hydrogen);
			Hydrazine.Add(newGasMix.Hydrazine);
			Helium.Add(newGasMix.Helium);
			Silanol.Add(newGasMix.Silanol);
			HydrochloricAcid.Add(newGasMix.HydrochloricAcid);
			Ozone.Add(newGasMix.Ozone);
		}
	}

	private void AddLiquids(GasMixture newGasMix)
	{
		if (newGasMix.IsValid && !(newGasMix.GetTotalMolesLiquids <= MoleQuantity.Zero) && !newGasMix.GetTotalMolesLiquids.IsNaN())
		{
			Water.Add(newGasMix.Water);
			PollutedWater.Add(newGasMix.PollutedWater);
			LiquidNitrogen.Add(newGasMix.LiquidNitrogen);
			LiquidOxygen.Add(newGasMix.LiquidOxygen);
			LiquidMethane.Add(newGasMix.LiquidMethane);
			LiquidCarbonDioxide.Add(newGasMix.LiquidCarbonDioxide);
			LiquidPollutant.Add(newGasMix.LiquidPollutant);
			LiquidNitrousOxide.Add(newGasMix.LiquidNitrousOxide);
			LiquidHydrogen.Add(newGasMix.LiquidHydrogen);
			LiquidHydrazine.Add(newGasMix.LiquidHydrazine);
			LiquidAlcohol.Add(newGasMix.LiquidAlcohol);
			LiquidSodiumChloride.Add(newGasMix.LiquidSodiumChloride);
			LiquidSilanol.Add(newGasMix.LiquidSilanol);
			LiquidHydrochloricAcid.Add(newGasMix.LiquidHydrochloricAcid);
			LiquidOzone.Add(newGasMix.LiquidOzone);
		}
	}

	public void Add(Mole mole)
	{
		if (mole.IsValid)
		{
			switch (mole.Type)
			{
			case Chemistry.GasType.Oxygen:
				Oxygen.Add(mole);
				break;
			case Chemistry.GasType.Nitrogen:
				Nitrogen.Add(mole);
				break;
			case Chemistry.GasType.CarbonDioxide:
				CarbonDioxide.Add(mole);
				break;
			case Chemistry.GasType.Methane:
				Methane.Add(mole);
				break;
			case Chemistry.GasType.Pollutant:
				Pollutant.Add(mole);
				break;
			case Chemistry.GasType.Water:
				Water.Add(mole);
				break;
			case Chemistry.GasType.PollutedWater:
				PollutedWater.Add(mole);
				break;
			case Chemistry.GasType.NitrousOxide:
				NitrousOxide.Add(mole);
				break;
			case Chemistry.GasType.LiquidNitrogen:
				LiquidNitrogen.Add(mole);
				break;
			case Chemistry.GasType.LiquidOxygen:
				LiquidOxygen.Add(mole);
				break;
			case Chemistry.GasType.LiquidMethane:
				LiquidMethane.Add(mole);
				break;
			case Chemistry.GasType.Steam:
				Steam.Add(mole);
				break;
			case Chemistry.GasType.LiquidCarbonDioxide:
				LiquidCarbonDioxide.Add(mole);
				break;
			case Chemistry.GasType.LiquidPollutant:
				LiquidPollutant.Add(mole);
				break;
			case Chemistry.GasType.LiquidNitrousOxide:
				LiquidNitrousOxide.Add(mole);
				break;
			case Chemistry.GasType.Hydrogen:
				Hydrogen.Add(mole);
				break;
			case Chemistry.GasType.LiquidHydrogen:
				LiquidHydrogen.Add(mole);
				break;
			case Chemistry.GasType.Hydrazine:
				Hydrazine.Add(mole);
				break;
			case Chemistry.GasType.LiquidHydrazine:
				LiquidHydrazine.Add(mole);
				break;
			case Chemistry.GasType.LiquidAlcohol:
				LiquidAlcohol.Add(mole);
				break;
			case Chemistry.GasType.Helium:
				Helium.Add(mole);
				break;
			case Chemistry.GasType.LiquidSodiumChloride:
				LiquidSodiumChloride.Add(mole);
				break;
			case Chemistry.GasType.Silanol:
				Silanol.Add(mole);
				break;
			case Chemistry.GasType.LiquidSilanol:
				LiquidSilanol.Add(mole);
				break;
			case Chemistry.GasType.HydrochloricAcid:
				HydrochloricAcid.Add(mole);
				break;
			case Chemistry.GasType.LiquidHydrochloricAcid:
				LiquidHydrochloricAcid.Add(mole);
				break;
			case Chemistry.GasType.Ozone:
				Ozone.Add(mole);
				break;
			case Chemistry.GasType.LiquidOzone:
				LiquidOzone.Add(mole);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			case Chemistry.GasType.Undefined:
				break;
			}
		}
	}

	public void Set(GasMixture gasMixture, AtmosphereHelper.MatterState matterState = AtmosphereHelper.MatterState.All)
	{
		if (gasMixture.IsValid)
		{
			switch (matterState)
			{
			case AtmosphereHelper.MatterState.Liquid:
				Water.Set(gasMixture.Water);
				PollutedWater.Set(gasMixture.PollutedWater);
				LiquidNitrogen.Set(gasMixture.LiquidNitrogen);
				LiquidOxygen.Set(gasMixture.LiquidOxygen);
				LiquidMethane.Set(gasMixture.LiquidMethane);
				LiquidCarbonDioxide.Set(gasMixture.LiquidCarbonDioxide);
				LiquidPollutant.Set(gasMixture.LiquidPollutant);
				LiquidNitrousOxide.Set(gasMixture.LiquidNitrousOxide);
				LiquidHydrogen.Set(gasMixture.LiquidHydrogen);
				LiquidHydrazine.Set(gasMixture.LiquidHydrazine);
				LiquidAlcohol.Set(gasMixture.LiquidAlcohol);
				LiquidSodiumChloride.Set(gasMixture.LiquidSodiumChloride);
				LiquidSilanol.Set(gasMixture.LiquidSilanol);
				LiquidHydrochloricAcid.Set(gasMixture.LiquidHydrochloricAcid);
				LiquidOzone.Set(gasMixture.LiquidOzone);
				break;
			case AtmosphereHelper.MatterState.Gas:
				Oxygen.Set(gasMixture.Oxygen);
				Nitrogen.Set(gasMixture.Nitrogen);
				CarbonDioxide.Set(gasMixture.CarbonDioxide);
				Methane.Set(gasMixture.Methane);
				Pollutant.Set(gasMixture.Pollutant);
				NitrousOxide.Set(gasMixture.NitrousOxide);
				Steam.Set(gasMixture.Steam);
				Hydrogen.Set(gasMixture.Hydrogen);
				Hydrazine.Set(gasMixture.Hydrazine);
				Helium.Set(gasMixture.Helium);
				Silanol.Set(gasMixture.Silanol);
				HydrochloricAcid.Set(gasMixture.HydrochloricAcid);
				Ozone.Set(gasMixture.Ozone);
				break;
			case AtmosphereHelper.MatterState.All:
				Oxygen.Set(gasMixture.Oxygen);
				Nitrogen.Set(gasMixture.Nitrogen);
				CarbonDioxide.Set(gasMixture.CarbonDioxide);
				Methane.Set(gasMixture.Methane);
				Pollutant.Set(gasMixture.Pollutant);
				NitrousOxide.Set(gasMixture.NitrousOxide);
				Steam.Set(gasMixture.Steam);
				Hydrogen.Set(gasMixture.Hydrogen);
				Hydrazine.Set(gasMixture.Hydrazine);
				Helium.Set(gasMixture.Helium);
				Silanol.Set(gasMixture.Silanol);
				HydrochloricAcid.Set(gasMixture.HydrochloricAcid);
				Ozone.Set(gasMixture.Ozone);
				Water.Set(gasMixture.Water);
				PollutedWater.Set(gasMixture.PollutedWater);
				LiquidNitrogen.Set(gasMixture.LiquidNitrogen);
				LiquidOxygen.Set(gasMixture.LiquidOxygen);
				LiquidMethane.Set(gasMixture.LiquidMethane);
				LiquidCarbonDioxide.Set(gasMixture.LiquidCarbonDioxide);
				LiquidPollutant.Set(gasMixture.LiquidPollutant);
				LiquidNitrousOxide.Set(gasMixture.LiquidNitrousOxide);
				LiquidHydrogen.Set(gasMixture.LiquidHydrogen);
				LiquidHydrazine.Set(gasMixture.LiquidHydrazine);
				LiquidAlcohol.Set(gasMixture.LiquidAlcohol);
				LiquidSodiumChloride.Set(gasMixture.LiquidSodiumChloride);
				LiquidSilanol.Set(gasMixture.LiquidSilanol);
				LiquidHydrochloricAcid.Set(gasMixture.LiquidHydrochloricAcid);
				LiquidOzone.Set(gasMixture.LiquidOzone);
				break;
			}
			UpdateCache();
		}
	}

	public void Scale(double ratio, AtmosphereHelper.MatterState matterState = AtmosphereHelper.MatterState.All)
	{
		switch (matterState)
		{
		case AtmosphereHelper.MatterState.Liquid:
			ScaleLiquids(ratio);
			break;
		case AtmosphereHelper.MatterState.Gas:
			ScaleGasses(ratio);
			break;
		case AtmosphereHelper.MatterState.All:
			ScaleLiquids(ratio);
			ScaleGasses(ratio);
			break;
		}
	}

	private void ScaleGasses(double ratio)
	{
		Oxygen.Scale(ratio);
		Nitrogen.Scale(ratio);
		CarbonDioxide.Scale(ratio);
		Methane.Scale(ratio);
		Pollutant.Scale(ratio);
		NitrousOxide.Scale(ratio);
		Steam.Scale(ratio);
		Hydrogen.Scale(ratio);
		Hydrazine.Scale(ratio);
		Helium.Scale(ratio);
		Silanol.Scale(ratio);
		HydrochloricAcid.Scale(ratio);
		Ozone.Scale(ratio);
	}

	private void ScaleLiquids(double ratio)
	{
		Water.Scale(ratio);
		PollutedWater.Scale(ratio);
		LiquidNitrogen.Scale(ratio);
		LiquidOxygen.Scale(ratio);
		LiquidMethane.Scale(ratio);
		LiquidCarbonDioxide.Scale(ratio);
		LiquidPollutant.Scale(ratio);
		LiquidNitrousOxide.Scale(ratio);
		LiquidHydrogen.Scale(ratio);
		LiquidHydrazine.Scale(ratio);
		LiquidAlcohol.Scale(ratio);
		LiquidSodiumChloride.Scale(ratio);
		LiquidSilanol.Scale(ratio);
		LiquidHydrochloricAcid.Scale(ratio);
		LiquidOzone.Scale(ratio);
	}

	public MoleQuantity GetGasMoles(Chemistry.GasType gasType)
	{
		return gasType switch
		{
			Chemistry.GasType.Undefined => GetTotalMolesGassesAndLiquids, 
			Chemistry.GasType.Oxygen => Oxygen.Quantity, 
			Chemistry.GasType.Nitrogen => Nitrogen.Quantity, 
			Chemistry.GasType.CarbonDioxide => CarbonDioxide.Quantity, 
			Chemistry.GasType.Methane => Methane.Quantity, 
			Chemistry.GasType.Pollutant => Pollutant.Quantity, 
			Chemistry.GasType.Water => Water.Quantity, 
			Chemistry.GasType.PollutedWater => PollutedWater.Quantity, 
			Chemistry.GasType.NitrousOxide => NitrousOxide.Quantity, 
			Chemistry.GasType.LiquidNitrogen => LiquidNitrogen.Quantity, 
			Chemistry.GasType.LiquidOxygen => LiquidOxygen.Quantity, 
			Chemistry.GasType.LiquidMethane => LiquidMethane.Quantity, 
			Chemistry.GasType.Steam => Steam.Quantity, 
			Chemistry.GasType.LiquidCarbonDioxide => LiquidCarbonDioxide.Quantity, 
			Chemistry.GasType.LiquidPollutant => LiquidPollutant.Quantity, 
			Chemistry.GasType.LiquidNitrousOxide => LiquidNitrousOxide.Quantity, 
			Chemistry.GasType.Hydrogen => Hydrogen.Quantity, 
			Chemistry.GasType.LiquidHydrogen => LiquidHydrogen.Quantity, 
			Chemistry.GasType.Hydrazine => Hydrazine.Quantity, 
			Chemistry.GasType.LiquidHydrazine => LiquidHydrazine.Quantity, 
			Chemistry.GasType.LiquidAlcohol => LiquidAlcohol.Quantity, 
			Chemistry.GasType.Helium => Helium.Quantity, 
			Chemistry.GasType.LiquidSodiumChloride => LiquidSodiumChloride.Quantity, 
			Chemistry.GasType.Silanol => Silanol.Quantity, 
			Chemistry.GasType.LiquidSilanol => LiquidSilanol.Quantity, 
			Chemistry.GasType.HydrochloricAcid => HydrochloricAcid.Quantity, 
			Chemistry.GasType.LiquidHydrochloricAcid => LiquidHydrochloricAcid.Quantity, 
			Chemistry.GasType.Ozone => Ozone.Quantity, 
			Chemistry.GasType.LiquidOzone => LiquidOzone.Quantity, 
			_ => throw new ArgumentOutOfRangeException("gasType", gasType, null), 
		};
	}

	public Slot GetSlot(int slotIndex)
	{
		throw new NotImplementedException();
	}

	public Slot GetNextFreeSlot(string slotId)
	{
		throw new NotImplementedException();
	}

	public float GetGasTypeRatio(Chemistry.GasType gasType)
	{
		MoleQuantity getTotalMolesGassesAndLiquids = GetTotalMolesGassesAndLiquids;
		if (getTotalMolesGassesAndLiquids.Equals(MoleQuantity.Zero))
		{
			return 0f;
		}
		return gasType switch
		{
			Chemistry.GasType.Oxygen => (Oxygen.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.Nitrogen => (Nitrogen.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.CarbonDioxide => (CarbonDioxide.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.Methane => (Methane.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.Pollutant => (Pollutant.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.Water => (Water.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.PollutedWater => (PollutedWater.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.NitrousOxide => (NitrousOxide.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.LiquidNitrogen => (LiquidNitrogen.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.LiquidOxygen => (LiquidOxygen.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.LiquidMethane => (LiquidMethane.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.Steam => (Steam.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.LiquidCarbonDioxide => (LiquidCarbonDioxide.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.LiquidPollutant => (LiquidPollutant.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.LiquidNitrousOxide => (LiquidNitrousOxide.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.Hydrogen => (Hydrogen.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.LiquidHydrogen => (LiquidHydrogen.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.Hydrazine => (Hydrazine.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.LiquidHydrazine => (LiquidHydrazine.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.LiquidAlcohol => (LiquidAlcohol.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.Helium => (Helium.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.LiquidSodiumChloride => (LiquidSodiumChloride.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.Silanol => (Silanol.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.LiquidSilanol => (LiquidSilanol.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.HydrochloricAcid => (HydrochloricAcid.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.LiquidHydrochloricAcid => (LiquidHydrochloricAcid.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.Ozone => (Ozone.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			Chemistry.GasType.LiquidOzone => (LiquidOzone.Quantity / getTotalMolesGassesAndLiquids).ToFloat(), 
			_ => throw new ArgumentOutOfRangeException("gasType", gasType, null), 
		};
	}

	public void Divide(float divideBy, AtmosphereHelper.MatterState matterState = AtmosphereHelper.MatterState.All)
	{
		switch (matterState)
		{
		case AtmosphereHelper.MatterState.Liquid:
			DivideLiquids(divideBy);
			break;
		case AtmosphereHelper.MatterState.Gas:
			DivideGasses(divideBy);
			break;
		case AtmosphereHelper.MatterState.All:
			DivideGasses(divideBy);
			DivideLiquids(divideBy);
			break;
		}
	}

	private void DivideGasses(float divideBy)
	{
		Oxygen.Split(divideBy);
		CarbonDioxide.Split(divideBy);
		Nitrogen.Split(divideBy);
		Methane.Split(divideBy);
		Pollutant.Split(divideBy);
		NitrousOxide.Split(divideBy);
		Steam.Split(divideBy);
		Hydrogen.Split(divideBy);
		Hydrazine.Split(divideBy);
		Helium.Split(divideBy);
		Silanol.Split(divideBy);
		HydrochloricAcid.Split(divideBy);
		Ozone.Split(divideBy);
	}

	private void DivideLiquids(float divideBy)
	{
		Water.Split(divideBy);
		PollutedWater.Split(divideBy);
		LiquidNitrogen.Split(divideBy);
		LiquidOxygen.Split(divideBy);
		LiquidMethane.Split(divideBy);
		LiquidCarbonDioxide.Split(divideBy);
		LiquidPollutant.Split(divideBy);
		LiquidNitrousOxide.Split(divideBy);
		LiquidHydrogen.Split(divideBy);
		LiquidHydrazine.Split(divideBy);
		LiquidAlcohol.Split(divideBy);
		LiquidSodiumChloride.Split(divideBy);
		LiquidSilanol.Split(divideBy);
		LiquidHydrochloricAcid.Split(divideBy);
		LiquidOzone.Split(divideBy);
	}

	public GasMixture RemoveAndReturn(GasMixture gasMixture, AtmosphereHelper.MatterState matterState)
	{
		if (!gasMixture.IsValid)
		{
			return GasMixtureHelper.Invalid;
		}
		GasMixture result = GasMixtureHelper.Create();
		switch (matterState)
		{
		case AtmosphereHelper.MatterState.Liquid:
			result.Water.Set(Water.Remove(gasMixture.Water.Quantity));
			result.PollutedWater.Set(PollutedWater.Remove(gasMixture.PollutedWater.Quantity));
			result.LiquidNitrogen.Set(LiquidNitrogen.Remove(gasMixture.LiquidNitrogen.Quantity));
			result.LiquidOxygen.Set(LiquidOxygen.Remove(gasMixture.LiquidOxygen.Quantity));
			result.LiquidMethane.Set(LiquidMethane.Remove(gasMixture.LiquidMethane.Quantity));
			result.LiquidCarbonDioxide.Set(LiquidCarbonDioxide.Remove(gasMixture.LiquidCarbonDioxide.Quantity));
			result.LiquidPollutant.Set(LiquidPollutant.Remove(gasMixture.LiquidPollutant.Quantity));
			result.LiquidNitrousOxide.Set(LiquidNitrousOxide.Remove(gasMixture.LiquidNitrousOxide.Quantity));
			result.LiquidHydrogen.Set(LiquidHydrogen.Remove(gasMixture.LiquidHydrogen.Quantity));
			result.LiquidHydrazine.Set(LiquidHydrazine.Remove(gasMixture.LiquidHydrazine.Quantity));
			result.LiquidAlcohol.Set(LiquidAlcohol.Remove(gasMixture.LiquidAlcohol.Quantity));
			result.LiquidSodiumChloride.Set(LiquidSodiumChloride.Remove(gasMixture.LiquidSodiumChloride.Quantity));
			result.LiquidSilanol.Set(LiquidSilanol.Remove(gasMixture.LiquidSilanol.Quantity));
			result.LiquidHydrochloricAcid.Set(LiquidHydrochloricAcid.Remove(gasMixture.LiquidHydrochloricAcid.Quantity));
			result.LiquidOzone.Set(LiquidOzone.Remove(gasMixture.LiquidOzone.Quantity));
			break;
		case AtmosphereHelper.MatterState.Gas:
			result.Oxygen.Set(Oxygen.Remove(gasMixture.Oxygen.Quantity));
			result.Nitrogen.Set(Nitrogen.Remove(gasMixture.Nitrogen.Quantity));
			result.CarbonDioxide.Set(CarbonDioxide.Remove(gasMixture.CarbonDioxide.Quantity));
			result.Methane.Set(Methane.Remove(gasMixture.Methane.Quantity));
			result.Pollutant.Set(Pollutant.Remove(gasMixture.Pollutant.Quantity));
			result.NitrousOxide.Set(NitrousOxide.Remove(gasMixture.NitrousOxide.Quantity));
			result.Steam.Set(Steam.Remove(gasMixture.Steam.Quantity));
			result.Hydrogen.Set(Hydrogen.Remove(gasMixture.Hydrogen.Quantity));
			result.Hydrazine.Set(Hydrazine.Remove(gasMixture.Hydrazine.Quantity));
			result.Helium.Set(Helium.Remove(gasMixture.Helium.Quantity));
			result.Silanol.Set(Silanol.Remove(gasMixture.Silanol.Quantity));
			result.HydrochloricAcid.Set(HydrochloricAcid.Remove(gasMixture.HydrochloricAcid.Quantity));
			result.Ozone.Set(Ozone.Remove(gasMixture.Ozone.Quantity));
			break;
		case AtmosphereHelper.MatterState.All:
			result.Oxygen.Set(Oxygen.Remove(gasMixture.Oxygen.Quantity));
			result.Nitrogen.Set(Nitrogen.Remove(gasMixture.Nitrogen.Quantity));
			result.CarbonDioxide.Set(CarbonDioxide.Remove(gasMixture.CarbonDioxide.Quantity));
			result.Methane.Set(Methane.Remove(gasMixture.Methane.Quantity));
			result.Pollutant.Set(Pollutant.Remove(gasMixture.Pollutant.Quantity));
			result.NitrousOxide.Set(NitrousOxide.Remove(gasMixture.NitrousOxide.Quantity));
			result.Steam.Set(Steam.Remove(gasMixture.Steam.Quantity));
			result.Hydrogen.Set(Hydrogen.Remove(gasMixture.Hydrogen.Quantity));
			result.Hydrazine.Set(Hydrazine.Remove(gasMixture.Hydrazine.Quantity));
			result.Helium.Set(Helium.Remove(gasMixture.Helium.Quantity));
			result.Silanol.Set(Silanol.Remove(gasMixture.Silanol.Quantity));
			result.HydrochloricAcid.Set(HydrochloricAcid.Remove(gasMixture.HydrochloricAcid.Quantity));
			result.Ozone.Set(Ozone.Remove(gasMixture.Ozone.Quantity));
			result.Water.Set(Water.Remove(gasMixture.Water.Quantity));
			result.PollutedWater.Set(PollutedWater.Remove(gasMixture.PollutedWater.Quantity));
			result.LiquidNitrogen.Set(LiquidNitrogen.Remove(gasMixture.LiquidNitrogen.Quantity));
			result.LiquidOxygen.Set(LiquidOxygen.Remove(gasMixture.LiquidOxygen.Quantity));
			result.LiquidMethane.Set(LiquidMethane.Remove(gasMixture.LiquidMethane.Quantity));
			result.LiquidCarbonDioxide.Set(LiquidCarbonDioxide.Remove(gasMixture.LiquidCarbonDioxide.Quantity));
			result.LiquidPollutant.Set(LiquidPollutant.Remove(gasMixture.LiquidPollutant.Quantity));
			result.LiquidNitrousOxide.Set(LiquidNitrousOxide.Remove(gasMixture.LiquidNitrousOxide.Quantity));
			result.LiquidHydrogen.Set(LiquidHydrogen.Remove(gasMixture.LiquidHydrogen.Quantity));
			result.LiquidHydrazine.Set(LiquidHydrazine.Remove(gasMixture.LiquidHydrazine.Quantity));
			result.LiquidAlcohol.Set(LiquidAlcohol.Remove(gasMixture.LiquidAlcohol.Quantity));
			result.LiquidSodiumChloride.Set(LiquidSodiumChloride.Remove(gasMixture.LiquidSodiumChloride.Quantity));
			result.LiquidSilanol.Set(LiquidSilanol.Remove(gasMixture.LiquidSilanol.Quantity));
			result.LiquidHydrochloricAcid.Set(LiquidHydrochloricAcid.Remove(gasMixture.LiquidHydrochloricAcid.Quantity));
			result.LiquidOzone.Set(LiquidOzone.Remove(gasMixture.LiquidOzone.Quantity));
			break;
		}
		return result;
	}

	public void Remove(GasMixture gasMixture)
	{
		if (gasMixture.IsValid)
		{
			Oxygen.Remove(gasMixture.Oxygen.Quantity);
			Nitrogen.Remove(gasMixture.Nitrogen.Quantity);
			CarbonDioxide.Remove(gasMixture.CarbonDioxide.Quantity);
			Methane.Remove(gasMixture.Methane.Quantity);
			Pollutant.Remove(gasMixture.Pollutant.Quantity);
			Water.Remove(gasMixture.Water.Quantity);
			PollutedWater.Remove(gasMixture.PollutedWater.Quantity);
			NitrousOxide.Remove(gasMixture.NitrousOxide.Quantity);
			LiquidNitrogen.Remove(gasMixture.LiquidNitrogen.Quantity);
			LiquidOxygen.Remove(gasMixture.LiquidOxygen.Quantity);
			LiquidMethane.Remove(gasMixture.LiquidMethane.Quantity);
			Steam.Remove(gasMixture.Steam.Quantity);
			LiquidCarbonDioxide.Remove(gasMixture.LiquidCarbonDioxide.Quantity);
			LiquidPollutant.Remove(gasMixture.LiquidPollutant.Quantity);
			LiquidNitrousOxide.Remove(gasMixture.LiquidNitrousOxide.Quantity);
			Hydrogen.Remove(gasMixture.Hydrogen.Quantity);
			LiquidHydrogen.Remove(gasMixture.LiquidHydrogen.Quantity);
			Hydrazine.Remove(gasMixture.Hydrazine.Quantity);
			LiquidHydrazine.Remove(gasMixture.LiquidHydrazine.Quantity);
			LiquidAlcohol.Remove(gasMixture.LiquidAlcohol.Quantity);
			Helium.Remove(gasMixture.Helium.Quantity);
			LiquidSodiumChloride.Remove(gasMixture.LiquidSodiumChloride.Quantity);
			Silanol.Remove(gasMixture.Silanol.Quantity);
			LiquidSilanol.Remove(gasMixture.LiquidSilanol.Quantity);
			HydrochloricAcid.Remove(gasMixture.HydrochloricAcid.Quantity);
			LiquidHydrochloricAcid.Remove(gasMixture.LiquidHydrochloricAcid.Quantity);
			Ozone.Remove(gasMixture.Ozone.Quantity);
			LiquidOzone.Remove(gasMixture.LiquidOzone.Quantity);
		}
	}

	public GasMixture Remove(MoleQuantity totalMolesRemoved, AtmosphereHelper.MatterState stateToRemove)
	{
		MoleQuantity totalMoles = GetTotalMoles(stateToRemove);
		MoleQuantity moleQuantity = RocketMath.Min(totalMoles, totalMolesRemoved);
		if (moleQuantity <= MoleQuantity.Zero)
		{
			return GasMixtureHelper.Invalid;
		}
		GasMixture result = GasMixtureHelper.Create();
		MoleQuantity moleQuantity2 = moleQuantity / totalMoles;
		switch (stateToRemove)
		{
		case AtmosphereHelper.MatterState.Liquid:
			result.Add(Water.Remove(Water.Quantity * moleQuantity2));
			result.Add(PollutedWater.Remove(PollutedWater.Quantity * moleQuantity2));
			result.Add(LiquidNitrogen.Remove(LiquidNitrogen.Quantity * moleQuantity2));
			result.Add(LiquidOxygen.Remove(LiquidOxygen.Quantity * moleQuantity2));
			result.Add(LiquidMethane.Remove(LiquidMethane.Quantity * moleQuantity2));
			result.Add(LiquidCarbonDioxide.Remove(LiquidCarbonDioxide.Quantity * moleQuantity2));
			result.Add(LiquidPollutant.Remove(LiquidPollutant.Quantity * moleQuantity2));
			result.Add(LiquidNitrousOxide.Remove(LiquidNitrousOxide.Quantity * moleQuantity2));
			result.Add(LiquidHydrogen.Remove(LiquidHydrogen.Quantity * moleQuantity2));
			result.Add(LiquidHydrazine.Remove(LiquidHydrazine.Quantity * moleQuantity2));
			result.Add(LiquidAlcohol.Remove(LiquidAlcohol.Quantity * moleQuantity2));
			result.Add(LiquidSodiumChloride.Remove(LiquidSodiumChloride.Quantity * moleQuantity2));
			result.Add(LiquidSilanol.Remove(LiquidSilanol.Quantity * moleQuantity2));
			result.Add(LiquidHydrochloricAcid.Remove(LiquidHydrochloricAcid.Quantity * moleQuantity2));
			result.Add(LiquidOzone.Remove(LiquidOzone.Quantity * moleQuantity2));
			break;
		case AtmosphereHelper.MatterState.Gas:
			result.Add(Oxygen.Remove(Oxygen.Quantity * moleQuantity2));
			result.Add(Nitrogen.Remove(Nitrogen.Quantity * moleQuantity2));
			result.Add(CarbonDioxide.Remove(CarbonDioxide.Quantity * moleQuantity2));
			result.Add(Methane.Remove(Methane.Quantity * moleQuantity2));
			result.Add(Pollutant.Remove(Pollutant.Quantity * moleQuantity2));
			result.Add(NitrousOxide.Remove(NitrousOxide.Quantity * moleQuantity2));
			result.Add(Steam.Remove(Steam.Quantity * moleQuantity2));
			result.Add(Hydrogen.Remove(Hydrogen.Quantity * moleQuantity2));
			result.Add(Hydrazine.Remove(Hydrazine.Quantity * moleQuantity2));
			result.Add(Helium.Remove(Helium.Quantity * moleQuantity2));
			result.Add(Silanol.Remove(Silanol.Quantity * moleQuantity2));
			result.Add(HydrochloricAcid.Remove(HydrochloricAcid.Quantity * moleQuantity2));
			result.Add(Ozone.Remove(Ozone.Quantity * moleQuantity2));
			break;
		case AtmosphereHelper.MatterState.All:
			result.Add(Oxygen.Remove(Oxygen.Quantity * moleQuantity2));
			result.Add(Nitrogen.Remove(Nitrogen.Quantity * moleQuantity2));
			result.Add(CarbonDioxide.Remove(CarbonDioxide.Quantity * moleQuantity2));
			result.Add(Methane.Remove(Methane.Quantity * moleQuantity2));
			result.Add(Pollutant.Remove(Pollutant.Quantity * moleQuantity2));
			result.Add(NitrousOxide.Remove(NitrousOxide.Quantity * moleQuantity2));
			result.Add(Steam.Remove(Steam.Quantity * moleQuantity2));
			result.Add(Hydrogen.Remove(Hydrogen.Quantity * moleQuantity2));
			result.Add(Hydrazine.Remove(Hydrazine.Quantity * moleQuantity2));
			result.Add(Helium.Remove(Helium.Quantity * moleQuantity2));
			result.Add(Silanol.Remove(Silanol.Quantity * moleQuantity2));
			result.Add(HydrochloricAcid.Remove(HydrochloricAcid.Quantity * moleQuantity2));
			result.Add(Ozone.Remove(Ozone.Quantity * moleQuantity2));
			result.Add(Water.Remove(Water.Quantity * moleQuantity2));
			result.Add(PollutedWater.Remove(PollutedWater.Quantity * moleQuantity2));
			result.Add(LiquidNitrogen.Remove(LiquidNitrogen.Quantity * moleQuantity2));
			result.Add(LiquidOxygen.Remove(LiquidOxygen.Quantity * moleQuantity2));
			result.Add(LiquidMethane.Remove(LiquidMethane.Quantity * moleQuantity2));
			result.Add(LiquidCarbonDioxide.Remove(LiquidCarbonDioxide.Quantity * moleQuantity2));
			result.Add(LiquidPollutant.Remove(LiquidPollutant.Quantity * moleQuantity2));
			result.Add(LiquidNitrousOxide.Remove(LiquidNitrousOxide.Quantity * moleQuantity2));
			result.Add(LiquidHydrogen.Remove(LiquidHydrogen.Quantity * moleQuantity2));
			result.Add(LiquidHydrazine.Remove(LiquidHydrazine.Quantity * moleQuantity2));
			result.Add(LiquidAlcohol.Remove(LiquidAlcohol.Quantity * moleQuantity2));
			result.Add(LiquidSodiumChloride.Remove(LiquidSodiumChloride.Quantity * moleQuantity2));
			result.Add(LiquidSilanol.Remove(LiquidSilanol.Quantity * moleQuantity2));
			result.Add(LiquidHydrochloricAcid.Remove(LiquidHydrochloricAcid.Quantity * moleQuantity2));
			result.Add(LiquidOzone.Remove(LiquidOzone.Quantity * moleQuantity2));
			break;
		}
		return result;
	}

	public void RemoveInto(MoleQuantity totalMolesRemoved, AtmosphereHelper.MatterState stateToRemove, ref GasMixture destination)
	{
		MoleQuantity totalMoles = GetTotalMoles(stateToRemove);
		MoleQuantity moleQuantity = RocketMath.Min(totalMoles, totalMolesRemoved);
		if (!(moleQuantity <= MoleQuantity.Zero))
		{
			MoleQuantity moleQuantity2 = moleQuantity / totalMoles;
			switch (stateToRemove)
			{
			case AtmosphereHelper.MatterState.Liquid:
				destination.Water.Add(Water.Remove(Water.Quantity * moleQuantity2));
				destination.PollutedWater.Add(PollutedWater.Remove(PollutedWater.Quantity * moleQuantity2));
				destination.LiquidNitrogen.Add(LiquidNitrogen.Remove(LiquidNitrogen.Quantity * moleQuantity2));
				destination.LiquidOxygen.Add(LiquidOxygen.Remove(LiquidOxygen.Quantity * moleQuantity2));
				destination.LiquidMethane.Add(LiquidMethane.Remove(LiquidMethane.Quantity * moleQuantity2));
				destination.LiquidCarbonDioxide.Add(LiquidCarbonDioxide.Remove(LiquidCarbonDioxide.Quantity * moleQuantity2));
				destination.LiquidPollutant.Add(LiquidPollutant.Remove(LiquidPollutant.Quantity * moleQuantity2));
				destination.LiquidNitrousOxide.Add(LiquidNitrousOxide.Remove(LiquidNitrousOxide.Quantity * moleQuantity2));
				destination.LiquidHydrogen.Add(LiquidHydrogen.Remove(LiquidHydrogen.Quantity * moleQuantity2));
				destination.LiquidHydrazine.Add(LiquidHydrazine.Remove(LiquidHydrazine.Quantity * moleQuantity2));
				destination.LiquidAlcohol.Add(LiquidAlcohol.Remove(LiquidAlcohol.Quantity * moleQuantity2));
				destination.LiquidSodiumChloride.Add(LiquidSodiumChloride.Remove(LiquidSodiumChloride.Quantity * moleQuantity2));
				destination.LiquidSilanol.Add(LiquidSilanol.Remove(LiquidSilanol.Quantity * moleQuantity2));
				destination.LiquidHydrochloricAcid.Add(LiquidHydrochloricAcid.Remove(LiquidHydrochloricAcid.Quantity * moleQuantity2));
				destination.LiquidOzone.Add(LiquidOzone.Remove(LiquidOzone.Quantity * moleQuantity2));
				break;
			case AtmosphereHelper.MatterState.Gas:
				destination.Oxygen.Add(Oxygen.Remove(Oxygen.Quantity * moleQuantity2));
				destination.Nitrogen.Add(Nitrogen.Remove(Nitrogen.Quantity * moleQuantity2));
				destination.CarbonDioxide.Add(CarbonDioxide.Remove(CarbonDioxide.Quantity * moleQuantity2));
				destination.Methane.Add(Methane.Remove(Methane.Quantity * moleQuantity2));
				destination.Pollutant.Add(Pollutant.Remove(Pollutant.Quantity * moleQuantity2));
				destination.NitrousOxide.Add(NitrousOxide.Remove(NitrousOxide.Quantity * moleQuantity2));
				destination.Steam.Add(Steam.Remove(Steam.Quantity * moleQuantity2));
				destination.Hydrogen.Add(Hydrogen.Remove(Hydrogen.Quantity * moleQuantity2));
				destination.Hydrazine.Add(Hydrazine.Remove(Hydrazine.Quantity * moleQuantity2));
				destination.Helium.Add(Helium.Remove(Helium.Quantity * moleQuantity2));
				destination.Silanol.Add(Silanol.Remove(Silanol.Quantity * moleQuantity2));
				destination.HydrochloricAcid.Add(HydrochloricAcid.Remove(HydrochloricAcid.Quantity * moleQuantity2));
				destination.Ozone.Add(Ozone.Remove(Ozone.Quantity * moleQuantity2));
				break;
			case AtmosphereHelper.MatterState.All:
				destination.Oxygen.Add(Oxygen.Remove(Oxygen.Quantity * moleQuantity2));
				destination.Nitrogen.Add(Nitrogen.Remove(Nitrogen.Quantity * moleQuantity2));
				destination.CarbonDioxide.Add(CarbonDioxide.Remove(CarbonDioxide.Quantity * moleQuantity2));
				destination.Methane.Add(Methane.Remove(Methane.Quantity * moleQuantity2));
				destination.Pollutant.Add(Pollutant.Remove(Pollutant.Quantity * moleQuantity2));
				destination.NitrousOxide.Add(NitrousOxide.Remove(NitrousOxide.Quantity * moleQuantity2));
				destination.Steam.Add(Steam.Remove(Steam.Quantity * moleQuantity2));
				destination.Hydrogen.Add(Hydrogen.Remove(Hydrogen.Quantity * moleQuantity2));
				destination.Hydrazine.Add(Hydrazine.Remove(Hydrazine.Quantity * moleQuantity2));
				destination.Helium.Add(Helium.Remove(Helium.Quantity * moleQuantity2));
				destination.Silanol.Add(Silanol.Remove(Silanol.Quantity * moleQuantity2));
				destination.HydrochloricAcid.Add(HydrochloricAcid.Remove(HydrochloricAcid.Quantity * moleQuantity2));
				destination.Ozone.Add(Ozone.Remove(Ozone.Quantity * moleQuantity2));
				destination.Water.Add(Water.Remove(Water.Quantity * moleQuantity2));
				destination.PollutedWater.Add(PollutedWater.Remove(PollutedWater.Quantity * moleQuantity2));
				destination.LiquidNitrogen.Add(LiquidNitrogen.Remove(LiquidNitrogen.Quantity * moleQuantity2));
				destination.LiquidOxygen.Add(LiquidOxygen.Remove(LiquidOxygen.Quantity * moleQuantity2));
				destination.LiquidMethane.Add(LiquidMethane.Remove(LiquidMethane.Quantity * moleQuantity2));
				destination.LiquidCarbonDioxide.Add(LiquidCarbonDioxide.Remove(LiquidCarbonDioxide.Quantity * moleQuantity2));
				destination.LiquidPollutant.Add(LiquidPollutant.Remove(LiquidPollutant.Quantity * moleQuantity2));
				destination.LiquidNitrousOxide.Add(LiquidNitrousOxide.Remove(LiquidNitrousOxide.Quantity * moleQuantity2));
				destination.LiquidHydrogen.Add(LiquidHydrogen.Remove(LiquidHydrogen.Quantity * moleQuantity2));
				destination.LiquidHydrazine.Add(LiquidHydrazine.Remove(LiquidHydrazine.Quantity * moleQuantity2));
				destination.LiquidAlcohol.Add(LiquidAlcohol.Remove(LiquidAlcohol.Quantity * moleQuantity2));
				destination.LiquidSodiumChloride.Add(LiquidSodiumChloride.Remove(LiquidSodiumChloride.Quantity * moleQuantity2));
				destination.LiquidSilanol.Add(LiquidSilanol.Remove(LiquidSilanol.Quantity * moleQuantity2));
				destination.LiquidHydrochloricAcid.Add(LiquidHydrochloricAcid.Remove(LiquidHydrochloricAcid.Quantity * moleQuantity2));
				destination.LiquidOzone.Add(LiquidOzone.Remove(LiquidOzone.Quantity * moleQuantity2));
				break;
			}
		}
	}

	public Mole Remove(Chemistry.GasType gasType, MoleQuantity quantity)
	{
		return gasType switch
		{
			Chemistry.GasType.Undefined => MoleHelper.Invalid, 
			Chemistry.GasType.Oxygen => Oxygen.Remove(quantity), 
			Chemistry.GasType.Nitrogen => Nitrogen.Remove(quantity), 
			Chemistry.GasType.CarbonDioxide => CarbonDioxide.Remove(quantity), 
			Chemistry.GasType.Methane => Methane.Remove(quantity), 
			Chemistry.GasType.Pollutant => Pollutant.Remove(quantity), 
			Chemistry.GasType.Water => Water.Remove(quantity), 
			Chemistry.GasType.PollutedWater => PollutedWater.Remove(quantity), 
			Chemistry.GasType.NitrousOxide => NitrousOxide.Remove(quantity), 
			Chemistry.GasType.LiquidNitrogen => LiquidNitrogen.Remove(quantity), 
			Chemistry.GasType.LiquidOxygen => LiquidOxygen.Remove(quantity), 
			Chemistry.GasType.LiquidMethane => LiquidMethane.Remove(quantity), 
			Chemistry.GasType.Steam => Steam.Remove(quantity), 
			Chemistry.GasType.LiquidCarbonDioxide => LiquidCarbonDioxide.Remove(quantity), 
			Chemistry.GasType.LiquidPollutant => LiquidPollutant.Remove(quantity), 
			Chemistry.GasType.LiquidNitrousOxide => LiquidNitrousOxide.Remove(quantity), 
			Chemistry.GasType.Hydrogen => Hydrogen.Remove(quantity), 
			Chemistry.GasType.LiquidHydrogen => LiquidHydrogen.Remove(quantity), 
			Chemistry.GasType.Hydrazine => Hydrazine.Remove(quantity), 
			Chemistry.GasType.LiquidHydrazine => LiquidHydrazine.Remove(quantity), 
			Chemistry.GasType.LiquidAlcohol => LiquidAlcohol.Remove(quantity), 
			Chemistry.GasType.Helium => Helium.Remove(quantity), 
			Chemistry.GasType.LiquidSodiumChloride => LiquidSodiumChloride.Remove(quantity), 
			Chemistry.GasType.Silanol => Silanol.Remove(quantity), 
			Chemistry.GasType.LiquidSilanol => LiquidSilanol.Remove(quantity), 
			Chemistry.GasType.HydrochloricAcid => HydrochloricAcid.Remove(quantity), 
			Chemistry.GasType.LiquidHydrochloricAcid => LiquidHydrochloricAcid.Remove(quantity), 
			Chemistry.GasType.Ozone => Ozone.Remove(quantity), 
			Chemistry.GasType.LiquidOzone => LiquidOzone.Remove(quantity), 
			_ => MoleHelper.Invalid, 
		};
	}

	public Mole RemoveAll(Chemistry.GasType gasType)
	{
		return Remove(gasType, GetMoleValue(gasType).Quantity);
	}

	public void LerpGasses(ref GasMixture target, float t)
	{
		Oxygen.Lerp(ref target.Oxygen, t);
		Nitrogen.Lerp(ref target.Nitrogen, t);
		CarbonDioxide.Lerp(ref target.CarbonDioxide, t);
		Methane.Lerp(ref target.Methane, t);
		Pollutant.Lerp(ref target.Pollutant, t);
		NitrousOxide.Lerp(ref target.NitrousOxide, t);
		Steam.Lerp(ref target.Steam, t);
		Hydrogen.Lerp(ref target.Hydrogen, t);
		Hydrazine.Lerp(ref target.Hydrazine, t);
		Helium.Lerp(ref target.Helium, t);
		Silanol.Lerp(ref target.Silanol, t);
		HydrochloricAcid.Lerp(ref target.HydrochloricAcid, t);
		Ozone.Lerp(ref target.Ozone, t);
	}

	public void LerpLiquids(ref GasMixture gasMixture, float t)
	{
		LiquidOxygen.Lerp(ref gasMixture.LiquidOxygen, t);
		LiquidNitrogen.Lerp(ref gasMixture.LiquidNitrogen, t);
		LiquidCarbonDioxide.Lerp(ref gasMixture.LiquidCarbonDioxide, t);
		LiquidMethane.Lerp(ref gasMixture.LiquidMethane, t);
		LiquidPollutant.Lerp(ref gasMixture.LiquidPollutant, t);
		LiquidNitrousOxide.Lerp(ref gasMixture.LiquidNitrousOxide, t);
		Water.Lerp(ref gasMixture.Water, t);
		PollutedWater.Lerp(ref gasMixture.PollutedWater, t);
		LiquidHydrogen.Lerp(ref gasMixture.LiquidHydrogen, t);
		LiquidHydrazine.Lerp(ref gasMixture.LiquidHydrazine, t);
		LiquidAlcohol.Lerp(ref gasMixture.LiquidAlcohol, t);
		LiquidSodiumChloride.Lerp(ref gasMixture.LiquidSodiumChloride, t);
		LiquidSilanol.Lerp(ref gasMixture.LiquidSilanol, t);
		LiquidHydrochloricAcid.Lerp(ref gasMixture.LiquidHydrochloricAcid, t);
		LiquidOzone.Lerp(ref gasMixture.LiquidOzone, t);
	}

	public void AddEnergy(MoleEnergy energy)
	{
		if (!(GetTotalMolesGassesAndLiquids <= Chemistry.MINIMUM_VALID_TOTAL_MOLES) && !energy.IsDenormalOrZero() && !energy.IsNaN())
		{
			TotalEnergy += energy;
		}
	}

	public MoleEnergy RemoveEnergy(MoleEnergy energy)
	{
		if (GetTotalMolesGassesAndLiquids <= Chemistry.MINIMUM_VALID_TOTAL_MOLES || energy <= MoleEnergy.Zero || energy.IsNaN())
		{
			return MoleEnergy.Zero;
		}
		MoleEnergy moleEnergy = RocketMath.Min(TotalEnergy, energy);
		TotalEnergy -= moleEnergy;
		return moleEnergy;
	}

	public MoleEnergy TransferEnergyTo(ref GasMixture targetGasMix, MoleEnergy signedEnergyToTransfer)
	{
		if (!targetGasMix.IsValid)
		{
			return MoleEnergy.Zero;
		}
		HeatCapacity heatCapacity = targetGasMix.HeatCapacity + HeatCapacity;
		MoleEnergy moleEnergy = targetGasMix.TotalEnergy + TotalEnergy;
		double num = moleEnergy.ToDouble() * (targetGasMix.HeatCapacity / heatCapacity).ToDouble();
		double value = targetGasMix.TotalEnergy.ToDouble() - num;
		double num2 = moleEnergy.ToDouble() * (HeatCapacity / heatCapacity).ToDouble();
		double value2 = TotalEnergy.ToDouble() - num2;
		if (Math.Abs(signedEnergyToTransfer.ToDouble()) > Math.Abs(value) || Math.Abs(signedEnergyToTransfer.ToDouble()) > Math.Abs(value2))
		{
			signedEnergyToTransfer = new MoleEnergy((double)Math.Sign(signedEnergyToTransfer.ToDouble()) * Math.Min(Math.Abs(value), Math.Abs(value2)));
		}
		if (signedEnergyToTransfer < MoleEnergy.Zero)
		{
			return targetGasMix.TransferEnergyTo(ref this, -signedEnergyToTransfer);
		}
		MoleEnergy moleEnergy2 = RemoveEnergy(signedEnergyToTransfer);
		if (moleEnergy2 > MoleEnergy.Zero)
		{
			targetGasMix.AddEnergy(moleEnergy2);
		}
		return moleEnergy2;
	}

	public MoleEnergy TransferEnergyTo(HeatSink target, MoleEnergy signedEnergyToTransfer)
	{
		if (target == null)
		{
			return MoleEnergy.Zero;
		}
		HeatCapacity heatCapacity = target.HeatCapacity + HeatCapacity;
		MoleEnergy moleEnergy = target.Energy + TotalEnergy;
		double num = moleEnergy.ToDouble() * (target.HeatCapacity / heatCapacity).ToDouble();
		double value = target.Energy.ToDouble() - num;
		double num2 = moleEnergy.ToDouble() * (HeatCapacity / heatCapacity).ToDouble();
		double value2 = TotalEnergy.ToDouble() - num2;
		if (Math.Abs(signedEnergyToTransfer.ToDouble()) > Math.Abs(value) || Math.Abs(signedEnergyToTransfer.ToDouble()) > Math.Abs(value2))
		{
			signedEnergyToTransfer = new MoleEnergy((double)Math.Sign(signedEnergyToTransfer.ToDouble()) * Math.Min(Math.Abs(value), Math.Abs(value2)));
		}
		if (signedEnergyToTransfer < MoleEnergy.Zero)
		{
			return target.TransferEnergyTo(ref this, -signedEnergyToTransfer);
		}
		MoleEnergy moleEnergy2 = RemoveEnergy(signedEnergyToTransfer);
		if (moleEnergy2 > MoleEnergy.Zero)
		{
			target.AddEnergy(moleEnergy2);
		}
		return moleEnergy2;
	}

	public void Cleanup()
	{
		Oxygen.Cleanup();
		Nitrogen.Cleanup();
		CarbonDioxide.Cleanup();
		Methane.Cleanup();
		Pollutant.Cleanup();
		Water.Cleanup();
		PollutedWater.Cleanup();
		NitrousOxide.Cleanup();
		LiquidNitrogen.Cleanup();
		LiquidOxygen.Cleanup();
		LiquidMethane.Cleanup();
		Steam.Cleanup();
		LiquidCarbonDioxide.Cleanup();
		LiquidPollutant.Cleanup();
		LiquidNitrousOxide.Cleanup();
		Hydrogen.Cleanup();
		LiquidHydrogen.Cleanup();
		Hydrazine.Cleanup();
		LiquidHydrazine.Cleanup();
		LiquidAlcohol.Cleanup();
		Helium.Cleanup();
		LiquidSodiumChloride.Cleanup();
		Silanol.Cleanup();
		LiquidSilanol.Cleanup();
		HydrochloricAcid.Cleanup();
		LiquidHydrochloricAcid.Cleanup();
		Ozone.Cleanup();
		LiquidOzone.Cleanup();
	}

	public bool IsNaN()
	{
		if (!Oxygen.IsNaN() && !Nitrogen.IsNaN() && !CarbonDioxide.IsNaN() && !Methane.IsNaN() && !Pollutant.IsNaN() && !Water.IsNaN() && !PollutedWater.IsNaN() && !NitrousOxide.IsNaN() && !LiquidNitrogen.IsNaN() && !LiquidOxygen.IsNaN() && !LiquidMethane.IsNaN() && !Steam.IsNaN() && !LiquidCarbonDioxide.IsNaN() && !LiquidPollutant.IsNaN() && !LiquidNitrousOxide.IsNaN() && !Hydrogen.IsNaN() && !LiquidHydrogen.IsNaN() && !Hydrazine.IsNaN() && !LiquidHydrazine.IsNaN() && !LiquidAlcohol.IsNaN() && !Helium.IsNaN() && !LiquidSodiumChloride.IsNaN() && !Silanol.IsNaN() && !LiquidSilanol.IsNaN() && !HydrochloricAcid.IsNaN() && !LiquidHydrochloricAcid.IsNaN() && !Ozone.IsNaN())
		{
			return LiquidOzone.IsNaN();
		}
		return true;
	}

	public GasMixture StateChange(AtmosphereHelper.MatterState matterStatesToChange, PressurekPa pressure, VolumeLitres volume)
	{
		switch (matterStatesToChange)
		{
		case AtmosphereHelper.MatterState.Liquid:
			return StateChangeLiquids(pressure, volume);
		case AtmosphereHelper.MatterState.Gas:
			return StateChangeGasses(pressure, volume);
		case AtmosphereHelper.MatterState.All:
		{
			GasMixture result = StateChangeGasses(pressure, volume);
			GasMixture gasMixture = StateChangeLiquids(pressure, volume);
			if (!result.IsValid)
			{
				return gasMixture;
			}
			result.Add(gasMixture);
			return result;
		}
		default:
			throw new ArgumentOutOfRangeException("matterStatesToChange", matterStatesToChange, null);
		}
	}

	public GasMixture StateChangeLiquids(PressurekPa pressure, VolumeLitres volume)
	{
		TemperatureKelvin zero = TemperatureKelvin.Zero;
		Mole mole = LiquidNitrogen.ChangeState(pressure, volume, zero);
		Mole mole2 = LiquidOxygen.ChangeState(pressure, volume, zero);
		Mole mole3 = LiquidMethane.ChangeState(pressure, volume, zero);
		Mole mole4 = Water.ChangeState(pressure, volume, zero);
		Mole mole5 = PollutedWater.ChangeState(pressure, volume, zero);
		Mole mole6 = LiquidCarbonDioxide.ChangeState(pressure, volume, zero);
		Mole mole7 = LiquidPollutant.ChangeState(pressure, volume, zero);
		Mole mole8 = LiquidNitrousOxide.ChangeState(pressure, volume, zero);
		Mole mole9 = LiquidHydrogen.ChangeState(pressure, volume, zero);
		Mole mole10 = LiquidHydrazine.ChangeState(pressure, volume, zero);
		Mole mole11 = LiquidAlcohol.ChangeState(pressure, volume, zero);
		Mole mole12 = LiquidSilanol.ChangeState(pressure, volume, zero);
		Mole mole13 = LiquidHydrochloricAcid.ChangeState(pressure, volume, zero);
		Mole mole14 = LiquidOzone.ChangeState(pressure, volume, zero);
		Mole mole15 = LiquidSodiumChloride.ChangeState(pressure, volume, zero);
		if (mole.IsValid || mole2.IsValid || mole3.IsValid || mole4.IsValid || mole5.IsValid || mole6.IsValid || mole7.IsValid || mole8.IsValid || mole9.IsValid || mole10.IsValid || mole11.IsValid || mole12.IsValid || mole13.IsValid || mole14.IsValid || mole15.IsValid)
		{
			GasMixture result = GasMixtureHelper.Create();
			result.Add(mole);
			result.Add(mole2);
			result.Add(mole3);
			result.Add(mole4);
			result.Add(mole5);
			result.Add(mole6);
			result.Add(mole7);
			result.Add(mole8);
			result.Add(mole9);
			result.Add(mole10);
			result.Add(mole11);
			result.Add(mole12);
			result.Add(mole13);
			result.Add(mole14);
			result.Add(mole15);
			return result;
		}
		return GasMixtureHelper.Invalid;
	}

	public GasMixture StateChangeGasses(PressurekPa pressure, VolumeLitres volume)
	{
		TemperatureKelvin zero = TemperatureKelvin.Zero;
		Mole mole = Nitrogen.ChangeState(pressure, volume, zero);
		Mole mole2 = Oxygen.ChangeState(pressure, volume, zero);
		Mole mole3 = Methane.ChangeState(pressure, volume, zero);
		Mole mole4 = Steam.ChangeState(pressure, volume, zero);
		Mole mole5 = CarbonDioxide.ChangeState(pressure, volume, zero);
		Mole mole6 = Pollutant.ChangeState(pressure, volume, zero);
		Mole mole7 = NitrousOxide.ChangeState(pressure, volume, zero);
		Mole mole8 = Hydrogen.ChangeState(pressure, volume, zero);
		Mole mole9 = Hydrazine.ChangeState(pressure, volume, zero);
		Mole mole10 = Silanol.ChangeState(pressure, volume, zero);
		Mole mole11 = HydrochloricAcid.ChangeState(pressure, volume, zero);
		Mole mole12 = Ozone.ChangeState(pressure, volume, zero);
		if (mole.IsValid || mole2.IsValid || mole3.IsValid || mole4.IsValid || mole5.IsValid || mole6.IsValid || mole7.IsValid || mole8.IsValid || mole9.IsValid || mole10.IsValid || mole11.IsValid || mole12.IsValid)
		{
			GasMixture result = GasMixtureHelper.Create();
			result.Add(mole);
			result.Add(mole2);
			result.Add(mole3);
			result.Add(mole4);
			result.Add(mole5);
			result.Add(mole6);
			result.Add(mole7);
			result.Add(mole8);
			result.Add(mole9);
			result.Add(mole10);
			result.Add(mole11);
			result.Add(mole12);
			return result;
		}
		return GasMixtureHelper.Invalid;
	}

	public bool TrySetCreatedReagentMixture(ReagentMixture reagentMixture)
	{
		return false;
	}

	public Slot GetNextFreeSlot()
	{
		throw new NotImplementedException();
	}

	public float TotalMassGassesAndLiquidsGrams()
	{
		return (float)(Water.GetMass() + PollutedWater.GetMass() + LiquidNitrogen.GetMass() + LiquidOxygen.GetMass() + LiquidMethane.GetMass() + LiquidCarbonDioxide.GetMass() + LiquidPollutant.GetMass() + LiquidNitrousOxide.GetMass() + LiquidHydrogen.GetMass() + LiquidHydrazine.GetMass() + LiquidAlcohol.GetMass() + LiquidSodiumChloride.GetMass() + LiquidSilanol.GetMass() + LiquidHydrochloricAcid.GetMass() + LiquidOzone.GetMass() + Oxygen.GetMass() + Nitrogen.GetMass() + CarbonDioxide.GetMass() + Methane.GetMass() + Pollutant.GetMass() + NitrousOxide.GetMass() + Steam.GetMass() + Hydrogen.GetMass() + Hydrazine.GetMass() + Helium.GetMass() + Silanol.GetMass() + HydrochloricAcid.GetMass() + Ozone.GetMass());
	}

	public VolumeLitres GetMolarVolumeLiquids()
	{
		double num = GetTotalMolesLiquids.ToDouble();
		if (num <= 0.0)
		{
			return VolumeLitres.Zero;
		}
		return Water.GetVolume() / num + PollutedWater.GetVolume() / num + LiquidNitrogen.GetVolume() / num + LiquidOxygen.GetVolume() / num + LiquidMethane.GetVolume() / num + LiquidCarbonDioxide.GetVolume() / num + LiquidPollutant.GetVolume() / num + LiquidNitrousOxide.GetVolume() / num + LiquidHydrogen.GetVolume() / num + LiquidHydrazine.GetVolume() / num + LiquidAlcohol.GetVolume() / num + LiquidSodiumChloride.GetVolume() / num + LiquidSilanol.GetVolume() / num + LiquidHydrochloricAcid.GetVolume() / num + LiquidOzone.GetVolume() / num;
	}

	public float MolarMassGassesGrams()
	{
		double num = GetTotalMolesGasses.ToDouble();
		if (num <= 0.0)
		{
			return 0f;
		}
		return (float)(Oxygen.GetMass() / num + Nitrogen.GetMass() / num + CarbonDioxide.GetMass() / num + Methane.GetMass() / num + Pollutant.GetMass() / num + NitrousOxide.GetMass() / num + Steam.GetMass() / num + Hydrogen.GetMass() / num + Hydrazine.GetMass() / num + Helium.GetMass() / num + Silanol.GetMass() / num + HydrochloricAcid.GetMass() / num + Ozone.GetMass() / num);
	}

	public float HeatCapacityRatio()
	{
		MoleQuantity getTotalMolesGassesAndLiquids = GetTotalMolesGassesAndLiquids;
		if (getTotalMolesGassesAndLiquids < Chemistry.MINIMUM_QUANTITY_MOLES)
		{
			return (float)Chemistry.TriatomicDegreesOfFreedom;
		}
		return (float)(Oxygen.HeatCapacityRatio() * (Oxygen.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Nitrogen.HeatCapacityRatio() * (Nitrogen.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + CarbonDioxide.HeatCapacityRatio() * (CarbonDioxide.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Methane.HeatCapacityRatio() * (Methane.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Pollutant.HeatCapacityRatio() * (Pollutant.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Water.HeatCapacityRatio() * (Water.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + PollutedWater.HeatCapacityRatio() * (PollutedWater.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + NitrousOxide.HeatCapacityRatio() * (NitrousOxide.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidNitrogen.HeatCapacityRatio() * (LiquidNitrogen.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidOxygen.HeatCapacityRatio() * (LiquidOxygen.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidMethane.HeatCapacityRatio() * (LiquidMethane.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidCarbonDioxide.HeatCapacityRatio() * (LiquidCarbonDioxide.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidPollutant.HeatCapacityRatio() * (LiquidPollutant.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidNitrousOxide.HeatCapacityRatio() * (LiquidNitrousOxide.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Steam.HeatCapacityRatio() * (Steam.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Hydrogen.HeatCapacityRatio() * (Hydrogen.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidHydrogen.HeatCapacityRatio() * (LiquidHydrogen.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Hydrazine.HeatCapacityRatio() * (Hydrazine.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidHydrazine.HeatCapacityRatio() * (LiquidHydrazine.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidAlcohol.HeatCapacityRatio() * (LiquidAlcohol.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Helium.HeatCapacityRatio() * (Helium.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidSodiumChloride.HeatCapacityRatio() * (LiquidSodiumChloride.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Silanol.HeatCapacityRatio() * (Silanol.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidSilanol.HeatCapacityRatio() * (LiquidSilanol.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + HydrochloricAcid.HeatCapacityRatio() * (HydrochloricAcid.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidHydrochloricAcid.HeatCapacityRatio() * (LiquidHydrochloricAcid.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + Ozone.HeatCapacityRatio() * (Ozone.Quantity / getTotalMolesGassesAndLiquids).ToDouble() + LiquidOzone.HeatCapacityRatio() * (LiquidOzone.Quantity / getTotalMolesGassesAndLiquids).ToDouble());
	}

	public void SetQuantity(float value)
	{
		throw new NotImplementedException();
	}

	public MoleQuantity GetTotalMoles()
	{
		return GetTotalMoles(AtmosphereHelper.MatterState.All);
	}

	public int GetPrefabHash()
	{
		return -1;
	}

	public string DebugPrint()
	{
		List<string> list = new List<string>();
		if (Oxygen.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.Oxygen) + " " + StringManager.Get(Oxygen.Quantity.ToFloat()));
		}
		if (Nitrogen.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.Nitrogen) + " " + StringManager.Get(Nitrogen.Quantity.ToFloat()));
		}
		if (CarbonDioxide.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.CarbonDioxide) + " " + StringManager.Get(CarbonDioxide.Quantity.ToFloat()));
		}
		if (Methane.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.Methane) + " " + StringManager.Get(Methane.Quantity.ToFloat()));
		}
		if (Pollutant.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.Pollutant) + " " + StringManager.Get(Pollutant.Quantity.ToFloat()));
		}
		if (NitrousOxide.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.NitrousOxide) + " " + StringManager.Get(NitrousOxide.Quantity.ToFloat()));
		}
		if (Steam.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.Steam) + " " + StringManager.Get(Steam.Quantity.ToFloat()));
		}
		if (Hydrogen.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.Hydrogen) + " " + StringManager.Get(Hydrogen.Quantity.ToFloat()));
		}
		if (Water.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.Water) + " " + StringManager.Get(Water.Quantity.ToFloat()));
		}
		if (PollutedWater.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.PollutedWater) + " " + StringManager.Get(PollutedWater.Quantity.ToFloat()));
		}
		if (LiquidNitrogen.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.LiquidNitrogen) + " " + StringManager.Get(LiquidNitrogen.Quantity.ToFloat()));
		}
		if (LiquidOxygen.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.LiquidOxygen) + " " + StringManager.Get(LiquidOxygen.Quantity.ToFloat()));
		}
		if (LiquidMethane.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.LiquidMethane) + " " + StringManager.Get(LiquidMethane.Quantity.ToFloat()));
		}
		if (LiquidCarbonDioxide.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.LiquidCarbonDioxide) + " " + StringManager.Get(LiquidCarbonDioxide.Quantity.ToFloat()));
		}
		if (LiquidPollutant.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.LiquidPollutant) + " " + StringManager.Get(LiquidPollutant.Quantity.ToFloat()));
		}
		if (LiquidNitrousOxide.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.LiquidNitrousOxide) + " " + StringManager.Get(LiquidNitrousOxide.Quantity.ToFloat()));
		}
		if (LiquidHydrogen.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.LiquidHydrogen) + " " + StringManager.Get(LiquidHydrogen.Quantity.ToFloat()));
		}
		if (Hydrazine.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.Hydrazine) + " " + StringManager.Get(Hydrazine.Quantity.ToFloat()));
		}
		if (LiquidHydrazine.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.LiquidHydrazine) + " " + StringManager.Get(LiquidHydrazine.Quantity.ToFloat()));
		}
		if (LiquidAlcohol.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.LiquidAlcohol) + " " + StringManager.Get(LiquidAlcohol.Quantity.ToFloat()));
		}
		if (Helium.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.Helium) + " " + StringManager.Get(Helium.Quantity.ToFloat()));
		}
		if (LiquidSodiumChloride.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.LiquidSodiumChloride) + " " + StringManager.Get(LiquidSodiumChloride.Quantity.ToFloat()));
		}
		if (Silanol.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.Silanol) + " " + StringManager.Get(Silanol.Quantity.ToFloat()));
		}
		if (LiquidSilanol.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.LiquidSilanol) + " " + StringManager.Get(LiquidSilanol.Quantity.ToFloat()));
		}
		if (HydrochloricAcid.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.HydrochloricAcid) + " " + StringManager.Get(HydrochloricAcid.Quantity.ToFloat()));
		}
		if (LiquidHydrochloricAcid.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.LiquidHydrochloricAcid) + " " + StringManager.Get(LiquidHydrochloricAcid.Quantity.ToFloat()));
		}
		if (Ozone.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.Ozone) + " " + StringManager.Get(Ozone.Quantity.ToFloat()));
		}
		if (LiquidOzone.Quantity > MoleQuantity.Zero)
		{
			list.Add(Chemistry.GetSymbol(Chemistry.GasType.LiquidOzone) + " " + StringManager.Get(LiquidOzone.Quantity.ToFloat()));
		}
		return string.Join(Environment.NewLine, list);
	}

	public double InWorldLiquidVolumeMultiplier()
	{
		MoleQuantity getTotalMolesLiquids = GetTotalMolesLiquids;
		if (getTotalMolesLiquids < Chemistry.MINIMUM_QUANTITY_MOLES)
		{
			return 1.0;
		}
		return MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.Water) * (Water.Quantity / getTotalMolesLiquids).ToDouble() + MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.PollutedWater) * (PollutedWater.Quantity / getTotalMolesLiquids).ToDouble() + MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidNitrogen) * (LiquidNitrogen.Quantity / getTotalMolesLiquids).ToDouble() + MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidOxygen) * (LiquidOxygen.Quantity / getTotalMolesLiquids).ToDouble() + MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidMethane) * (LiquidMethane.Quantity / getTotalMolesLiquids).ToDouble() + MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidCarbonDioxide) * (LiquidCarbonDioxide.Quantity / getTotalMolesLiquids).ToDouble() + MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidPollutant) * (LiquidPollutant.Quantity / getTotalMolesLiquids).ToDouble() + MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidNitrousOxide) * (LiquidNitrousOxide.Quantity / getTotalMolesLiquids).ToDouble() + MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidHydrogen) * (LiquidHydrogen.Quantity / getTotalMolesLiquids).ToDouble() + MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidHydrazine) * (LiquidHydrazine.Quantity / getTotalMolesLiquids).ToDouble() + MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidAlcohol) * (LiquidAlcohol.Quantity / getTotalMolesLiquids).ToDouble() + MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidSodiumChloride) * (LiquidSodiumChloride.Quantity / getTotalMolesLiquids).ToDouble() + MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidSilanol) * (LiquidSilanol.Quantity / getTotalMolesLiquids).ToDouble() + MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidHydrochloricAcid) * (LiquidHydrochloricAcid.Quantity / getTotalMolesLiquids).ToDouble() + MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidOzone) * (LiquidOzone.Quantity / getTotalMolesLiquids).ToDouble();
	}

	public bool IsAutoIgnition()
	{
		TemperatureKelvin val = ((NitrousOxide.Quantity + LiquidNitrousOxide.Quantity > MinCombustionMoles) ? Chemistry.AutoIgnitionOffsetNitrogenDioxide : TemperatureKelvin.Zero);
		TemperatureKelvin val2 = ((Ozone.Quantity + LiquidOzone.Quantity > MinCombustionMoles) ? Chemistry.AutoIgnitionOffsetOzone : TemperatureKelvin.Zero);
		TemperatureKelvin temperatureKelvin = RocketMath.Min(val, val2);
		TemperatureKelvin temperature = Temperature;
		if (Methane.Quantity + LiquidMethane.Quantity > MinCombustionMoles && temperature > Methane.AutoIgnitionTemperature + temperatureKelvin)
		{
			return true;
		}
		if (Hydrogen.Quantity + LiquidHydrogen.Quantity > MinCombustionMoles && temperature > Hydrogen.AutoIgnitionTemperature + temperatureKelvin)
		{
			return true;
		}
		if (LiquidAlcohol.Quantity > MinCombustionMoles && temperature > LiquidAlcohol.AutoIgnitionTemperature + temperatureKelvin)
		{
			return true;
		}
		if (Hydrazine.Quantity + LiquidHydrazine.Quantity > MinCombustionMoles && temperature > Hydrazine.AutoIgnitionTemperature)
		{
			return true;
		}
		return false;
	}

	public MoleEnergy Combust(double combustionRatio, out MoleQuantity burnedFuel, out float cleanBurnRatio)
	{
		burnedFuel = MoleQuantity.Zero;
		cleanBurnRatio = 1f;
		MoleEnergy zero = MoleEnergy.Zero;
		MoleQuantity totalFuel = TotalFuel;
		MoleQuantity totalOxidiser = TotalOxidiser;
		MoleQuantity totalHypergolics = TotalHypergolics;
		if (totalHypergolics <= MoleQuantity.Zero && (totalFuel <= MoleQuantity.Zero || totalOxidiser <= MoleQuantity.Zero))
		{
			return zero;
		}
		GasMixture newGasMix = GasMixtureHelper.Create();
		if (totalHypergolics > MoleQuantity.Zero)
		{
			MoleQuantity quantity = Hydrazine.Quantity;
			if (quantity > MoleQuantity.Zero)
			{
				newGasMix.Add(Combustion.CombustMoles(Hydrazine.Remove(quantity / 2.0), Hydrazine.Remove(quantity / 2.0), combustionRatio, out var combustionEnergy, out var combustedFuel, out var cleanBurnRatio2));
				zero += combustionEnergy;
				burnedFuel += combustedFuel;
				cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio2, 1f);
			}
			MoleQuantity quantity2 = LiquidHydrazine.Quantity;
			if (quantity2 > MoleQuantity.Zero)
			{
				newGasMix.Add(Combustion.CombustMoles(LiquidHydrazine.Remove(quantity2 / 2.0), LiquidHydrazine.Remove(quantity2 / 2.0), combustionRatio, out var combustionEnergy2, out var combustedFuel2, out var cleanBurnRatio3));
				zero += combustionEnergy2;
				burnedFuel += combustedFuel2;
				cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio3, 1f);
			}
		}
		if (totalOxidiser > MoleQuantity.Zero && totalFuel > MoleQuantity.Zero)
		{
			MoleQuantity quantity3 = Methane.Quantity;
			MoleQuantity quantity4 = LiquidMethane.Quantity;
			MoleQuantity quantity5 = Hydrogen.Quantity;
			MoleQuantity quantity6 = LiquidHydrogen.Quantity;
			MoleQuantity quantity7 = LiquidAlcohol.Quantity;
			MoleQuantity quantity8 = Oxygen.Quantity;
			MoleQuantity quantity9 = LiquidOxygen.Quantity;
			MoleQuantity quantity10 = NitrousOxide.Quantity;
			MoleQuantity quantity11 = LiquidNitrousOxide.Quantity;
			MoleQuantity quantity12 = Ozone.Quantity;
			MoleQuantity quantity13 = LiquidOzone.Quantity;
			double num = (quantity3 / totalFuel).ToDouble();
			double num2 = (quantity4 / totalFuel).ToDouble();
			double num3 = (quantity5 / totalFuel).ToDouble();
			double num4 = (quantity6 / totalFuel).ToDouble();
			double num5 = (quantity7 / totalFuel).ToDouble();
			double num6 = (quantity8 / totalOxidiser).ToDouble();
			double num7 = (quantity9 / totalOxidiser).ToDouble();
			double num8 = (quantity10 / totalOxidiser).ToDouble();
			double num9 = (quantity11 / totalOxidiser).ToDouble();
			double num10 = (quantity12 / totalOxidiser).ToDouble();
			double num11 = (quantity13 / totalOxidiser).ToDouble();
			MoleQuantity moleQuantity = quantity3 * Combustion.ResultMethaneOxygen.OxidiserRatio;
			MoleQuantity moleQuantity2 = quantity4 * Combustion.ResultMethaneOxygen.OxidiserRatio;
			MoleQuantity moleQuantity3 = quantity5 * Combustion.ResultHydrogenOxygen.OxidiserRatio;
			MoleQuantity moleQuantity4 = quantity6 * Combustion.ResultHydrogenOxygen.OxidiserRatio;
			MoleQuantity moleQuantity5 = quantity7 * Combustion.ResultAlcoholOxygen.OxidiserRatio;
			MoleQuantity moleQuantity6 = moleQuantity + moleQuantity2 + moleQuantity3 + moleQuantity4 + moleQuantity5;
			MoleQuantity moleQuantity7 = ((moleQuantity6 > MoleQuantity.Zero) ? RocketMath.Min(quantity8 / moleQuantity6, MoleQuantity.One) : MoleQuantity.One);
			moleQuantity *= moleQuantity7;
			moleQuantity2 *= moleQuantity7;
			moleQuantity3 *= moleQuantity7;
			moleQuantity4 *= moleQuantity7;
			moleQuantity5 *= moleQuantity7;
			MoleQuantity moleQuantity8 = quantity3 * Combustion.ResultMethaneOxygen.OxidiserRatio;
			MoleQuantity moleQuantity9 = quantity4 * Combustion.ResultMethaneOxygen.OxidiserRatio;
			MoleQuantity moleQuantity10 = quantity5 * Combustion.ResultHydrogenOxygen.OxidiserRatio;
			MoleQuantity moleQuantity11 = quantity6 * Combustion.ResultHydrogenOxygen.OxidiserRatio;
			MoleQuantity moleQuantity12 = quantity7 * Combustion.ResultAlcoholOxygen.OxidiserRatio;
			MoleQuantity moleQuantity13 = moleQuantity8 + moleQuantity9 + moleQuantity10 + moleQuantity11 + moleQuantity12;
			MoleQuantity moleQuantity14 = ((moleQuantity13 > MoleQuantity.Zero) ? RocketMath.Min(quantity9 / moleQuantity13, MoleQuantity.One) : MoleQuantity.One);
			moleQuantity8 *= moleQuantity14;
			moleQuantity9 *= moleQuantity14;
			moleQuantity10 *= moleQuantity14;
			moleQuantity11 *= moleQuantity14;
			moleQuantity12 *= moleQuantity14;
			MoleQuantity moleQuantity15 = quantity3 * Combustion.ResultMethaneNitrous.OxidiserRatio;
			MoleQuantity moleQuantity16 = quantity4 * Combustion.ResultMethaneNitrous.OxidiserRatio;
			MoleQuantity moleQuantity17 = quantity5 * Combustion.ResultHydrogenNitrous.OxidiserRatio;
			MoleQuantity moleQuantity18 = quantity6 * Combustion.ResultHydrogenNitrous.OxidiserRatio;
			MoleQuantity moleQuantity19 = quantity7 * Combustion.ResultAlcoholNitrous.OxidiserRatio;
			MoleQuantity moleQuantity20 = moleQuantity15 + moleQuantity16 + moleQuantity17 + moleQuantity18 + moleQuantity19;
			MoleQuantity moleQuantity21 = ((moleQuantity20 > MoleQuantity.Zero) ? RocketMath.Min(quantity10 / moleQuantity20, MoleQuantity.One) : MoleQuantity.One);
			moleQuantity15 *= moleQuantity21;
			moleQuantity16 *= moleQuantity21;
			moleQuantity17 *= moleQuantity21;
			moleQuantity18 *= moleQuantity21;
			moleQuantity19 *= moleQuantity21;
			MoleQuantity moleQuantity22 = quantity3 * Combustion.ResultMethaneNitrous.OxidiserRatio;
			MoleQuantity moleQuantity23 = quantity4 * Combustion.ResultMethaneNitrous.OxidiserRatio;
			MoleQuantity moleQuantity24 = quantity5 * Combustion.ResultHydrogenNitrous.OxidiserRatio;
			MoleQuantity moleQuantity25 = quantity6 * Combustion.ResultHydrogenNitrous.OxidiserRatio;
			MoleQuantity moleQuantity26 = quantity7 * Combustion.ResultAlcoholNitrous.OxidiserRatio;
			MoleQuantity moleQuantity27 = moleQuantity22 + moleQuantity23 + moleQuantity24 + moleQuantity25 + moleQuantity26;
			MoleQuantity moleQuantity28 = ((moleQuantity27 > MoleQuantity.Zero) ? RocketMath.Min(quantity11 / moleQuantity27, MoleQuantity.One) : MoleQuantity.One);
			moleQuantity22 *= moleQuantity28;
			moleQuantity23 *= moleQuantity28;
			moleQuantity24 *= moleQuantity28;
			moleQuantity25 *= moleQuantity28;
			moleQuantity26 *= moleQuantity28;
			MoleQuantity moleQuantity29 = quantity3 * Combustion.ResultMethaneOzone.OxidiserRatio;
			MoleQuantity moleQuantity30 = quantity4 * Combustion.ResultMethaneOzone.OxidiserRatio;
			MoleQuantity moleQuantity31 = quantity5 * Combustion.ResultHydrogenOzone.OxidiserRatio;
			MoleQuantity moleQuantity32 = quantity6 * Combustion.ResultHydrogenOzone.OxidiserRatio;
			MoleQuantity moleQuantity33 = quantity7 * Combustion.ResultAlcoholOzone.OxidiserRatio;
			MoleQuantity moleQuantity34 = moleQuantity29 + moleQuantity30 + moleQuantity31 + moleQuantity32 + moleQuantity33;
			MoleQuantity moleQuantity35 = ((moleQuantity34 > MoleQuantity.Zero) ? RocketMath.Min(quantity12 / moleQuantity34, MoleQuantity.One) : MoleQuantity.One);
			moleQuantity29 *= moleQuantity35;
			moleQuantity30 *= moleQuantity35;
			moleQuantity31 *= moleQuantity35;
			moleQuantity32 *= moleQuantity35;
			moleQuantity33 *= moleQuantity35;
			MoleQuantity moleQuantity36 = quantity3 * Combustion.ResultMethaneOzone.OxidiserRatio;
			MoleQuantity moleQuantity37 = quantity4 * Combustion.ResultMethaneOzone.OxidiserRatio;
			MoleQuantity moleQuantity38 = quantity5 * Combustion.ResultHydrogenOzone.OxidiserRatio;
			MoleQuantity moleQuantity39 = quantity6 * Combustion.ResultHydrogenOzone.OxidiserRatio;
			MoleQuantity moleQuantity40 = quantity7 * Combustion.ResultAlcoholOzone.OxidiserRatio;
			MoleQuantity moleQuantity41 = moleQuantity36 + moleQuantity37 + moleQuantity38 + moleQuantity39 + moleQuantity40;
			MoleQuantity moleQuantity42 = ((moleQuantity41 > MoleQuantity.Zero) ? RocketMath.Min(quantity13 / moleQuantity41, MoleQuantity.One) : MoleQuantity.One);
			moleQuantity36 *= moleQuantity42;
			moleQuantity37 *= moleQuantity42;
			moleQuantity38 *= moleQuantity42;
			moleQuantity39 *= moleQuantity42;
			moleQuantity40 *= moleQuantity42;
			MoleQuantity moleQuantity43 = moleQuantity * Combustion.ResultMethaneOxygen.FuelRatio;
			MoleQuantity moleQuantity44 = moleQuantity8 * Combustion.ResultMethaneOxygen.FuelRatio;
			MoleQuantity moleQuantity45 = moleQuantity15 * Combustion.ResultMethaneNitrous.FuelRatio;
			MoleQuantity moleQuantity46 = moleQuantity22 * Combustion.ResultMethaneNitrous.FuelRatio;
			MoleQuantity moleQuantity47 = moleQuantity29 * Combustion.ResultMethaneOzone.FuelRatio;
			MoleQuantity moleQuantity48 = moleQuantity36 * Combustion.ResultMethaneOzone.FuelRatio;
			MoleQuantity moleQuantity49 = moleQuantity43 + moleQuantity44 + moleQuantity45 + moleQuantity46 + moleQuantity47 + moleQuantity48;
			MoleQuantity moleQuantity50 = ((moleQuantity49 > MoleQuantity.Zero) ? RocketMath.Min(quantity3 / moleQuantity49, MoleQuantity.One) : MoleQuantity.One);
			moleQuantity43 *= moleQuantity50;
			moleQuantity44 *= moleQuantity50;
			moleQuantity45 *= moleQuantity50;
			moleQuantity46 *= moleQuantity50;
			moleQuantity47 *= moleQuantity50;
			moleQuantity48 *= moleQuantity50;
			MoleQuantity moleQuantity51 = moleQuantity2 * Combustion.ResultMethaneOxygen.FuelRatio;
			MoleQuantity moleQuantity52 = moleQuantity9 * Combustion.ResultMethaneOxygen.FuelRatio;
			MoleQuantity moleQuantity53 = moleQuantity16 * Combustion.ResultMethaneNitrous.FuelRatio;
			MoleQuantity moleQuantity54 = moleQuantity23 * Combustion.ResultMethaneNitrous.FuelRatio;
			MoleQuantity moleQuantity55 = moleQuantity30 * Combustion.ResultMethaneOzone.FuelRatio;
			MoleQuantity moleQuantity56 = moleQuantity37 * Combustion.ResultMethaneOzone.FuelRatio;
			MoleQuantity moleQuantity57 = moleQuantity51 + moleQuantity52 + moleQuantity53 + moleQuantity54 + moleQuantity55 + moleQuantity56;
			MoleQuantity moleQuantity58 = ((moleQuantity57 > MoleQuantity.Zero) ? RocketMath.Min(quantity4 / moleQuantity57, MoleQuantity.One) : MoleQuantity.One);
			moleQuantity51 *= moleQuantity58;
			moleQuantity52 *= moleQuantity58;
			moleQuantity53 *= moleQuantity58;
			moleQuantity54 *= moleQuantity58;
			moleQuantity55 *= moleQuantity58;
			moleQuantity56 *= moleQuantity58;
			MoleQuantity moleQuantity59 = moleQuantity3 * Combustion.ResultHydrogenOxygen.FuelRatio;
			MoleQuantity moleQuantity60 = moleQuantity10 * Combustion.ResultHydrogenOxygen.FuelRatio;
			MoleQuantity moleQuantity61 = moleQuantity17 * Combustion.ResultHydrogenNitrous.FuelRatio;
			MoleQuantity moleQuantity62 = moleQuantity24 * Combustion.ResultHydrogenNitrous.FuelRatio;
			MoleQuantity moleQuantity63 = moleQuantity31 * Combustion.ResultHydrogenOzone.FuelRatio;
			MoleQuantity moleQuantity64 = moleQuantity38 * Combustion.ResultHydrogenOzone.FuelRatio;
			MoleQuantity moleQuantity65 = moleQuantity59 + moleQuantity60 + moleQuantity61 + moleQuantity62 + moleQuantity63 + moleQuantity64;
			MoleQuantity moleQuantity66 = ((moleQuantity65 > MoleQuantity.Zero) ? RocketMath.Min(quantity5 / moleQuantity65, MoleQuantity.One) : MoleQuantity.One);
			moleQuantity59 *= moleQuantity66;
			moleQuantity60 *= moleQuantity66;
			moleQuantity61 *= moleQuantity66;
			moleQuantity62 *= moleQuantity66;
			moleQuantity63 *= moleQuantity66;
			moleQuantity64 *= moleQuantity66;
			MoleQuantity moleQuantity67 = moleQuantity4 * Combustion.ResultHydrogenOxygen.FuelRatio;
			MoleQuantity moleQuantity68 = moleQuantity11 * Combustion.ResultHydrogenOxygen.FuelRatio;
			MoleQuantity moleQuantity69 = moleQuantity18 * Combustion.ResultHydrogenNitrous.FuelRatio;
			MoleQuantity moleQuantity70 = moleQuantity25 * Combustion.ResultHydrogenNitrous.FuelRatio;
			MoleQuantity moleQuantity71 = moleQuantity32 * Combustion.ResultHydrogenOzone.FuelRatio;
			MoleQuantity moleQuantity72 = moleQuantity39 * Combustion.ResultHydrogenOzone.FuelRatio;
			MoleQuantity moleQuantity73 = moleQuantity67 + moleQuantity68 + moleQuantity69 + moleQuantity70 + moleQuantity71 + moleQuantity72;
			MoleQuantity moleQuantity74 = ((moleQuantity73 > MoleQuantity.Zero) ? RocketMath.Min(quantity6 / moleQuantity73, MoleQuantity.One) : MoleQuantity.One);
			moleQuantity67 *= moleQuantity74;
			moleQuantity68 *= moleQuantity74;
			moleQuantity69 *= moleQuantity74;
			moleQuantity70 *= moleQuantity74;
			moleQuantity71 *= moleQuantity74;
			moleQuantity72 *= moleQuantity74;
			MoleQuantity moleQuantity75 = moleQuantity5 * Combustion.ResultAlcoholOxygen.FuelRatio;
			MoleQuantity moleQuantity76 = moleQuantity12 * Combustion.ResultAlcoholOxygen.FuelRatio;
			MoleQuantity moleQuantity77 = moleQuantity19 * Combustion.ResultAlcoholNitrous.FuelRatio;
			MoleQuantity moleQuantity78 = moleQuantity26 * Combustion.ResultAlcoholNitrous.FuelRatio;
			MoleQuantity moleQuantity79 = moleQuantity33 * Combustion.ResultAlcoholOzone.FuelRatio;
			MoleQuantity moleQuantity80 = moleQuantity40 * Combustion.ResultAlcoholOzone.FuelRatio;
			MoleQuantity moleQuantity81 = moleQuantity75 + moleQuantity76 + moleQuantity77 + moleQuantity78 + moleQuantity79 + moleQuantity80;
			MoleQuantity moleQuantity82 = ((moleQuantity81 > MoleQuantity.Zero) ? RocketMath.Min(quantity7 / moleQuantity81, MoleQuantity.One) : MoleQuantity.One);
			moleQuantity75 *= moleQuantity82;
			moleQuantity76 *= moleQuantity82;
			moleQuantity77 *= moleQuantity82;
			moleQuantity78 *= moleQuantity82;
			moleQuantity79 *= moleQuantity82;
			moleQuantity80 *= moleQuantity82;
			if (quantity3 > MoleQuantity.Zero)
			{
				if (quantity8 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(Methane.Remove(moleQuantity43), Oxygen.Remove(moleQuantity), combustionRatio, out var combustionEnergy3, out var combustedFuel3, out var cleanBurnRatio4));
					zero += combustionEnergy3;
					burnedFuel += combustedFuel3;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio4, (float)(num6 * num));
				}
				if (quantity9 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(Methane.Remove(moleQuantity44), LiquidOxygen.Remove(moleQuantity8), combustionRatio, out var combustionEnergy4, out var combustedFuel4, out var cleanBurnRatio5));
					zero += combustionEnergy4;
					burnedFuel += combustedFuel4;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio5, (float)(num7 * num));
				}
				if (quantity10 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(Methane.Remove(moleQuantity45), NitrousOxide.Remove(moleQuantity15), combustionRatio, out var combustionEnergy5, out var combustedFuel5, out var cleanBurnRatio6));
					zero += combustionEnergy5;
					burnedFuel += combustedFuel5;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio6, (float)(num8 * num));
				}
				if (quantity11 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(Methane.Remove(moleQuantity46), LiquidNitrousOxide.Remove(moleQuantity22), combustionRatio, out var combustionEnergy6, out var combustedFuel6, out var cleanBurnRatio7));
					zero += combustionEnergy6;
					burnedFuel += combustedFuel6;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio7, (float)(num9 * num));
				}
				if (quantity12 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(Methane.Remove(moleQuantity47), Ozone.Remove(moleQuantity29), combustionRatio, out var combustionEnergy7, out var combustedFuel7, out var cleanBurnRatio8));
					zero += combustionEnergy7;
					burnedFuel += combustedFuel7;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio8, (float)(num10 * num));
				}
				if (quantity13 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(Methane.Remove(moleQuantity48), LiquidOzone.Remove(moleQuantity36), combustionRatio, out var combustionEnergy8, out var combustedFuel8, out var cleanBurnRatio9));
					zero += combustionEnergy8;
					burnedFuel += combustedFuel8;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio9, (float)(num11 * num));
				}
			}
			if (quantity4 > MoleQuantity.Zero)
			{
				if (quantity8 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidMethane.Remove(moleQuantity51), Oxygen.Remove(moleQuantity2), combustionRatio, out var combustionEnergy9, out var combustedFuel9, out var cleanBurnRatio10));
					zero += combustionEnergy9;
					burnedFuel += combustedFuel9;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio10, (float)(num6 * num2));
				}
				if (quantity9 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidMethane.Remove(moleQuantity52), LiquidOxygen.Remove(moleQuantity9), combustionRatio, out var combustionEnergy10, out var combustedFuel10, out var cleanBurnRatio11));
					zero += combustionEnergy10;
					burnedFuel += combustedFuel10;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio11, (float)(num7 * num2));
				}
				if (quantity10 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidMethane.Remove(moleQuantity53), NitrousOxide.Remove(moleQuantity16), combustionRatio, out var combustionEnergy11, out var combustedFuel11, out var cleanBurnRatio12));
					zero += combustionEnergy11;
					burnedFuel += combustedFuel11;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio12, (float)(num8 * num2));
				}
				if (quantity11 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidMethane.Remove(moleQuantity54), LiquidNitrousOxide.Remove(moleQuantity23), combustionRatio, out var combustionEnergy12, out var combustedFuel12, out var cleanBurnRatio13));
					zero += combustionEnergy12;
					burnedFuel += combustedFuel12;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio13, (float)(num9 * num2));
				}
				if (quantity12 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidMethane.Remove(moleQuantity55), Ozone.Remove(moleQuantity30), combustionRatio, out var combustionEnergy13, out var combustedFuel13, out var cleanBurnRatio14));
					zero += combustionEnergy13;
					burnedFuel += combustedFuel13;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio14, (float)(num10 * num2));
				}
				if (quantity13 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidMethane.Remove(moleQuantity56), LiquidOzone.Remove(moleQuantity37), combustionRatio, out var combustionEnergy14, out var combustedFuel14, out var cleanBurnRatio15));
					zero += combustionEnergy14;
					burnedFuel += combustedFuel14;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio15, (float)(num11 * num2));
				}
			}
			if (quantity5 > MoleQuantity.Zero)
			{
				if (quantity8 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(Hydrogen.Remove(moleQuantity59), Oxygen.Remove(moleQuantity3), combustionRatio, out var combustionEnergy15, out var combustedFuel15, out var cleanBurnRatio16));
					zero += combustionEnergy15;
					burnedFuel += combustedFuel15;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio16, (float)(num6 * num3));
				}
				if (quantity9 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(Hydrogen.Remove(moleQuantity60), LiquidOxygen.Remove(moleQuantity10), combustionRatio, out var combustionEnergy16, out var combustedFuel16, out var cleanBurnRatio17));
					zero += combustionEnergy16;
					burnedFuel += combustedFuel16;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio17, (float)(num7 * num3));
				}
				if (quantity10 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(Hydrogen.Remove(moleQuantity61), NitrousOxide.Remove(moleQuantity17), combustionRatio, out var combustionEnergy17, out var combustedFuel17, out var cleanBurnRatio18));
					zero += combustionEnergy17;
					burnedFuel += combustedFuel17;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio18, (float)(num8 * num3));
				}
				if (quantity11 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(Hydrogen.Remove(moleQuantity62), LiquidNitrousOxide.Remove(moleQuantity24), combustionRatio, out var combustionEnergy18, out var combustedFuel18, out var cleanBurnRatio19));
					zero += combustionEnergy18;
					burnedFuel += combustedFuel18;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio19, (float)(num9 * num3));
				}
				if (quantity12 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(Hydrogen.Remove(moleQuantity63), Ozone.Remove(moleQuantity31), combustionRatio, out var combustionEnergy19, out var combustedFuel19, out var cleanBurnRatio20));
					zero += combustionEnergy19;
					burnedFuel += combustedFuel19;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio20, (float)(num10 * num3));
				}
				if (quantity13 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(Hydrogen.Remove(moleQuantity64), LiquidOzone.Remove(moleQuantity38), combustionRatio, out var combustionEnergy20, out var combustedFuel20, out var cleanBurnRatio21));
					zero += combustionEnergy20;
					burnedFuel += combustedFuel20;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio21, (float)(num11 * num3));
				}
			}
			if (quantity6 > MoleQuantity.Zero)
			{
				if (quantity8 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidHydrogen.Remove(moleQuantity67), Oxygen.Remove(moleQuantity4), combustionRatio, out var combustionEnergy21, out var combustedFuel21, out var cleanBurnRatio22));
					zero += combustionEnergy21;
					burnedFuel += combustedFuel21;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio22, (float)(num6 * num4));
				}
				if (quantity9 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidHydrogen.Remove(moleQuantity68), LiquidOxygen.Remove(moleQuantity11), combustionRatio, out var combustionEnergy22, out var combustedFuel22, out var cleanBurnRatio23));
					zero += combustionEnergy22;
					burnedFuel += combustedFuel22;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio23, (float)(num7 * num4));
				}
				if (quantity10 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidHydrogen.Remove(moleQuantity69), NitrousOxide.Remove(moleQuantity18), combustionRatio, out var combustionEnergy23, out var combustedFuel23, out var cleanBurnRatio24));
					zero += combustionEnergy23;
					burnedFuel += combustedFuel23;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio24, (float)(num8 * num4));
				}
				if (quantity11 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidHydrogen.Remove(moleQuantity70), LiquidNitrousOxide.Remove(moleQuantity25), combustionRatio, out var combustionEnergy24, out var combustedFuel24, out var cleanBurnRatio25));
					zero += combustionEnergy24;
					burnedFuel += combustedFuel24;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio25, (float)(num9 * num4));
				}
				if (quantity12 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidHydrogen.Remove(moleQuantity71), Ozone.Remove(moleQuantity32), combustionRatio, out var combustionEnergy25, out var combustedFuel25, out var cleanBurnRatio26));
					zero += combustionEnergy25;
					burnedFuel += combustedFuel25;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio26, (float)(num10 * num4));
				}
				if (quantity13 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidHydrogen.Remove(moleQuantity72), LiquidOzone.Remove(moleQuantity39), combustionRatio, out var combustionEnergy26, out var combustedFuel26, out var cleanBurnRatio27));
					zero += combustionEnergy26;
					burnedFuel += combustedFuel26;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio27, (float)(num11 * num4));
				}
			}
			if (quantity7 > MoleQuantity.Zero)
			{
				if (quantity8 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidAlcohol.Remove(moleQuantity75), Oxygen.Remove(moleQuantity5), combustionRatio, out var combustionEnergy27, out var combustedFuel27, out var cleanBurnRatio28));
					zero += combustionEnergy27;
					burnedFuel += combustedFuel27;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio28, (float)(num6 * num5));
				}
				if (quantity9 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidAlcohol.Remove(moleQuantity76), LiquidOxygen.Remove(moleQuantity12), combustionRatio, out var combustionEnergy28, out var combustedFuel28, out var cleanBurnRatio29));
					zero += combustionEnergy28;
					burnedFuel += combustedFuel28;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio29, (float)(num7 * num5));
				}
				if (quantity10 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidAlcohol.Remove(moleQuantity77), NitrousOxide.Remove(moleQuantity19), combustionRatio, out var combustionEnergy29, out var combustedFuel29, out var cleanBurnRatio30));
					zero += combustionEnergy29;
					burnedFuel += combustedFuel29;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio30, (float)(num8 * num5));
				}
				if (quantity11 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidAlcohol.Remove(moleQuantity78), LiquidNitrousOxide.Remove(moleQuantity26), combustionRatio, out var combustionEnergy30, out var combustedFuel30, out var cleanBurnRatio31));
					zero += combustionEnergy30;
					burnedFuel += combustedFuel30;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio31, (float)(num9 * num5));
				}
				if (quantity12 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidAlcohol.Remove(moleQuantity79), Ozone.Remove(moleQuantity33), combustionRatio, out var combustionEnergy31, out var combustedFuel31, out var cleanBurnRatio32));
					zero += combustionEnergy31;
					burnedFuel += combustedFuel31;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio32, (float)(num10 * num5));
				}
				if (quantity13 > MoleQuantity.Zero)
				{
					newGasMix.Add(Combustion.CombustMoles(LiquidAlcohol.Remove(moleQuantity80), LiquidOzone.Remove(moleQuantity40), combustionRatio, out var combustionEnergy32, out var combustedFuel32, out var cleanBurnRatio33));
					zero += combustionEnergy32;
					burnedFuel += combustedFuel32;
					cleanBurnRatio = Mathf.Lerp(cleanBurnRatio, cleanBurnRatio33, (float)(num11 * num5));
				}
			}
		}
		Add(newGasMix);
		return zero;
	}
}
