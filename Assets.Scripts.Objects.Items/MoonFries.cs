namespace Assets.Scripts.Objects.Items;

public class MoonFries : Food
{
	public override FoodQuality GetFoodQuality()
	{
		return FoodQuality.Complex;
	}
}
