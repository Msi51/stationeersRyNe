using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using ImGuiNET;
using UnityEngine;
using Weather;

namespace Assets.Scripts;

public class GlobalGasMix
{
	public string DisplayName = "Global Gas Mix";

	private MoleQuantity Oxygen;

	private MoleQuantity Nitrogen;

	private MoleQuantity CarbonDioxide;

	private MoleQuantity Methane;

	private MoleQuantity Pollutant;

	private MoleQuantity Water;

	private MoleQuantity PollutedWater;

	private MoleQuantity NitrousOxide;

	private MoleQuantity LiquidNitrogen;

	private MoleQuantity LiquidOxygen;

	private MoleQuantity LiquidMethane;

	private MoleQuantity Steam;

	private MoleQuantity LiquidCarbonDioxide;

	private MoleQuantity LiquidPollutant;

	private MoleQuantity LiquidNitrousOxide;

	private MoleQuantity Hydrogen;

	private MoleQuantity LiquidHydrogen;

	private MoleQuantity Hydrazine;

	private MoleQuantity LiquidHydrazine;

	private MoleQuantity Alcohol;

	private MoleQuantity Helium;

	private MoleQuantity LiquidSodiumChloride;

	private MoleQuantity Silanol;

	private MoleQuantity LiquidSilanol;

	private MoleQuantity HydrochloricAcid;

	private MoleQuantity LiquidHydrochloricAcid;

	private MoleQuantity Ozone;

	private MoleQuantity LiquidOzone;

	public static TemperatureKelvin GlobalTemperatureStateChangeOffset = new TemperatureKelvin(2.0);

	private MoleQuantity _added = MoleQuantity.Zero;

	private MoleQuantity _removed = MoleQuantity.Zero;

	public VolumeLitres Volume { get; private set; }

	public GlobalGasMix(VolumeLitres volume)
	{
		Volume = volume;
		Oxygen = MoleQuantity.Zero;
		Nitrogen = MoleQuantity.Zero;
		CarbonDioxide = MoleQuantity.Zero;
		Methane = MoleQuantity.Zero;
		Pollutant = MoleQuantity.Zero;
		Water = MoleQuantity.Zero;
		PollutedWater = MoleQuantity.Zero;
		NitrousOxide = MoleQuantity.Zero;
		LiquidNitrogen = MoleQuantity.Zero;
		LiquidOxygen = MoleQuantity.Zero;
		LiquidMethane = MoleQuantity.Zero;
		Steam = MoleQuantity.Zero;
		LiquidCarbonDioxide = MoleQuantity.Zero;
		LiquidPollutant = MoleQuantity.Zero;
		LiquidNitrousOxide = MoleQuantity.Zero;
		Hydrogen = MoleQuantity.Zero;
		LiquidHydrogen = MoleQuantity.Zero;
		Hydrazine = MoleQuantity.Zero;
		LiquidHydrazine = MoleQuantity.Zero;
		Alcohol = MoleQuantity.Zero;
		Helium = MoleQuantity.Zero;
		LiquidSodiumChloride = MoleQuantity.Zero;
		Silanol = MoleQuantity.Zero;
		LiquidSilanol = MoleQuantity.Zero;
		HydrochloricAcid = MoleQuantity.Zero;
		LiquidHydrochloricAcid = MoleQuantity.Zero;
		Ozone = MoleQuantity.Zero;
		LiquidOzone = MoleQuantity.Zero;
	}

	public static GlobalGasMix Create(GlobalAtmosphereData data)
	{
		GlobalGasMix globalGasMix = new GlobalGasMix(data.GetVolume());
		foreach (GlobalMoleData globalMoleData in data.GlobalGasMixData.GlobalMoleDatas)
		{
			globalGasMix.ApplyGlobalMoleData(globalMoleData);
		}
		return globalGasMix;
	}

