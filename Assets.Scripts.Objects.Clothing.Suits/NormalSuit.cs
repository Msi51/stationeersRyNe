namespace Assets.Scripts.Objects.Clothing.Suits;

public class NormalSuit : SuitBase
{
	public override float RadiationFactor => 0.05f;

	public override Slot BackSlot => Slots[9];

	public override bool AllowedInBack(DynamicThing thing)
	{
		if (thing.PrefabHash == -412551656)
		{
			return false;
		}
		return true;
	}
}
