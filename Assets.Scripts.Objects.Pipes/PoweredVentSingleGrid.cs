using Assets.Scripts.Atmospherics;
using Objects.Electrical;

namespace Assets.Scripts.Objects.Pipes;

public class PoweredVentSingleGrid : PoweredVent
{
	protected override void ExchangeWithWorld()
	{
		Atmosphere atmosphere = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
		Atmosphere atmosphere2 = ConnectedPipeNetwork.Atmosphere;
		TemperatureKelvin totalTemperature = new TemperatureKelvin((atmosphere2.Temperature.ToDouble() * atmosphere2.TotalMoles.ToDouble() + atmosphere.Temperature.ToDouble() * atmosphere.TotalMoles.ToDouble()) / (atmosphere.TotalMoles.ToDouble() + atmosphere2.TotalMoles.ToDouble()));
		switch (base.VentDirection)
		{
		case VentDirection.Inward:
		{
			PressurekPa pressurekPa = base.PressurePerTick - PumpGasToPipe(atmosphere, atmosphere2, base.PressurePerTick);
			if (pressurekPa > PressurekPa.One)
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
			PressurekPa pressurekPa = base.PressurePerTick - PumpGasToWorld(atmosphere, atmosphere2, totalTemperature, base.PressurePerTick);
			if (pressurekPa > PressurekPa.One)
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
