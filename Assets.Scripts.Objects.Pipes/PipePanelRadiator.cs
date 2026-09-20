namespace Assets.Scripts.Objects.Pipes;

public class PipePanelRadiator : PipeRadiator
{
	public override float ConvectionFactor => 0.2f;

	public override float RadiationFactor => 3f;
}
