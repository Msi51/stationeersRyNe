namespace Assets.Scripts.Objects;

public class MediumRadiatorConvection : MediumRadiatorBase
{
	public override float ConvectionFactor => 1.25f;

	public override float RadiationFactor => 0.4f;

	public override float SolarHeatingFactor => 0.2f;
}
