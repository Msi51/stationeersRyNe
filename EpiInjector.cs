using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;

public class EpiInjector : TimedInjector
{
	public float StimAmount = 200f;

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
		human.Apply(new StimEffect(Duration, StimAmount));
		return base.OnUseItem(quantity, onUseThing);
	}
}
