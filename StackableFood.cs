using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Reagents;
using UnityEngine;

public class StackableFood : Stackable, INutrition
{
	[Tooltip("How much to use (consume) when using secondary function (eat)")]
	public float UseAmount;

	public float NutritionValue;

	public float EatSpeed;

	public float MoodBonus => 0f;

	public float WaterMoles => 0f;

	public bool Equals(Recipe recipe)
	{
		return CreatedReagentMixture.Equals(recipe);
	}

	public float GetNutritionalValue()
	{
		return NutritionValue * UseAmount;
	}

	public FoodQuality GetFoodQuality()
	{
		return FoodQuality.Cooked;
	}

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletionRatio = 1f)
	{
		if (RootParent == this)
		{
			return base.OnUseSecondary(doAction);
		}
		Human human = RootParent as Human;
		if (human == null || human.GetNutritionStorage() <= human.Nutrition)
		{
			return base.OnUseSecondary(doAction);
		}
		DelayedActionInstance result;
		if (!human.CanEat())
		{
			result = new DelayedActionInstance
			{
				Duration = float.MaxValue,
				ActionMessage = ActionStrings.ConsumeFail
			};
		}
		else
		{
			float num = EatAmount(human);
			result = new DelayedActionInstance
			{
				Duration = EatTime(num),
				ActionMessage = ActionStrings.Consume,
				ActionSoundHash = Item.EatingHash,
				ActionCompleteSoundHash = Item.EatingFinishedHash
			};
			if (!doAction)
			{
				return result;
			}
			if (actionCompletionRatio >= 1f)
			{
				OnUseItem(num * actionCompletionRatio, RootParent);
			}
		}
		return result;
	}

	public override void OnDamageDestroyed()
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Destroy(this);
		}
	}

	public override bool OnUseItem(float quantityToEat, Thing useOnThing)
	{
		quantityToEat = Mathf.Min(quantityToEat, base.Quantity);
		return base.OnUseItem(quantityToEat, useOnThing);
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.Edibles);
	}

	public override string GetStationpediaCategoryKey()
	{
		return StationpediaCategoryStrings.Edibles;
	}

	public float EatAmount(Entity eater)
	{
		return UseAmount;
	}

	public float Nutrition(float quantity)
	{
		return NutritionValue * quantity;
	}

	public float EatTime(float quantityToEat)
	{
		float num = quantityToEat * NutritionValue;
		return EatSpeed * num;
	}
}
