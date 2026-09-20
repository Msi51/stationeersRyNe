using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Items;

public class Cheese : Food, IMicrowaveIngredient, IIngredient, IChemistryIngredient
{
	public override FoodQuality GetFoodQuality()
	{
		return FoodQuality.Raw;
	}

	public override string GetQuantityText()
	{
		return StringGenerator.GetString((int)base.Quantity, Unit.g);
	}
}
