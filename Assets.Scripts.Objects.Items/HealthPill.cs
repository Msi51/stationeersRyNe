using Assets.Scripts.Objects.Entities;

namespace Assets.Scripts.Objects.Items;

public class HealthPill : Pill
{
	public float HealAmount = 51f;

	public override string GetQuantityText()
	{
		return "x" + base.Quantity;
	}

	public override bool OnUseItem(float quantity, Thing onUseThing)
	{
		if (!onUseThing)
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
