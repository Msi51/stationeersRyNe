namespace Assets.Scripts.Objects.Items;

public abstract class Cake : Food
{
	public override FoodQuality GetFoodQuality()
	{
		return FoodQuality.Complex;
	}
}
