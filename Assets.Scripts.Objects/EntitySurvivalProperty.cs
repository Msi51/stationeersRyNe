using System.Xml.Serialization;

namespace Assets.Scripts.Objects;

public enum EntitySurvivalProperty
{
	[XmlEnum("None")]
	None,
	[XmlEnum("OxygenQuality")]
	OxygenQuality,
	[XmlEnum("Nutrition")]
	Nutrition,
	[XmlEnum("Hydration")]
	Hydration,
	[XmlEnum("Mood")]
	Mood,
	[XmlEnum("Hygiene")]
	Hygiene,
	[XmlEnum("FoodQuality")]
	FoodQuality,
	[XmlEnum("Health")]
	Health
}
