using Assets.Scripts.Objects.Appliances;

namespace Assets.Scripts.Objects.Items;

public class Bottle : Food, IIngredientLabelled, IMicrowaveIngredient, IIngredient, IPackageableIngredient, IChemistryIngredient
{
	public string GetStandardUnit()
	{
		return "ml";
	}

	public override FoodQuality GetFoodQuality()
	{
		return FoodQuality.Raw;
	}

	public override string GetQuantityText()
	{
		return $"{base.Quantity:F0}ml";
	}
}
