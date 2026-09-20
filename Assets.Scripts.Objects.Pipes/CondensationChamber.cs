using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class CondensationChamber : StateChangeDevice
{
	protected override void AtmosphericsProcessing()
	{
		if (IsOpen)
		{
			if (OnOff && Powered && InputNetwork?.Atmosphere != null)
			{
				AtmosphereHelper.DrainLiquids(InputNetwork.Atmosphere, base.InternalAtmosphere, RocketMath.Min(InputNetwork.Atmosphere.TotalVolumeLiquids / 2.0, Chemistry.PipeVolume));
				AtmosphereHelper.MoveRegulatedGas(InputNetwork.Atmosphere, base.InternalAtmosphere, base.PressurePerTick, base.OutputSetting, RegulatorType.Upstream, AtmosphereHelper.MatterState.Gas);
			}
			if (base.HasOpenGrid && base.InternalAtmosphere.TotalMoles > MoleQuantity.Zero)
			{
				base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L).Add(base.InternalAtmosphere.GasMixture);
				base.InternalAtmosphere.GasMixture.Reset();
			}
		}
		else if (OnOff && Powered)
		{
			if (OutputNetwork?.Atmosphere != null)
			{
				AtmosphereHelper.DrainLiquids(base.InternalAtmosphere, OutputNetwork.Atmosphere, Chemistry.PipeVolume);
			}
			if (InputNetwork?.Atmosphere != null)
			{
				AtmosphereHelper.DrainLiquids(InputNetwork.Atmosphere, base.InternalAtmosphere, RocketMath.Min(InputNetwork.Atmosphere.TotalVolumeLiquids / 2.0, Chemistry.PipeVolume));
				AtmosphereHelper.MoveRegulatedGas(InputNetwork.Atmosphere, base.InternalAtmosphere, base.PressurePerTick, base.OutputSetting, RegulatorType.Upstream, AtmosphereHelper.MatterState.Gas);
			}
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = new PassiveTooltip(true);
		passiveTooltip.Title = DisplayName;
		PassiveTooltip result = passiveTooltip;
		if (InputConnection != null && (bool)InputConnection.Collider && hitCollider == InputConnection.Collider)
		{
			result = result.Populate(InputConnection);
			result.Title = GameStrings.CondensationChamberGasConnection.DisplayString;
			return result;
		}
		if (OutputConnection != null && (bool)OutputConnection.Collider && hitCollider == OutputConnection.Collider)
		{
			result = result.Populate(OutputConnection);
			result.Title = GameStrings.CondensationChamberLiquidConnection.DisplayString;
			return result;
		}
		if (InputConnection2 != null && (bool)InputConnection2.Collider && hitCollider == InputConnection2.Collider)
		{
			result = result.Populate(InputConnection2);
			result.Title = GameStrings.EvaporationChamberHeatConnection.DisplayString;
			return result;
		}
		if (hitCollider != temperatureGaugeCollider || base.InternalAtmosphere == null)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		Tooltip.ToolTipStringBuilder.Clear();
		if (hitCollider == temperatureGaugeCollider)
		{
			Tooltip.ToolTipStringBuilder.Append(AtmosphericsManager.DisplayBasicAtmosphere(base.InternalAtmosphere));
			Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.HeatExchangerEnergyTransfer.AsString(AtmosAnalyser.GetEnergyUnitString(base.EnergyTransfer.ToFloat()).AsColor("yellow")));
		}
		result.Extended = Tooltip.ToolTipStringBuilder.ToString();
		return result;
	}
}
