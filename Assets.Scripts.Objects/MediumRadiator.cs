namespace Assets.Scripts.Objects;

public class MediumRadiator : MediumRadiatorBase
{
	public override float ConvectionFactor => 0.2f;

	public override float RadiationFactor => 4f;

	public override float SolarHeatingFactor => 2f;
}
