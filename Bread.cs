using Assets.Scripts.Objects.Items;

public class Bread : Food
{
	public override FoodQuality GetFoodQuality()
	{
		return FoodQuality.Canned;
	}
}
