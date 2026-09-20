using System;
using System.Text;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Objects.Electrical;
using Objects.Items;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Food : Consumable, INutrition, IAtmospherical, ISpatial, IPhysical, IProfile, IDensePoolable
{
	[Header("Food")]
	[Tooltip("Total nutrition available in 1 quantity")]
	public float NutritionValue;

	[Tooltip("Time taken to consume one unit of Nutrition")]
	public float EatSpeed = 1f;

	[Tooltip("Total water moles delivered to the stomach when 1 quantity is consumed. Provides no hydration")]
	public float WaterValue;

	public virtual float MoodBonus => 0f;

	public float WaterMoles => WaterValue;

	public static event Event OnFoodEatedEvent;

	public bool Equals(Recipe recipe)
	{
		return CreatedReagentMixture.Equals(recipe);
	}

	public static float GetFoodQualityRatio(INutrition iNutrition)
	{
		return iNutrition.GetFoodQuality() switch
		{
			FoodQuality.None => 0f, 
			FoodQuality.Raw => 0.25f, 
			FoodQuality.Cooked => 0.5f, 
			FoodQuality.Canned => 0.75f, 
			FoodQuality.Complex => 1f, 
			_ => 0f, 
		};
	}

	public static Assets.Scripts.Localization2.GameString GetFoodQualityStationpediaDescription(INutrition iNutrition)
	{
		return iNutrition.GetFoodQuality() switch
		{
			FoodQuality.None => GameStrings.StationpediaNoFoodQuality, 
			FoodQuality.Raw => GameStrings.StationpediaLowFoodQuality, 
			FoodQuality.Cooked => GameStrings.StationpediaOkFoodQuality, 
			FoodQuality.Canned => GameStrings.StationpediaGoodFoodQuality, 
			FoodQuality.Complex => GameStrings.StationpediaSuperiorFoodQuality, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	public static Assets.Scripts.Localization2.GameString GetFoodQualityDescription(FoodQuality foodQuality)
	{
		return foodQuality switch
		{
			FoodQuality.Raw => GameStrings.TooltipLowFoodQuality, 
			FoodQuality.Cooked => GameStrings.TooltipOkFoodQuality, 
			FoodQuality.Canned => GameStrings.TooltipGoodFoodQuality, 
			FoodQuality.Complex => GameStrings.TooltipSuperiorFoodQuality, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	public static Assets.Scripts.Localization2.GameString GetFoodQualityWord(FoodQuality foodQuality)
	{
		return foodQuality switch
		{
			FoodQuality.Raw => GameStrings.FoodQualityLow, 
			FoodQuality.Cooked => GameStrings.FoodQualityOk, 
			FoodQuality.Canned => GameStrings.FoodQualityGood, 
			FoodQuality.Complex => GameStrings.FoodQualityBest, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	public static string GetFoodQualityColor(FoodQuality foodQuality)
	{
		return foodQuality switch
		{
			FoodQuality.Raw => "red", 
			FoodQuality.Cooked => "orange", 
			FoodQuality.Canned => "yellow", 
			FoodQuality.Complex => "green", 
			_ => "white", 
		};
	}

	public static string GetColoredFoodQualityWord(FoodQuality foodQuality)
	{
		return GetFoodQualityWord(foodQuality).AsColor(GetFoodQualityColor(foodQuality));
	}

	public static void AppendFoodQualityText(StringBuilder stringBuilder, INutrition iNutrition)
	{
		AppendFoodQualityText(stringBuilder, iNutrition.GetFoodQuality());
	}

	public static void AppendFoodQualityText(StringBuilder stringBuilder, FoodQuality foodQuality)
	{
		if (foodQuality != FoodQuality.None)
		{
			stringBuilder.AppendLine(GameStrings.TooltipFoodQuality.AsString(GetColoredFoodQualityWord(foodQuality)));
		}
	}

	public float GetNutritionalValue()
	{
		return NutritionValue * base.Quantity;
	}

	public virtual FoodQuality GetFoodQuality()
	{
		return FoodQuality.Complex;
	}

	public override bool UseDefaultUiUsingSounds()
	{
		return false;
	}

	public override object GetModXmlType()
	{
		return new FoodModData();
	}

	public override void DeserializModData(ThingModData modData)
	{
		base.DeserializModData(modData);
		if (modData is FoodModData foodModData)
		{
			if (!float.IsNaN(foodModData.NutritionValue))
			{
				NutritionValue = foodModData.NutritionValue;
			}
			if (!float.IsNaN(foodModData.EatSpeed))
			{
				EatSpeed = foodModData.EatSpeed;
			}
			if (!float.IsNaN(foodModData.WaterValue))
			{
				WaterValue = foodModData.WaterValue;
			}
		}
	}

	public virtual void GenerateTradeValue(RecipeReference recipeReference)
	{
		TradeValue = NutritionValue * GameConstants.Trade.CreditPerNutrition;
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.Edibles);
	}

	public override string GetStationpediaCategoryKey()
	{
		return StationpediaCategoryStrings.Edibles;
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
			float num2 = (human.ExperiencingRespawnStress ? (1f / (float)DifficultySetting.Current.RespawnStressConsumptionSpeed) : 1f);
			Food.OnFoodEatedEvent?.Invoke();
			result = new DelayedActionInstance
			{
				Duration = EatTime(num * num2),
				ActionMessage = ActionStrings.Consume,
				ActionSoundHash = Item.EatingHash,
				ActionCompleteSoundHash = Item.EatingFinishedHash
			};
			if (!doAction)
			{
				return result;
			}
			OnUseItem(num * actionCompletionRatio, RootParent);
			human.OnFoodEaten(this);
		}
		return result;
	}

	public override bool OnUseItem(float quantityToEat, Thing useOnThing)
	{
		quantityToEat = Mathf.Min(quantityToEat, base.Quantity);
		return base.OnUseItem(quantityToEat, useOnThing);
	}

	public float EatAmount(Entity eater)
	{
		if (NutritionValue == 0f)
		{
			return base.Quantity;
		}
		return Mathf.Min((eater.GetNutritionStorage() - eater.Nutrition) / NutritionValue, base.Quantity);
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
