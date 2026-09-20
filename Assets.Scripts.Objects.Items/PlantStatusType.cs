using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

public enum PlantStatusType
{
	[XmlEnum("Dehydrated")]
	Dehydrated,
	[XmlEnum("Lit")]
	Lit,
	[XmlEnum("Darkness")]
	Darkness,
	[XmlEnum("LowTemperature")]
	LowTemperature,
	[XmlEnum("HighTemperature")]
	HighTemperature,
	[XmlEnum("Suffocated")]
	Suffocated,
	[XmlEnum("LowPressure")]
	LowPressure,
	[XmlEnum("HighPressure")]
	HighPressure,
	[XmlEnum("UnDesiredGas")]
	UnDesiredGas,
	[XmlEnum("LowWaterTemperature")]
	LowWaterTemperature,
	[XmlEnum("HighWaterTemperature")]
	HighWaterTemperature
}
