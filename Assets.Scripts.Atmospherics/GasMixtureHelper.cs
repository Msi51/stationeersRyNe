using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Atmospherics;

public static class GasMixtureHelper
{
	public static readonly GasMixture Invalid;

	public static GasMixture Create()
	{
		return new GasMixture(MoleQuantity.Zero);
	}

	public static GasMixture Create(GasMixture gasMixture)
	{
		return new GasMixture(gasMixture);
	}

	public static GasMixture Create(GlobalGasMix globalGasMix, AtmosphereHelper.MatterState matterState)
	{
		GasMixture result = Create();
		if (matterState == AtmosphereHelper.MatterState.Gas || matterState == AtmosphereHelper.MatterState.All)
		{
			result.Oxygen = new Mole(Chemistry.GasType.Oxygen, globalGasMix.Get(Chemistry.GasType.Oxygen), MoleEnergy.Zero);
			result.Nitrogen = new Mole(Chemistry.GasType.Nitrogen, globalGasMix.Get(Chemistry.GasType.Nitrogen), MoleEnergy.Zero);
			result.CarbonDioxide = new Mole(Chemistry.GasType.CarbonDioxide, globalGasMix.Get(Chemistry.GasType.CarbonDioxide), MoleEnergy.Zero);
			result.Methane = new Mole(Chemistry.GasType.Methane, globalGasMix.Get(Chemistry.GasType.Methane), MoleEnergy.Zero);
			result.Pollutant = new Mole(Chemistry.GasType.Pollutant, globalGasMix.Get(Chemistry.GasType.Pollutant), MoleEnergy.Zero);
			result.NitrousOxide = new Mole(Chemistry.GasType.NitrousOxide, globalGasMix.Get(Chemistry.GasType.NitrousOxide), MoleEnergy.Zero);
			result.Steam = new Mole(Chemistry.GasType.Steam, globalGasMix.Get(Chemistry.GasType.Steam), MoleEnergy.Zero);
			result.Hydrogen = new Mole(Chemistry.GasType.Hydrogen, globalGasMix.Get(Chemistry.GasType.Hydrogen), MoleEnergy.Zero);
			result.Hydrazine = new Mole(Chemistry.GasType.Hydrazine, globalGasMix.Get(Chemistry.GasType.Hydrazine), MoleEnergy.Zero);
			result.Helium = new Mole(Chemistry.GasType.Helium, globalGasMix.Get(Chemistry.GasType.Helium), MoleEnergy.Zero);
			result.Silanol = new Mole(Chemistry.GasType.Silanol, globalGasMix.Get(Chemistry.GasType.Silanol), MoleEnergy.Zero);
			result.HydrochloricAcid = new Mole(Chemistry.GasType.HydrochloricAcid, globalGasMix.Get(Chemistry.GasType.HydrochloricAcid), MoleEnergy.Zero);
			result.Ozone = new Mole(Chemistry.GasType.Ozone, globalGasMix.Get(Chemistry.GasType.Ozone), MoleEnergy.Zero);
		}
		if (matterState == AtmosphereHelper.MatterState.Liquid || matterState == AtmosphereHelper.MatterState.All)
		{
			result.LiquidOxygen = new Mole(Chemistry.GasType.LiquidOxygen, globalGasMix.Get(Chemistry.GasType.LiquidOxygen), MoleEnergy.Zero);
			result.LiquidNitrogen = new Mole(Chemistry.GasType.LiquidNitrogen, globalGasMix.Get(Chemistry.GasType.LiquidNitrogen), MoleEnergy.Zero);
			result.LiquidCarbonDioxide = new Mole(Chemistry.GasType.LiquidCarbonDioxide, globalGasMix.Get(Chemistry.GasType.LiquidCarbonDioxide), MoleEnergy.Zero);
			result.LiquidMethane = new Mole(Chemistry.GasType.LiquidMethane, globalGasMix.Get(Chemistry.GasType.LiquidMethane), MoleEnergy.Zero);
			result.LiquidPollutant = new Mole(Chemistry.GasType.LiquidPollutant, globalGasMix.Get(Chemistry.GasType.LiquidPollutant), MoleEnergy.Zero);
			result.LiquidNitrousOxide = new Mole(Chemistry.GasType.LiquidNitrousOxide, globalGasMix.Get(Chemistry.GasType.LiquidNitrousOxide), MoleEnergy.Zero);
			result.Water = new Mole(Chemistry.GasType.Water, globalGasMix.Get(Chemistry.GasType.Water), MoleEnergy.Zero);
			result.PollutedWater = new Mole(Chemistry.GasType.PollutedWater, globalGasMix.Get(Chemistry.GasType.PollutedWater), MoleEnergy.Zero);
			result.LiquidHydrogen = new Mole(Chemistry.GasType.LiquidHydrogen, globalGasMix.Get(Chemistry.GasType.LiquidHydrogen), MoleEnergy.Zero);
			result.LiquidHydrazine = new Mole(Chemistry.GasType.LiquidHydrazine, globalGasMix.Get(Chemistry.GasType.LiquidHydrazine), MoleEnergy.Zero);
			result.LiquidAlcohol = new Mole(Chemistry.GasType.LiquidAlcohol, globalGasMix.Get(Chemistry.GasType.LiquidAlcohol), MoleEnergy.Zero);
			result.LiquidSodiumChloride = new Mole(Chemistry.GasType.LiquidSodiumChloride, globalGasMix.Get(Chemistry.GasType.LiquidSodiumChloride), MoleEnergy.Zero);
			result.LiquidSilanol = new Mole(Chemistry.GasType.LiquidSilanol, globalGasMix.Get(Chemistry.GasType.LiquidSilanol), MoleEnergy.Zero);
			result.LiquidHydrochloricAcid = new Mole(Chemistry.GasType.LiquidHydrochloricAcid, globalGasMix.Get(Chemistry.GasType.LiquidHydrochloricAcid), MoleEnergy.Zero);
			result.LiquidOzone = new Mole(Chemistry.GasType.LiquidOzone, globalGasMix.Get(Chemistry.GasType.LiquidOzone), MoleEnergy.Zero);
		}
		result.TotalEnergy = new MoleEnergy(result.HeatCapacity, globalGasMix.GetGlobalGasMixTemperature(WorldSetting.Current.Data.GlobalAtmosphereData));
		return result;
	}

