using Assets.Scripts.Atmospherics;

namespace Assets.Scripts.Objects.Pipes;

public class PressureFedLiquidEngineHeavy : PressureFedLiquidEngine
{
	protected override float HeatExchangeAreaMax => 5f;

	protected override float FlowRateMin => 0.04f;

	protected override float FlowRateMax => 1.5f;

	public override float EngineEfficiency => 36f;

	public override float MassContribution => 750f;

	protected override PressurekPa MAXPressurePerTick => new PressurekPa(6000.0);
}
