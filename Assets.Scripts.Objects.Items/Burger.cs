namespace Assets.Scripts.Objects.Items;

public class Burger : Food
{
	public override float MoodBonus => 1f;

	public override float GetDecayScale()
	{
		if (!(base.ParentSlot?.Parent is FoodContainer foodContainer))
		{
			return base.GetDecayScale();
		}
		return foodContainer.DecayScale;
	}

	public override FoodQuality GetFoodQuality()
	{
		return FoodQuality.Complex;
	}
}
