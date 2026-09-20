namespace Assets.Scripts.Objects.Items;

public class ChocolateBar : Food
{
	public override float MoodBonus => 1f;

	public override FoodQuality GetFoodQuality()
	{
		return FoodQuality.Complex;
	}
}
