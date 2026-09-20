namespace Assets.Scripts.Objects.Clothing.Suits;

public class InsulatedSuit : SuitBase
{
	public override float RadiationFactor => 0.05f;

	public override float BruteDamagePassthroughAsStun => 0f;

	public override float MovementSpeedMultiplier => 0.8f;

	public override float SuitVelocityAbsorbed => 15f;

	public override float HygieneReductionMultiplier => 2f;
}
