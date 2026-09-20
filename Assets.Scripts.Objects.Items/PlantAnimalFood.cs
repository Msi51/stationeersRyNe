namespace Assets.Scripts.Objects.Items;

public class PlantAnimalFood : Plant, IAnimalFood
{
	public float GetNutritionValue()
	{
		return NutritionValue;
	}
}