	public void Load(GlobalGasMixSaveData saveData)
	{
		Volume = new VolumeLitres(saveData.Volume.Value);
		Oxygen = new MoleQuantity(saveData.Oxygen.Value);
		Nitrogen = new MoleQuantity(saveData.Nitrogen.Value);
		CarbonDioxide = new MoleQuantity(saveData.CarbonDioxide.Value);
		Methane = new MoleQuantity(saveData.Methane.Value);
		Pollutant = new MoleQuantity(saveData.Pollutant.Value);
		Water = new MoleQuantity(saveData.Water.Value);
		PollutedWater = new MoleQuantity(saveData.PollutedWater.Value);
		NitrousOxide = new MoleQuantity(saveData.NitrousOxide.Value);
		LiquidNitrogen = new MoleQuantity(saveData.LiquidNitrogen.Value);
		LiquidOxygen = new MoleQuantity(saveData.LiquidOxygen.Value);
		LiquidMethane = new MoleQuantity(saveData.LiquidMethane.Value);
		Steam = new MoleQuantity(saveData.Steam.Value);
		LiquidCarbonDioxide = new MoleQuantity(saveData.LiquidCarbonDioxide.Value);
		LiquidPollutant = new MoleQuantity(saveData.LiquidPollutant.Value);
		LiquidNitrousOxide = new MoleQuantity(saveData.LiquidNitrousOxide.Value);
		Hydrogen = new MoleQuantity(saveData.Hydrogen.Value);
		LiquidHydrogen = new MoleQuantity(saveData.LiquidHydrogen.Value);
		Hydrazine = new MoleQuantity(saveData.Hydrazine?.Value ?? 0.0);
		LiquidHydrazine = new MoleQuantity(saveData.LiquidHydrazine?.Value ?? 0.0);
		Alcohol = new MoleQuantity(saveData.LiquidAlcohol?.Value ?? 0.0);
		Helium = new MoleQuantity(saveData.Helium?.Value ?? 0.0);
		LiquidSodiumChloride = new MoleQuantity(saveData.LiquidSodiumChloride?.Value ?? 0.0);
		Silanol = new MoleQuantity(saveData.Silanol?.Value ?? 0.0);
		LiquidSilanol = new MoleQuantity(saveData.LiquidSilanol?.Value ?? 0.0);
		HydrochloricAcid = new MoleQuantity(saveData.HydrochloricAcid?.Value ?? 0.0);
		LiquidHydrochloricAcid = new MoleQuantity(saveData.LiquidHydrochloricAcid?.Value ?? 0.0);
		Ozone = new MoleQuantity(saveData.Ozone?.Value ?? 0.0);
		LiquidOzone = new MoleQuantity(saveData.LiquidOzone?.Value ?? 0.0);
	}

	public GasMixture ToInstancedGasMixture()
	{
		return GasMixtureHelper.Create(this, AtmosphereHelper.MatterState.Gas);
	}

	public void Add(GlobalGasMix other, AtmosphereHelper.MatterState matterState)
	{
		switch (matterState)
		{
		case AtmosphereHelper.MatterState.Liquid:
			AddLiquid(other);
			break;
		case AtmosphereHelper.MatterState.Gas:
			AddGas(other);
			break;
		case AtmosphereHelper.MatterState.All:
			AddLiquid(other);
			AddGas(other);
			break;
		default:
			throw new ArgumentOutOfRangeException("matterState", matterState, null);
		}
	}

	private void AddGas(GlobalGasMix other)
	{
		Oxygen += other.Oxygen;
		Nitrogen += other.Nitrogen;
		CarbonDioxide += other.CarbonDioxide;
		Methane += other.Methane;
		Pollutant += other.Pollutant;
		NitrousOxide += other.NitrousOxide;
		Steam += other.Steam;
		Hydrogen += other.Hydrogen;
		Hydrazine += other.Hydrazine;
		Helium += other.Helium;
		Silanol += other.Silanol;
		HydrochloricAcid += other.HydrochloricAcid;
		Ozone += other.Ozone;
	}

	private void AddLiquid(GlobalGasMix other)
	{
		Water += other.Water;
		PollutedWater += other.PollutedWater;
		LiquidNitrogen += other.LiquidNitrogen;
		LiquidOxygen += other.LiquidOxygen;
		LiquidMethane += other.LiquidMethane;
		LiquidCarbonDioxide += other.LiquidCarbonDioxide;
		LiquidPollutant += other.LiquidPollutant;
		LiquidNitrousOxide += other.LiquidNitrousOxide;
		LiquidHydrogen += other.LiquidHydrogen;
		LiquidHydrazine += other.LiquidHydrazine;
		Alcohol += other.Alcohol;
		LiquidSodiumChloride += other.LiquidSodiumChloride;
		LiquidSilanol += other.LiquidSilanol;
		LiquidHydrochloricAcid += other.LiquidHydrochloricAcid;
		LiquidOzone += other.LiquidOzone;
	}

	public void Scale(double scale)
	{
		Oxygen *= scale;
		Nitrogen *= scale;
		CarbonDioxide *= scale;
		Methane *= scale;
		Pollutant *= scale;
		Water *= scale;
		PollutedWater *= scale;
		NitrousOxide *= scale;
		LiquidNitrogen *= scale;
		LiquidOxygen *= scale;
		LiquidMethane *= scale;
		Steam *= scale;
		LiquidCarbonDioxide *= scale;
		LiquidPollutant *= scale;
		LiquidNitrousOxide *= scale;
		Hydrogen *= scale;
		LiquidHydrogen *= scale;
		Hydrazine *= scale;
		LiquidHydrazine *= scale;
		Alcohol *= scale;
		Helium *= scale;
		LiquidSodiumChloride *= scale;
		Silanol *= scale;
		LiquidSilanol *= scale;
		HydrochloricAcid *= scale;
		LiquidHydrochloricAcid *= scale;
		Ozone *= scale;
		LiquidOzone *= scale;
	}