	public static Mole[] ReadOnlyMoles(GasMixture gasMixture)
	{
		return new Mole[28]
		{
			gasMixture.Oxygen, gasMixture.Nitrogen, gasMixture.CarbonDioxide, gasMixture.Methane, gasMixture.Pollutant, gasMixture.Water, gasMixture.PollutedWater, gasMixture.NitrousOxide, gasMixture.LiquidNitrogen, gasMixture.LiquidOxygen,
			gasMixture.LiquidMethane, gasMixture.Steam, gasMixture.LiquidCarbonDioxide, gasMixture.LiquidPollutant, gasMixture.LiquidNitrousOxide, gasMixture.Hydrogen, gasMixture.LiquidHydrogen, gasMixture.Hydrazine, gasMixture.LiquidHydrazine, gasMixture.LiquidAlcohol,
			gasMixture.Helium, gasMixture.LiquidSodiumChloride, gasMixture.Silanol, gasMixture.LiquidSilanol, gasMixture.HydrochloricAcid, gasMixture.LiquidHydrochloricAcid, gasMixture.Ozone, gasMixture.LiquidOzone
		};
	}

	public static GasMixture RemoveStackWorthOfFrozenGas(ref GasMixture frozenMix)
	{
		MoleQuantity removedMoles = new MoleQuantity(2500.0);
		GasMixture result = Create();
		result.Water.Set(frozenMix.Water.Remove(removedMoles));
		result.PollutedWater.Set(frozenMix.PollutedWater.Remove(removedMoles));
		result.Oxygen.Set(frozenMix.Oxygen.Remove(removedMoles));
		result.Nitrogen.Set(frozenMix.Nitrogen.Remove(removedMoles));
		result.CarbonDioxide.Set(frozenMix.CarbonDioxide.Remove(removedMoles));
		result.Methane.Set(frozenMix.Methane.Remove(removedMoles));
		result.Pollutant.Set(frozenMix.Pollutant.Remove(removedMoles));
		result.NitrousOxide.Set(frozenMix.NitrousOxide.Remove(removedMoles));
		result.LiquidNitrogen.Set(frozenMix.LiquidNitrogen.Remove(removedMoles));
		result.LiquidOxygen.Set(frozenMix.LiquidOxygen.Remove(removedMoles));
		result.LiquidMethane.Set(frozenMix.LiquidMethane.Remove(removedMoles));
		result.Steam.Set(frozenMix.Steam.Remove(removedMoles));
		result.LiquidCarbonDioxide.Set(frozenMix.LiquidCarbonDioxide.Remove(removedMoles));
		result.LiquidPollutant.Set(frozenMix.LiquidPollutant.Remove(removedMoles));
		result.LiquidNitrousOxide.Set(frozenMix.LiquidNitrousOxide.Remove(removedMoles));
		result.Hydrogen.Set(frozenMix.Hydrogen.Remove(removedMoles));
		result.LiquidHydrogen.Set(frozenMix.LiquidHydrogen.Remove(removedMoles));
		result.Hydrazine.Set(frozenMix.Hydrazine.Remove(removedMoles));
		result.LiquidHydrazine.Set(frozenMix.LiquidHydrazine.Remove(removedMoles));
		result.LiquidAlcohol.Set(frozenMix.LiquidAlcohol.Remove(removedMoles));
		result.Helium.Set(frozenMix.Helium.Remove(removedMoles));
		result.LiquidSodiumChloride.Set(frozenMix.LiquidSodiumChloride.Remove(removedMoles));
		result.Silanol.Set(frozenMix.Silanol.Remove(removedMoles));
		result.LiquidSilanol.Set(frozenMix.LiquidSilanol.Remove(removedMoles));
		result.HydrochloricAcid.Set(frozenMix.HydrochloricAcid.Remove(removedMoles));
		result.LiquidHydrochloricAcid.Set(frozenMix.LiquidHydrochloricAcid.Remove(removedMoles));
		result.Ozone.Set(frozenMix.Ozone.Remove(removedMoles));
		result.LiquidOzone.Set(frozenMix.LiquidOzone.Remove(removedMoles));
		return result;
	}

