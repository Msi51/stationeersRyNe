using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Util;
using Objects.Items;

namespace Assets.Scripts.Objects.Items;

public class CocoaPowder : Consumable, IMicrowaveIngredient, IIngredient, IChemistryIngredient
{
	public override string GetQuantityText()
	{
		return StringGenerator.GetString((int)base.Quantity, Unit.g);
	}
}
