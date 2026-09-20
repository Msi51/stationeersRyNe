using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;

public class HealthInjector : TimedInjector
{
	public float HealAmount = 51f;

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
		human.Apply(new HealingEffect(HealAmount, Duration));
		return base.OnUseItem(quantity, onUseThing);
	}
}
