using Assets.Scripts;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;

public class EmptyPackaging : Stackable, IPackageableIngredient, IIngredient
{
	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.FoodCategory);
	}
}
