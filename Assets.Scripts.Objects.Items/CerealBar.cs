using Assets.Scripts.Objects.Appliances;

namespace Assets.Scripts.Objects.Items;

public class CerealBar : Food, IIngredientLabelled, IMicrowaveIngredient, IIngredient
{
	public string GetStandardUnit()
	{
		return "g";
	}

	public override FoodQuality GetFoodQuality()
	{
		return FoodQuality.Canned;
	}
}
