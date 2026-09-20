using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;

public class StunInjector : TimedInjector
{
	public float StunAmount = 200f;

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
