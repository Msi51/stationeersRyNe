namespace Assets.Scripts.Objects.Clothing.Suits;

public class SpaceSuit : SuitBase
{
	public override float RadiationFactor => 0.025f;

	public override float MovementSpeedMultiplier => 1.25f;

	public override float SuitVelocityAbsorbed => 5f;

	public override float BruteDamagePassthroughAsStun => 6f;
}
