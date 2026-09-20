using Assets.Scripts.Objects.Appliances;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class CannedFood : Food, IIngredientLabelled, IMicrowaveIngredient, IIngredient
{
	public Item Debris;

	public override FoodQuality GetFoodQuality()
	{
		return FoodQuality.Canned;
	}

	public override void GenerateTradeValue(RecipeReference recipeReference)
	{
		TradeValue = NutritionValue * GameConstants.Trade.CreditPerNutrition;
	}

	public override void DestroyItemAtZero()
	{
		Slot parentSlot = base.ParentSlot;
		Vector3 centerPosition = CenterPosition;
		Item debris = Debris;
		base.DestroyItemAtZero();
		if (GameManager.RunSimulation)
		{
			if (parentSlot != null)
			{
				OnServer.CreateOld(debris, parentSlot);
			}
			else
			{
				OnServer.CreateOld(debris, centerPosition, Quaternion.identity, 0uL);
			}
		}
	}

	public string GetStandardUnit()
	{
		return "g";
	}
}
