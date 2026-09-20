using Assets.Scripts.Atmospherics;

namespace Assets.Scripts.Objects.Pipes;

public class PressureFedGasEngineHeavy : PressureFedGasEngine
{
	public override float EngineEfficiency => 22f;

	protected override PressurekPa MAXPressurePerTick => new PressurekPa(8500.0);

	public override float MassContribution => 750f;
}