	public void Remove(GasMixture toRemove)
	{
		Oxygen -= toRemove.Oxygen.Quantity;
		Nitrogen -= toRemove.Nitrogen.Quantity;
		CarbonDioxide -= toRemove.CarbonDioxide.Quantity;
		Methane -= toRemove.Methane.Quantity;
		Pollutant -= toRemove.Pollutant.Quantity;
		Water -= toRemove.Water.Quantity;
		PollutedWater -= toRemove.PollutedWater.Quantity;
		NitrousOxide -= toRemove.NitrousOxide.Quantity;
		LiquidNitrogen -= toRemove.LiquidNitrogen.Quantity;
		LiquidOxygen -= toRemove.LiquidOxygen.Quantity;
		LiquidMethane -= toRemove.LiquidMethane.Quantity;
		Steam -= toRemove.Steam.Quantity;
		LiquidCarbonDioxide -= toRemove.LiquidCarbonDioxide.Quantity;
		LiquidPollutant -= toRemove.LiquidPollutant.Quantity;
		LiquidNitrousOxide -= toRemove.LiquidNitrousOxide.Quantity;
		Hydrogen -= toRemove.Hydrogen.Quantity;
		LiquidHydrogen -= toRemove.LiquidHydrogen.Quantity;
		Hydrazine -= toRemove.Hydrazine.Quantity;
		LiquidHydrazine -= toRemove.LiquidHydrazine.Quantity;
		Alcohol -= toRemove.LiquidAlcohol.Quantity;
		Helium -= toRemove.Helium.Quantity;
		LiquidSodiumChloride -= toRemove.LiquidSodiumChloride.Quantity;
		Silanol -= toRemove.Silanol.Quantity;
		LiquidSilanol -= toRemove.LiquidSilanol.Quantity;
		HydrochloricAcid -= toRemove.HydrochloricAcid.Quantity;
		LiquidHydrochloricAcid -= toRemove.LiquidHydrochloricAcid.Quantity;
		Ozone -= toRemove.Ozone.Quantity;
		LiquidOzone -= toRemove.LiquidOzone.Quantity;
		_removed += toRemove.GetTotalMolesGassesAndLiquids;
	}

	public GasMixture Remove(MoleQuantity totalMolesRemoved, AtmosphereHelper.MatterState stateToRemove)
	{
		if (RocketMath.Min(TotalQuantity(stateToRemove), totalMolesRemoved) <= MoleQuantity.Zero)
		{
			return GasMixtureHelper.Invalid;
		}
		GasMixture gasMixture = ToInstancedGasMixture().Remove(totalMolesRemoved, stateToRemove);
		Remove(gasMixture);
		return gasMixture;
	}

	public void Add(GasMixture add)
	{
		Oxygen += add.Oxygen.Quantity;
		Nitrogen += add.Nitrogen.Quantity;
		CarbonDioxide += add.CarbonDioxide.Quantity;
		Methane += add.Methane.Quantity;
		Pollutant += add.Pollutant.Quantity;
		Water += add.Water.Quantity;
		PollutedWater += add.PollutedWater.Quantity;
		NitrousOxide += add.NitrousOxide.Quantity;
		LiquidNitrogen += add.LiquidNitrogen.Quantity;
		LiquidOxygen += add.LiquidOxygen.Quantity;
		LiquidMethane += add.LiquidMethane.Quantity;
		Steam += add.Steam.Quantity;
		LiquidCarbonDioxide += add.LiquidCarbonDioxide.Quantity;
		LiquidPollutant += add.LiquidPollutant.Quantity;
		LiquidNitrousOxide += add.LiquidNitrousOxide.Quantity;
		Hydrogen += add.Hydrogen.Quantity;
		LiquidHydrogen += add.LiquidHydrogen.Quantity;
		Hydrazine += add.Hydrazine.Quantity;
		LiquidHydrazine += add.LiquidHydrazine.Quantity;
		Alcohol += add.LiquidAlcohol.Quantity;
		Helium += add.Helium.Quantity;
		LiquidSodiumChloride += add.LiquidSodiumChloride.Quantity;
		Silanol += add.Silanol.Quantity;
		LiquidSilanol += add.LiquidSilanol.Quantity;
		HydrochloricAcid += add.HydrochloricAcid.Quantity;
		LiquidHydrochloricAcid += add.LiquidHydrochloricAcid.Quantity;
		Ozone += add.Ozone.Quantity;
		LiquidOzone += add.LiquidOzone.Quantity;
		_added += add.GetTotalMolesGassesAndLiquids;
	}

	public void ClearQuantities(AtmosphereHelper.MatterState matterState)
	{
		switch (matterState)
		{
		case AtmosphereHelper.MatterState.Liquid:
			ClearLiquid();
			break;
		case AtmosphereHelper.MatterState.Gas:
			ClearGas();
			break;
		case AtmosphereHelper.MatterState.All:
			ClearLiquid();
			ClearGas();
			break;
		default:
			throw new ArgumentOutOfRangeException("matterState", matterState, null);
		}
	}

