namespace Assets.Scripts.Objects.Items;

public class PumpkinPie : Food
{
	public override float MoodBonus => 0.5f;

	public override FoodQuality GetFoodQuality()
	{
		return FoodQuality.Complex;
	}
}
