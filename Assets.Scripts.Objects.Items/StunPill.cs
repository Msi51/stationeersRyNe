using Assets.Scripts.Objects.Entities;

namespace Assets.Scripts.Objects.Items;

public class StunPill : Pill
{
	public float StunAmount = 200f;

	public override string GetQuantityText()
	{
		return string.Empty;
	}

	public override bool OnUseItem(float quantity, Thing onUseThing)
	{
		if (onUseThing == null)
		{
			return true;
		}
		Human human = onUseThing as Human;
		if (!human)
		{
			return true;
		}
		human.Apply(new StunEffect(Duration, StunAmount));
		return base.OnUseItem(quantity, onUseThing);
	}
}