	private void ClearGas()
	{
		Oxygen = MoleQuantity.Zero;
		Nitrogen = MoleQuantity.Zero;
		CarbonDioxide = MoleQuantity.Zero;
		Methane = MoleQuantity.Zero;
		Pollutant = MoleQuantity.Zero;
		NitrousOxide = MoleQuantity.Zero;
		Steam = MoleQuantity.Zero;
		Hydrogen = MoleQuantity.Zero;
		Hydrazine = MoleQuantity.Zero;
		Helium = MoleQuantity.Zero;
		Silanol = MoleQuantity.Zero;
		HydrochloricAcid = MoleQuantity.Zero;
		Ozone = MoleQuantity.Zero;
	}

	private void ClearLiquid()
	{
		Water = MoleQuantity.Zero;
		PollutedWater = MoleQuantity.Zero;
		LiquidNitrogen = MoleQuantity.Zero;
		LiquidOxygen = MoleQuantity.Zero;
		LiquidMethane = MoleQuantity.Zero;
		LiquidCarbonDioxide = MoleQuantity.Zero;
		LiquidPollutant = MoleQuantity.Zero;
		LiquidNitrousOxide = MoleQuantity.Zero;
		LiquidHydrogen = MoleQuantity.Zero;
		LiquidHydrazine = MoleQuantity.Zero;
		Alcohol = MoleQuantity.Zero;
		LiquidSodiumChloride = MoleQuantity.Zero;
		LiquidSilanol = MoleQuantity.Zero;
		LiquidHydrochloricAcid = MoleQuantity.Zero;
		LiquidOzone = MoleQuantity.Zero;
	}

	public MoleQuantity Get(Chemistry.GasType gasType)
	{
		return gasType switch
		{
			Chemistry.GasType.Oxygen => Oxygen, 
			Chemistry.GasType.Nitrogen => Nitrogen, 
			Chemistry.GasType.CarbonDioxide => CarbonDioxide, 
			Chemistry.GasType.Methane => Methane, 
			Chemistry.GasType.Pollutant => Pollutant, 
			Chemistry.GasType.Water => Water, 
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
			Chemistry.GasType.PollutedWater => PollutedWater, 
			Chemistry.GasType.Hydrazine => Hydrazine, 
			Chemistry.GasType.LiquidHydrazine => LiquidHydrazine, 
			Chemistry.GasType.LiquidAlcohol => Alcohol, 
			Chemistry.GasType.Helium => Helium, 
			Chemistry.GasType.LiquidSodiumChloride => LiquidSodiumChloride, 
			Chemistry.GasType.Silanol => Silanol, 
			Chemistry.GasType.LiquidSilanol => LiquidSilanol, 
			Chemistry.GasType.HydrochloricAcid => HydrochloricAcid, 
			Chemistry.GasType.LiquidHydrochloricAcid => LiquidHydrochloricAcid, 
			Chemistry.GasType.Ozone => Ozone, 
			Chemistry.GasType.LiquidOzone => LiquidOzone, 
			Chemistry.GasType.Undefined => MoleQuantity.Zero, 
			_ => throw new ArgumentOutOfRangeException("gasType", gasType, null), 
		};
	}

	public void Set(MoleQuantity quantity, Chemistry.GasType gasType)
	{
		switch (gasType)
		{
		case Chemistry.GasType.Oxygen:
			Oxygen = quantity;
			break;
		case Chemistry.GasType.Nitrogen:
			Nitrogen = quantity;
			break;
		case Chemistry.GasType.CarbonDioxide:
			CarbonDioxide = quantity;
			break;
		case Chemistry.GasType.Methane:
			Methane = quantity;
			break;
		case Chemistry.GasType.Pollutant:
			Pollutant = quantity;
			break;
		case Chemistry.GasType.Water:
			Water = quantity;
			break;
		case Chemistry.GasType.PollutedWater:
			PollutedWater = quantity;
			break;
		case Chemistry.GasType.NitrousOxide:
			NitrousOxide = quantity;
			break;
		case Chemistry.GasType.LiquidNitrogen:
			LiquidNitrogen = quantity;
			break;
		case Chemistry.GasType.LiquidOxygen:
			LiquidOxygen = quantity;
			break;
		case Chemistry.GasType.LiquidMethane:
			LiquidMethane = quantity;
			break;
		case Chemistry.GasType.Steam:
			Steam = quantity;
			break;
		case Chemistry.GasType.LiquidCarbonDioxide:
			LiquidCarbonDioxide = quantity;
			break;
		case Chemistry.GasType.LiquidPollutant:
			LiquidPollutant = quantity;
			break;
		case Chemistry.GasType.LiquidNitrousOxide:
			LiquidNitrousOxide = quantity;
			break;
		case Chemistry.GasType.Hydrogen:
			Hydrogen = quantity;
			break;
		case Chemistry.GasType.LiquidHydrogen:
			LiquidHydrogen = quantity;
			break;
		case Chemistry.GasType.Hydrazine:
			Hydrazine = quantity;
			break;
		case Chemistry.GasType.LiquidHydrazine:
			LiquidHydrazine = quantity;
			break;
		case Chemistry.GasType.LiquidAlcohol:
			Alcohol = quantity;
			break;
		case Chemistry.GasType.Helium:
			Helium = quantity;
			break;
		case Chemistry.GasType.LiquidSodiumChloride:
			LiquidSodiumChloride = quantity;
			break;
		case Chemistry.GasType.Silanol:
			Silanol = quantity;
			break;
		case Chemistry.GasType.LiquidSilanol:
			LiquidSilanol = quantity;
			break;
		case Chemistry.GasType.HydrochloricAcid:
			HydrochloricAcid = quantity;
			break;
		case Chemistry.GasType.LiquidHydrochloricAcid:
			LiquidHydrochloricAcid = quantity;
			break;
		case Chemistry.GasType.Ozone:
			Ozone = quantity;
			break;
		case Chemistry.GasType.LiquidOzone:
			LiquidOzone = quantity;
			break;
		default:
			throw new ArgumentOutOfRangeException("gasType", gasType, null);
		}
	}

