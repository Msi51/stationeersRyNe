using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Clothing.Suits;

public static class SuitModuleHelper
{
	public static float Heat(Atmosphere atmosphere, TemperatureKelvin outputTemperature, MoleEnergy maxUsableEnergy, float energyHeatingPowerCostPercent)
	{
		MoleEnergy moleEnergy = IdealGas.Energy(atmosphere.GasMixture.HeatCapacity, outputTemperature) - atmosphere.GasMixture.TotalEnergy;
		MoleEnergy moleEnergy2 = RocketMath.Abs(RocketMath.Clamp(moleEnergy, -maxUsableEnergy, maxUsableEnergy));
		if (!(moleEnergy >= MoleEnergy.Zero))
		{
			return 0f;
		}
		MoleEnergy moleEnergy3 = moleEnergy2 * energyHeatingPowerCostPercent;
		atmosphere.GasMixture.AddEnergy(moleEnergy2);
		return moleEnergy3.ToFloat();
	}

	public static float Cool(Atmosphere atmosphere, Atmosphere wasteAtmosphere, TemperatureKelvin outputTemperature, MoleEnergy maxUsableEnergy, float energyCoolingPowerCostPercent)
	{
		MoleEnergy moleEnergy = IdealGas.Energy(atmosphere.GasMixture.HeatCapacity, outputTemperature) - atmosphere.GasMixture.TotalEnergy;
		MoleEnergy moleEnergy2 = RocketMath.Abs(RocketMath.Clamp(moleEnergy, -maxUsableEnergy, maxUsableEnergy));
		if (!(moleEnergy < MoleEnergy.Zero))
		{
			return 0f;
		}
		MoleEnergy moleEnergy3 = moleEnergy2 * energyCoolingPowerCostPercent;
		moleEnergy2 = atmosphere.GasMixture.RemoveEnergy(moleEnergy2);
		wasteAtmosphere.GasMixture.AddEnergy(moleEnergy2);
		return moleEnergy3.ToFloat();
	}

	public static void HandleFilters(SuitBase suit)
	{
		if (suit.Exporting == 0 || suit.Battery == null || suit.Battery.IsEmpty || (suit.HasWasteTankSlot && suit.WasteTank == null) || suit.AirTank == null)
		{
			return;
		}
		List<Slot> filterSlots = suit.GetFilterSlots();
		for (int i = 0; i < filterSlots.Count; i++)
		{
			if (filterSlots[i].Occupant is GasFilter gasFilter)
			{
				if (suit.HasWasteTankSlot)
				{
					gasFilter.FilterGas(ref suit.InternalAtmosphere.GasMixture, ref suit.WasteTank.InternalAtmosphere.GasMixture);
				}
				else
				{
					gasFilter.FilterGas(ref suit.InternalAtmosphere.GasMixture, ref suit.WorldAtmosphere.GasMixture);
				}
			}
		}
	}

	public static void RegulatePressure(SuitBase suit, MoleQuantity minimumMolesToMove, out float powerUsed)
	{
		powerUsed = 0f;
		if (suit.Exporting != 0 && !(suit.Battery == null) && !suit.Battery.IsEmpty && (!suit.HasWasteTankSlot || !(suit.WasteTank == null)) && !(suit.AirTank == null))
		{
			PressurekPa pressure = RocketMath.Min(suit.PressurePerTick * suit.Efficiency, suit.InternalAtmosphere.PressureGassesAndLiquids - new PressurekPa(suit.OutputSetting));
			VolumeLitres volume = (suit.HasWasteTankSlot ? suit.WasteTank.InternalAtmosphere.Volume : suit.WorldAtmosphere.Volume);
			MoleQuantity val = IdealGas.Quantity(pressure, volume, suit.InternalAtmosphere.Temperature);
			if (suit.InternalAtmosphere.GasMixture.GetTotalMolesGassesAndLiquids < Chemistry.MINIMUM_VALID_TOTAL_MOLES)
			{
				val = suit.InternalAtmosphere.GasMixture.GetTotalMolesGassesAndLiquids;
			}
			GasMixture gasMixture = suit.InternalAtmosphere.Remove(RocketMath.Min(RocketMath.Max(minimumMolesToMove, val), suit.InternalAtmosphere.TotalMoles), AtmosphereHelper.MatterState.All);
			if (suit.HasWasteTankSlot)
			{
				suit.WasteTank.InternalAtmosphere.Add(gasMixture);
			}
			else
			{
				suit.WorldAtmosphere.Add(gasMixture);
			}
			powerUsed = 10f;
		}
	}

	public static void TakeGasFromAirTank(SuitBase suit)
	{
		if (suit.Importing != 0 && !(suit.AirTank == null) && !(suit.AirTank.InternalAtmosphere.PressureGassesAndLiquids < new PressurekPa(0.001)) && suit.InternalAtmosphere != null)
		{
			MoleQuantity transferMoles = IdealGas.Quantity(RocketMath.Min(suit.PressurePerTick * suit.Efficiency, new PressurekPa(suit.OutputSetting) - suit.InternalAtmosphere.PressureGassesAndLiquids), suit.InternalAtmosphere.Volume, suit.AirTank.InternalAtmosphere.Temperature);
			if (suit.AirTank.InternalAtmosphere.GasMixture.GetTotalMolesGassesAndLiquids < Chemistry.MINIMUM_VALID_TOTAL_MOLES)
			{
				transferMoles = suit.AirTank.InternalAtmosphere.GasMixture.GetTotalMolesGassesAndLiquids;
			}
			GasMixture gasMixture = suit.AirTank.InternalAtmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.Gas);
			suit.InternalAtmosphere.Add(gasMixture);
		}
	}

	public static void HandleSuitOverPressure(SuitBase suit)
	{
		if (suit.WasteTank?.InternalAtmosphere != null)
		{
			MoleQuantity val = IdealGas.Quantity(RocketMath.Min(suit.PressurePerTick * suit.Efficiency, suit.InternalAtmosphere.PressureGassesAndLiquids - new PressurekPa(suit.OutputSetting)), suit.WasteTank.InternalAtmosphere.Volume, suit.InternalAtmosphere.Temperature);
			GasMixture gasMixture = suit.InternalAtmosphere.Remove(RocketMath.Min(val, suit.InternalAtmosphere.TotalMoles), AtmosphereHelper.MatterState.All);
			suit.WasteTank.InternalAtmosphere.Add(gasMixture);
		}
	}
}
