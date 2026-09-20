using Assets.Scripts.Objects.Appliances;

namespace Assets.Scripts.Objects.Items;

public class PlantAnimalFoodMicrowaveIngredient : Plant, IAnimalFood, IMicrowaveIngredient, IIngredient
{
	public float GetNutritionValue()
	{
		return NutritionValue;
	}
}