	public HeatCapacity GetHeatCapacity()
	{
		return new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.Oxygen), Oxygen) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.Nitrogen), Nitrogen) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.CarbonDioxide), CarbonDioxide) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.Methane), Methane) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.Pollutant), Pollutant) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.Water), Water) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.PollutedWater), PollutedWater) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.NitrousOxide), NitrousOxide) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.LiquidNitrogen), LiquidNitrogen) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.LiquidOxygen), LiquidOxygen) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.LiquidMethane), LiquidMethane) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.Steam), Steam) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.LiquidCarbonDioxide), LiquidCarbonDioxide) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.LiquidPollutant), LiquidPollutant) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.LiquidNitrousOxide), LiquidNitrousOxide) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.Hydrogen), Hydrogen) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.LiquidHydrogen), LiquidHydrogen) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.Hydrazine), Hydrazine) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.LiquidHydrazine), LiquidHydrazine) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.LiquidAlcohol), Alcohol) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.Helium), Helium) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.LiquidSodiumChloride), LiquidSodiumChloride) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.Silanol), Silanol) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.LiquidSilanol), LiquidSilanol) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.HydrochloricAcid), HydrochloricAcid) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.LiquidHydrochloricAcid), LiquidHydrochloricAcid) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.Ozone), Ozone) + new HeatCapacity(Mole.SpecificHeat(Chemistry.GasType.LiquidOzone), LiquidOzone);
	}

	public MoleQuantity TotalQuantity(AtmosphereHelper.MatterState matterState = AtmosphereHelper.MatterState.All)
	{
		return matterState switch
		{
			AtmosphereHelper.MatterState.Liquid => TotalQuantityLiquid(), 
			AtmosphereHelper.MatterState.Gas => TotalQuantityGas(), 
			AtmosphereHelper.MatterState.All => TotalQuantityGas() + TotalQuantityLiquid(), 
			_ => MoleQuantity.Zero, 
		};
	}

	public MoleQuantity TotalQuantityGas()
	{
		return Oxygen + Nitrogen + CarbonDioxide + Methane + Pollutant + Steam + NitrousOxide + Hydrogen + Hydrazine + Helium + Silanol + HydrochloricAcid + Ozone;
	}

	public MoleQuantity TotalQuantityLiquid()
	{
		return Water + PollutedWater + LiquidOxygen + LiquidNitrogen + LiquidMethane + LiquidCarbonDioxide + LiquidPollutant + LiquidNitrousOxide + LiquidHydrogen + LiquidHydrazine + Alcohol + LiquidSodiumChloride + LiquidSilanol + LiquidHydrochloricAcid + LiquidOzone;
	}

	public VolumeLitres VolumeForGas()
	{
		return Volume - VolumeOfLiquid();
	}

	public VolumeLitres VolumeOfLiquid()
	{
		return Mole.MolarVolume(Chemistry.GasType.Water) * Water.ToDouble() * MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.Water) + Mole.MolarVolume(Chemistry.GasType.PollutedWater) * PollutedWater.ToDouble() * MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.PollutedWater) + Mole.MolarVolume(Chemistry.GasType.LiquidOxygen) * LiquidOxygen.ToDouble() * MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidOxygen) + Mole.MolarVolume(Chemistry.GasType.LiquidNitrogen) * LiquidNitrogen.ToDouble() * MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidNitrogen) + Mole.MolarVolume(Chemistry.GasType.LiquidMethane) * LiquidMethane.ToDouble() * MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidMethane) + Mole.MolarVolume(Chemistry.GasType.LiquidCarbonDioxide) * LiquidCarbonDioxide.ToDouble() * MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidCarbonDioxide) + Mole.MolarVolume(Chemistry.GasType.LiquidPollutant) * LiquidPollutant.ToDouble() * MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidPollutant) + Mole.MolarVolume(Chemistry.GasType.LiquidNitrousOxide) * LiquidNitrousOxide.ToDouble() * MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidNitrousOxide) + Mole.MolarVolume(Chemistry.GasType.LiquidHydrogen) * LiquidHydrogen.ToDouble() * MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidHydrogen) + Mole.MolarVolume(Chemistry.GasType.LiquidHydrazine) * LiquidHydrazine.ToDouble() * MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidHydrazine) + Mole.MolarVolume(Chemistry.GasType.LiquidAlcohol) * Alcohol.ToDouble() * MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidAlcohol) + Mole.MolarVolume(Chemistry.GasType.LiquidSodiumChloride) * LiquidSodiumChloride.ToDouble() * MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidSodiumChloride) + Mole.MolarVolume(Chemistry.GasType.LiquidSilanol) * LiquidSilanol.ToDouble() * MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidSilanol) + Mole.MolarVolume(Chemistry.GasType.LiquidHydrochloricAcid) * LiquidHydrochloricAcid.ToDouble() * MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidHydrochloricAcid) + Mole.MolarVolume(Chemistry.GasType.LiquidOzone) * LiquidOzone.ToDouble() * MoleHelper.GetInWorldVolumeMultiplier(Chemistry.GasType.LiquidOzone);
	}

	public TemperatureKelvin GetGlobalGasMixTemperature(GlobalAtmosphereData data)
	{
		return GetGlobalGasMixTemperature(data, Vector3.Angle(Vector3.up, OrbitalSimulation.WorldSunVector), OrbitalSimulation.System.GetSolarEnergyPercentClamped(OrbitalSimulation.System.GetSolarEnergy(), OrbitalSimulation.System.CalculateSolarIrradiance()));
	}

	public TemperatureKelvin GetGlobalGasMixTemperature(GlobalAtmosphereData data, float solarAngle, float solarEnergyPercent)
	{
		float ghgIndex = TerraForming.GetGhgIndex(this);
		double milliMolesPerLitre = IdealGas.GetMilliMolesPerLitre(Volume, TotalQuantityGas());
		TemperatureKelvin solarAngleTemperature = data.GetSolarAngleTemperature(solarAngle);
		if (WeatherManager.IsWeatherEventRunning && WeatherManager.CurrentWeatherEvent != null)
		{
			solarAngleTemperature += new TemperatureKelvin(WeatherManager.CurrentWeatherEvent.TemperatureOffset.GetOffset(solarAngle));
		}
		TemperatureKelvin solarDistanceTemperatureOffset = data.GetSolarDistanceTemperatureOffset(solarAngle, solarEnergyPercent);
		TemperatureKelvin gHGTemperatureOffset = data.GetGHGTemperatureOffset(solarAngle, ghgIndex);
		TemperatureKelvin densityOffset = data.GetDensityOffset(solarAngle, milliMolesPerLitre);
		TemperatureKelvin latentTemperatureOffset = PlanetaryAtmosphereSimulation.GetLatentTemperatureOffset();
		TemperatureKelvin externalInputEnergyOffset = PlanetaryAtmosphereSimulation.GetExternalInputEnergyOffset();
		if (!solarDistanceTemperatureOffset.IsNaN())
		{
			solarAngleTemperature += solarDistanceTemperatureOffset;
		}
		if (!gHGTemperatureOffset.IsNaN())
		{
			solarAngleTemperature += gHGTemperatureOffset;
		}
		if (!densityOffset.IsNaN())
		{
			solarAngleTemperature += densityOffset;
		}
		if (!latentTemperatureOffset.IsNaN())
		{
			solarAngleTemperature += latentTemperatureOffset;
		}
		if (!externalInputEnergyOffset.IsNaN())
		{
			solarAngleTemperature += externalInputEnergyOffset;
		}
		return solarAngleTemperature;
	}

	private void ApplyGlobalMoleData(GlobalMoleData globalMoleData)
	{
		switch (globalMoleData.Type)
		{
		case Chemistry.GasType.Oxygen:
			Oxygen = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.Nitrogen:
			Nitrogen = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.CarbonDioxide:
			CarbonDioxide = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.Methane:
			Methane = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.Pollutant:
			Pollutant = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.Water:
			Water = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.NitrousOxide:
			NitrousOxide = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.LiquidNitrogen:
			LiquidNitrogen = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.LiquidOxygen:
			LiquidOxygen = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.LiquidMethane:
			LiquidMethane = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.Steam:
			Steam = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.LiquidCarbonDioxide:
			LiquidCarbonDioxide = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.LiquidPollutant:
			LiquidPollutant = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.LiquidNitrousOxide:
			LiquidNitrousOxide = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.Hydrogen:
			Hydrogen = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.LiquidHydrogen:
			LiquidHydrogen = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.PollutedWater:
			PollutedWater = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.Hydrazine:
			Hydrazine = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.LiquidHydrazine:
			LiquidHydrazine = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.LiquidAlcohol:
			Alcohol = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.Helium:
			Helium = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.LiquidSodiumChloride:
			LiquidSodiumChloride = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.Silanol:
			Silanol = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.LiquidSilanol:
			LiquidSilanol = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.HydrochloricAcid:
			HydrochloricAcid = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.LiquidHydrochloricAcid:
			LiquidHydrochloricAcid = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.Ozone:
			Ozone = globalMoleData.ToMoleQuantity(Volume);
			break;
		case Chemistry.GasType.LiquidOzone:
			LiquidOzone = globalMoleData.ToMoleQuantity(Volume);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case Chemistry.GasType.Undefined:
			break;
		}
	}

	public MoleEnergy StateChangeMoleQuantity(Chemistry.GasType gasType, GlobalGasMix output)
	{
		MoleQuantity moleQuantity = Get(gasType);
		if (moleQuantity <= MoleQuantity.Zero)
		{
			return MoleEnergy.Zero;
		}
		TemperatureKelvin globalGasMixTemperature = GetGlobalGasMixTemperature(WorldSetting.Current.Data.GlobalAtmosphereData);
		MoleEnergy moleEnergy = IdealGas.Energy(new HeatCapacity(Mole.SpecificHeat(gasType), moleQuantity), globalGasMixTemperature);
		Mole mole = new Mole(gasType, moleQuantity, moleEnergy);
		if (globalGasMixTemperature < mole.FreezingTemperature() + GlobalTemperatureStateChangeOffset)
		{
			return MoleEnergy.Zero;
		}
		VolumeLitres volumeLitres = VolumeForGas();
		PressurekPa pressure = new PressurekPa(TotalQuantityGas(), globalGasMixTemperature, volumeLitres);
		Mole mole2 = mole.ChangeState(pressure, volumeLitres, GlobalTemperatureStateChangeOffset);
		if (!mole2.IsValid)
		{
			return MoleEnergy.Zero;
		}
		MoleQuantity quantity = mole2.Quantity;
		MoleEnergy result = mole2.Energy + mole.Energy - moleEnergy;
		Set(moleQuantity - quantity, gasType);
		Chemistry.GasType gasType2 = ((Mole.MatterState(gasType) == AtmosphereHelper.MatterState.Liquid) ? MoleHelper.EvaporationType(gasType) : MoleHelper.CondensationType(gasType));
		output.Set(output.Get(gasType2) + quantity, gasType2);
		return result;
	}

	public MoleEnergy TryEvaporateGroundLiquid(Chemistry.GasType gasType, GlobalGasMix output)
	{
		MoleQuantity moleQuantity = Get(gasType);
		if (Mole.MatterState(gasType) != AtmosphereHelper.MatterState.Liquid || moleQuantity <= MoleQuantity.Zero)
		{
			return MoleEnergy.Zero;
		}
		TemperatureKelvin globalGasMixTemperature = GetGlobalGasMixTemperature(WorldSetting.Current.Data.GlobalAtmosphereData);
		MoleEnergy moleEnergy = IdealGas.Energy(new HeatCapacity(Mole.SpecificHeat(gasType), moleQuantity), globalGasMixTemperature);
		Mole mole = new Mole(gasType, moleQuantity, moleEnergy);
		VolumeLitres volumeLitres = VolumeForGas();
		PressurekPa pressure = new PressurekPa(TotalQuantityGas(), globalGasMixTemperature, volumeLitres);
		Mole mole2 = mole.ChangeState(pressure, volumeLitres, GlobalTemperatureStateChangeOffset);
		if (!mole2.IsValid)
		{
			return MoleEnergy.Zero;
		}
		MoleQuantity quantity = mole2.Quantity;
		MoleEnergy result = mole2.Energy + mole.Energy - moleEnergy;
		Set(moleQuantity - quantity, gasType);
		Chemistry.GasType gasType2 = MoleHelper.EvaporationType(gasType);
		output.Set(output.Get(gasType2) + quantity, gasType2);
		if (GetGlobalGasMixTemperature(WorldSetting.Current.Data.GlobalAtmosphereData) < mole.FreezingTemperature() + GlobalTemperatureStateChangeOffset)
		{
			Set(moleQuantity, gasType);
			output.Set(output.Get(gasType2) - quantity, gasType2);
			return MoleEnergy.Zero;
		}
		return result;
	}

	public MoleEnergy FreezeMoleQuantity(Chemistry.GasType gasType, GlobalGasMix output, MoleQuantity max)
	{
		if (!Mole.CanFreeze(gasType))
		{
			return MoleEnergy.Zero;
		}
		MoleQuantity moleQuantity = Get(gasType);
		if (moleQuantity <= MoleQuantity.Zero)
		{
			return MoleEnergy.Zero;
		}
		TemperatureKelvin globalGasMixTemperature = GetGlobalGasMixTemperature(WorldSetting.Current.Data.GlobalAtmosphereData);
		MoleEnergy moleEnergy = IdealGas.Energy(new HeatCapacity(Mole.SpecificHeat(gasType), moleQuantity), globalGasMixTemperature);
		Mole mole = new Mole(gasType, moleQuantity, moleEnergy);
		Mole mole2 = mole.FreezeGlobalMoles(GlobalTemperatureStateChangeOffset, max);
		if (!mole2.IsValid)
		{
			return MoleEnergy.Zero;
		}
		MoleQuantity quantity = mole2.Quantity;
		MoleEnergy result = mole2.Energy + mole.Energy - moleEnergy;
		Set(moleQuantity - quantity, gasType);
		Chemistry.GasType gasType2 = MoleHelper.FreezeType(gasType);
		output.Set(output.Get(gasType2) + quantity, gasType2);
		return result;
	}

	public MoleEnergy MeltMoleQuantity(Chemistry.GasType gasType, GlobalGasMix output, MoleQuantity max)
	{
		MoleQuantity moleQuantity = Get(gasType);
		if (moleQuantity <= MoleQuantity.Zero)
		{
			return MoleEnergy.Zero;
		}
		TemperatureKelvin globalGasMixTemperature = GetGlobalGasMixTemperature(WorldSetting.Current.Data.GlobalAtmosphereData);
		MoleEnergy moleEnergy = IdealGas.Energy(new HeatCapacity(Mole.SpecificHeat(gasType), moleQuantity), globalGasMixTemperature);
		Mole mole = new Mole(gasType, moleQuantity, moleEnergy);
		Mole mole2 = mole.MeltGlobalLiquidMoles(GlobalTemperatureStateChangeOffset, max);
		if (!mole2.IsValid)
		{
			return MoleEnergy.Zero;
		}
		MoleQuantity quantity = mole2.Quantity;
		MoleEnergy result = mole2.Energy + mole.Energy - moleEnergy;
		Set(moleQuantity - quantity, gasType);
		output.Set(output.Get(gasType) + quantity, gasType);
		return result;
	}

	public void DrawDebugInfo()
	{
		ImGui.Begin("BatchInfo", (ImGuiWindowFlags)12687);
		Vector2 windowSize = new Vector2(500f, 800f);
		ImGui.SetWindowPos(new Vector2((float)Screen.width - windowSize.x, 0f), ImGuiCond.Always);
		ImGui.Columns(1);
		ImGui.NewLine();
		ImGui.Text(DisplayName);
		ImGui.Text("Added ");
		ImGui.SameLine();
		ImGui.Text(StringManager.Get(_added.ToFloat()));
		ImGui.Text("Removed ");
		ImGui.SameLine();
		ImGui.Text(StringManager.Get(_removed.ToFloat()));
		Chemistry.GasType[] values = EnumCollections.GasTypes.Values;
		foreach (Chemistry.GasType gasType in values)
		{
			if (gasType != Chemistry.GasType.Undefined && !(Get(gasType) <= MoleQuantity.Zero))
			{
				ImGui.Text(EnumCollections.GasTypes.GetName(gasType));
				ImGui.SameLine();
				ImGui.Text(" ");
				ImGui.SameLine();
				ImGui.Text(StringManager.Get(Get(gasType).ToDouble()));
			}
		}
		ImGui.SetWindowSize(windowSize);
		ImGui.End();
	}

	public void Draw()
	{
		ImGui.Text(DisplayName);
		Chemistry.GasType[] values = EnumCollections.GasTypes.Values;
		foreach (Chemistry.GasType gasType in values)
		{
			if (gasType != Chemistry.GasType.Undefined && gasType != Chemistry.GasType.Fuel && gasType != Chemistry.GasType.Air && Get(gasType) > MoleQuantity.Zero)
			{
				ImGui.Text(EnumCollections.GasTypes.GetName(gasType));
				ImGui.SameLine();
				ImGui.Text(": ");
				ImGui.SameLine();
				ImGui.Text(Get(gasType).ToFloat().ToStringPrefix("mol"));
				ImGui.Separator();
			}
		}
	}

	public void MixGasses(GlobalGasMix other)
	{
		Add(other, AtmosphereHelper.MatterState.Gas);
		other.ClearQuantities(AtmosphereHelper.MatterState.Gas);
		other.Add(this, AtmosphereHelper.MatterState.Gas);
		VolumeLitres volumeLitres = VolumeForGas() + other.VolumeForGas();
		Scale((VolumeForGas() / volumeLitres).ToDouble());
		other.Scale((other.VolumeForGas() / volumeLitres).ToDouble());
	}

	public void MoveLiquids(GlobalGasMix target)
	{
		target.Add(this, AtmosphereHelper.MatterState.Liquid);
		ClearQuantities(AtmosphereHelper.MatterState.Liquid);
	}
}
