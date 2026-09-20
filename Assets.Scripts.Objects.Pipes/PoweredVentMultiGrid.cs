using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using Objects.Electrical;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class PoweredVentMultiGrid : PoweredVent
{
	[SerializeField]
	private int gridMixingDepth = 6;

	private Atmosphere targetAtmosphere => ConnectedPipeNetwork.Atmosphere;

	protected override void ExchangeWithWorld()
	{
		Atmosphere atmosphere = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
		TemperatureKelvin totalTemperature = new TemperatureKelvin((targetAtmosphere.Temperature.ToDouble() * targetAtmosphere.TotalMoles.ToDouble() + atmosphere.Temperature.ToDouble() * atmosphere.TotalMoles.ToDouble()) / (atmosphere.TotalMoles.ToDouble() + targetAtmosphere.TotalMoles.ToDouble()));
		switch (base.VentDirection)
		{
		case VentDirection.Inward:
		{
			PressurekPa pressurekPa = base.PressurePerTick - MultiGridAtmospherics.PumpGasToPipe(atmosphere, targetAtmosphere, base.PressurePerTick, base.ExternalPressure);
			if (pressurekPa > MultiGridAtmospherics.DISCARD_REMAINING_THRESHOLD)
			{
				PressurekPa pressurekPa2 = RocketMath.Min(MultiGridAtmospherics.InwardsPressureRequired(atmosphere, pressurekPa, gridMixingDepth, base.ExternalPressure), pressurekPa);
				if (atmosphere.PressureGasses < pressurekPa2)
				{
					MultiGridAtmospherics.VentFromNeighborsToPipe(targetAtmosphere, atmosphere, ref pressurekPa, gridMixingDepth, base.ExternalPressure);
				}
				else
				{
					MultiGridAtmospherics.PumpGasToPipe(atmosphere, targetAtmosphere, pressurekPa2, base.ExternalPressure, force: true);
				}
			}
			if (pressurekPa > MultiGridAtmospherics.DISCARD_REMAINING_THRESHOLD + PressurekPa.One)
			{
				base.FlowIndicatorStatus = ((pressurekPa < base.PressurePerTick / 2.0) ? FlowIndicatorState.InwardsLimited : FlowIndicatorState.InwardsVeryLimited);
			}
			else
			{
				base.FlowIndicatorStatus = FlowIndicatorState.Max;
			}
			break;
		}
		case VentDirection.Outward:
		{
			PressurekPa pressurekPa = base.PressurePerTick - MultiGridAtmospherics.PumpGasToWorld(atmosphere, targetAtmosphere, totalTemperature, base.PressurePerTick, base.ExternalPressure);
			if (pressurekPa > MultiGridAtmospherics.DISCARD_REMAINING_THRESHOLD)
			{
				PressurekPa pressureToMove = MultiGridAtmospherics.OutwardsPressureRequired(atmosphere, pressurekPa, gridMixingDepth, base.ExternalPressure);
				pressurekPa -= MultiGridAtmospherics.PumpGasToWorld(atmosphere, targetAtmosphere, totalTemperature, pressureToMove, base.ExternalPressure, force: true);
			}
			if (pressurekPa > MultiGridAtmospherics.DISCARD_REMAINING_THRESHOLD + PressurekPa.One)
			{
				base.FlowIndicatorStatus = ((pressurekPa < base.PressurePerTick / 2.0) ? FlowIndicatorState.OutwardsLimited : FlowIndicatorState.OutwardsVeryLimited);
			}
			else
			{
				base.FlowIndicatorStatus = FlowIndicatorState.Max;
			}
			break;
		}
		}
	}
}
