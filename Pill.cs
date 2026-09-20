using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Reagents;

public class Pill : Stackable, INutrition
{
	public float Duration = 120f;

	public float MoodBonus => 0f;

	public float WaterMoles => 0f;

	public FoodQuality GetFoodQuality()
	{
		return FoodQuality.None;
	}

	public bool Equals(Recipe recipe)
	{
		return CreatedReagentMixture.Equals(recipe);
	}

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f)
	{
		if (RootParent == this)
		{
			return base.OnUseSecondary(doAction);
		}
		Human human = RootParent as Human;
		if ((bool)human && !human.CanEat())
		{
			return new DelayedActionInstance
			{
				Duration = float.MaxValue,
				ActionMessage = ActionStrings.ConsumeFail
			};
		}
		float num = EatAmount(human);
		DelayedActionInstance result = new DelayedActionInstance
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
		if (actionCompletedRatio >= 1f)
		{
			OnUseItem(num * actionCompletedRatio, RootParent);
		}
		return result;
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
		return 1f;
	}

	public float GetNutritionalValue()
	{
		return 0f;
	}

	public float Nutrition(float quantity)
	{
		return 0f;
	}

	public float EatTime(float quantityToEat)
	{
		return 1f * quantityToEat;
	}

	public override bool UseDefaultUiUsingSounds()
	{
		return false;
	}
}