	public static float GasRatio(LogicType logicType, GasMixture gasMixture)
	{
		MoleQuantity zero = MoleQuantity.Zero;
		if (gasMixture.GetTotalMolesGassesAndLiquids <= MoleQuantity.Zero)
		{
			return 0f;
		}
		switch (logicType)
		{
		case LogicType.RatioOxygen:
		case LogicType.RatioOxygenInput:
		case LogicType.RatioOxygenInput2:
		case LogicType.RatioOxygenOutput:
		case LogicType.RatioOxygenOutput2:
			zero = gasMixture.Oxygen.Quantity;
			break;
		case LogicType.RatioCarbonDioxide:
		case LogicType.RatioCarbonDioxideInput:
		case LogicType.RatioCarbonDioxideInput2:
		case LogicType.RatioCarbonDioxideOutput:
		case LogicType.RatioCarbonDioxideOutput2:
			zero = gasMixture.CarbonDioxide.Quantity;
			break;
		case LogicType.RatioNitrogen:
		case LogicType.RatioNitrogenInput:
		case LogicType.RatioNitrogenInput2:
		case LogicType.RatioNitrogenOutput:
		case LogicType.RatioNitrogenOutput2:
			zero = gasMixture.Nitrogen.Quantity;
			break;
		case LogicType.RatioPollutant:
		case LogicType.RatioPollutantInput:
		case LogicType.RatioPollutantInput2:
		case LogicType.RatioPollutantOutput:
		case LogicType.RatioPollutantOutput2:
			zero = gasMixture.Pollutant.Quantity;
			break;
		case LogicType.RatioMethane:
		case LogicType.RatioMethaneInput:
		case LogicType.RatioMethaneInput2:
		case LogicType.RatioMethaneOutput:
		case LogicType.RatioMethaneOutput2:
			zero = gasMixture.Methane.Quantity;
			break;
		case LogicType.RatioWater:
		case LogicType.RatioWaterInput:
		case LogicType.RatioWaterInput2:
		case LogicType.RatioWaterOutput:
		case LogicType.RatioWaterOutput2:
			zero = gasMixture.Water.Quantity;
			break;
		case LogicType.RatioNitrousOxide:
		case LogicType.RatioNitrousOxideInput:
		case LogicType.RatioNitrousOxideInput2:
		case LogicType.RatioNitrousOxideOutput:
		case LogicType.RatioNitrousOxideOutput2:
			zero = gasMixture.NitrousOxide.Quantity;
			break;
		case LogicType.RatioLiquidNitrogen:
		case LogicType.RatioLiquidNitrogenInput:
		case LogicType.RatioLiquidNitrogenInput2:
		case LogicType.RatioLiquidNitrogenOutput:
		case LogicType.RatioLiquidNitrogenOutput2:
			zero = gasMixture.LiquidNitrogen.Quantity;
			break;
		case LogicType.RatioLiquidOxygen:
		case LogicType.RatioLiquidOxygenInput:
		case LogicType.RatioLiquidOxygenInput2:
		case LogicType.RatioLiquidOxygenOutput:
		case LogicType.RatioLiquidOxygenOutput2:
			zero = gasMixture.LiquidOxygen.Quantity;
			break;
		case LogicType.RatioLiquidMethane:
		case LogicType.RatioLiquidMethaneInput:
		case LogicType.RatioLiquidMethaneInput2:
		case LogicType.RatioLiquidMethaneOutput:
		case LogicType.RatioLiquidMethaneOutput2:
			zero = gasMixture.LiquidMethane.Quantity;
			break;
		case LogicType.RatioSteam:
		case LogicType.RatioSteamInput:
		case LogicType.RatioSteamInput2:
		case LogicType.RatioSteamOutput:
		case LogicType.RatioSteamOutput2:
			zero = gasMixture.Steam.Quantity;
			break;
		case LogicType.RatioLiquidCarbonDioxide:
		case LogicType.RatioLiquidCarbonDioxideInput:
		case LogicType.RatioLiquidCarbonDioxideInput2:
		case LogicType.RatioLiquidCarbonDioxideOutput:
		case LogicType.RatioLiquidCarbonDioxideOutput2:
			zero = gasMixture.LiquidCarbonDioxide.Quantity;
			break;
		case LogicType.RatioLiquidPollutant:
		case LogicType.RatioLiquidPollutantInput:
		case LogicType.RatioLiquidPollutantInput2:
		case LogicType.RatioLiquidPollutantOutput:
		case LogicType.RatioLiquidPollutantOutput2:
			zero = gasMixture.LiquidPollutant.Quantity;
			break;
		case LogicType.RatioLiquidNitrousOxide:
		case LogicType.RatioLiquidNitrousOxideInput:
		case LogicType.RatioLiquidNitrousOxideInput2:
		case LogicType.RatioLiquidNitrousOxideOutput:
		case LogicType.RatioLiquidNitrousOxideOutput2:
			zero = gasMixture.LiquidNitrousOxide.Quantity;
			break;
		case LogicType.RatioHydrogen:
		case LogicType.RatioHydrogenInput:
		case LogicType.RatioHydrogenInput2:
		case LogicType.RatioHydrogenOutput:
		case LogicType.RatioHydrogenOutput2:
			zero = gasMixture.Hydrogen.Quantity;
			break;
		case LogicType.RatioLiquidHydrogen:
		case LogicType.RatioLiquidHydrogenInput:
		case LogicType.RatioLiquidHydrogenInput2:
		case LogicType.RatioLiquidHydrogenOutput:
		case LogicType.RatioLiquidHydrogenOutput2:
			zero = gasMixture.LiquidHydrogen.Quantity;
			break;
		case LogicType.RatioHydrazine:
		case LogicType.RatioHydrazineInput:
		case LogicType.RatioHydrazineInput2:
		case LogicType.RatioHydrazineOutput:
		case LogicType.RatioHydrazineOutput2:
			zero = gasMixture.Hydrazine.Quantity;
			break;
		case LogicType.RatioLiquidHydrazine:
		case LogicType.RatioLiquidHydrazineInput:
		case LogicType.RatioLiquidHydrazineInput2:
		case LogicType.RatioLiquidHydrazineOutput:
		case LogicType.RatioLiquidHydrazineOutput2:
			zero = gasMixture.LiquidHydrazine.Quantity;
			break;
		case LogicType.RatioLiquidAlcohol:
		case LogicType.RatioLiquidAlcoholInput:
		case LogicType.RatioLiquidAlcoholInput2:
		case LogicType.RatioLiquidAlcoholOutput:
		case LogicType.RatioLiquidAlcoholOutput2:
			zero = gasMixture.LiquidAlcohol.Quantity;
			break;
		case LogicType.RatioHelium:
		case LogicType.RatioHeliumInput:
		case LogicType.RatioHeliumInput2:
		case LogicType.RatioHeliumOutput:
		case LogicType.RatioHeliumOutput2:
			zero = gasMixture.Helium.Quantity;
			break;
		case LogicType.RatioLiquidSodiumChloride:
		case LogicType.RatioLiquidSodiumChlorideInput:
		case LogicType.RatioLiquidSodiumChlorideInput2:
		case LogicType.RatioLiquidSodiumChlorideOutput:
		case LogicType.RatioLiquidSodiumChlorideOutput2:
			zero = gasMixture.LiquidSodiumChloride.Quantity;
			break;
		case LogicType.RatioPollutedWater:
		case LogicType.RatioPollutedWaterInput:
		case LogicType.RatioPollutedWaterInput2:
		case LogicType.RatioPollutedWaterOutput:
		case LogicType.RatioPollutedWaterOutput2:
			zero = gasMixture.PollutedWater.Quantity;
			break;
		case LogicType.RatioSilanol:
		case LogicType.RatioSilanolInput:
		case LogicType.RatioSilanolInput2:
		case LogicType.RatioSilanolOutput:
		case LogicType.RatioSilanolOutput2:
			zero = gasMixture.Silanol.Quantity;
			break;
		case LogicType.RatioLiquidSilanol:
		case LogicType.RatioLiquidSilanolInput:
		case LogicType.RatioLiquidSilanolInput2:
		case LogicType.RatioLiquidSilanolOutput:
		case LogicType.RatioLiquidSilanolOutput2:
			zero = gasMixture.LiquidSilanol.Quantity;
			break;
		case LogicType.RatioHydrochloricAcid:
		case LogicType.RatioHydrochloricAcidInput:
		case LogicType.RatioHydrochloricAcidInput2:
		case LogicType.RatioHydrochloricAcidOutput:
		case LogicType.RatioHydrochloricAcidOutput2:
			zero = gasMixture.HydrochloricAcid.Quantity;
			break;
		case LogicType.RatioLiquidHydrochloricAcid:
		case LogicType.RatioLiquidHydrochloricAcidInput:
		case LogicType.RatioLiquidHydrochloricAcidInput2:
		case LogicType.RatioLiquidHydrochloricAcidOutput:
		case LogicType.RatioLiquidHydrochloricAcidOutput2:
			zero = gasMixture.LiquidHydrochloricAcid.Quantity;
			break;
		case LogicType.RatioOzone:
		case LogicType.RatioOzoneInput:
		case LogicType.RatioOzoneInput2:
		case LogicType.RatioOzoneOutput:
		case LogicType.RatioOzoneOutput2:
			zero = gasMixture.Ozone.Quantity;
			break;
		case LogicType.RatioLiquidOzone:
		case LogicType.RatioLiquidOzoneInput:
		case LogicType.RatioLiquidOzoneInput2:
		case LogicType.RatioLiquidOzoneOutput:
		case LogicType.RatioLiquidOzoneOutput2:
			zero = gasMixture.LiquidOzone.Quantity;
			break;
		default:
			return -1f;
		}
		return zero.ToFloat() / gasMixture.GetTotalMolesGassesAndLiquids.ToFloat();
	}
}
