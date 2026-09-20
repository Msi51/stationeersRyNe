using System.Xml.Serialization;
using Objects.Items;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(ThingModData))]
public class FoodModData : ConsumableModData
{
	public float NutritionValue = float.NaN;

	public float EatSpeed = float.NaN;

	public float WaterValue = float.NaN;
}
