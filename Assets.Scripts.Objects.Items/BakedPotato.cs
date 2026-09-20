using Assets.Scripts.Objects.Appliances;

namespace Assets.Scripts.Objects.Items;

public class BakedPotato : Food, IPackageableIngredient, IIngredient
{
	public override FoodQuality GetFoodQuality()
	{
		return FoodQuality.Cooked;
	}
}
