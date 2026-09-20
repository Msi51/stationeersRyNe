using Reagents;

namespace Assets.Scripts.Objects.Items;

public interface INutrition
{
	float MoodBonus { get; }

	float WaterMoles { get; }

	float GetNutritionalValue();

	float Nutrition(float useAmount);

	Thing.DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f);

	bool OnUseItem(float quantity, Thing useOnThing);

	float EatAmount(Entity eater);

	float EatTime(float quantityToEat);

	FoodQuality GetFoodQuality();

	bool Equals(Recipe recipe);
}
