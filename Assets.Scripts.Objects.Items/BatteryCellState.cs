using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

public enum BatteryCellState
{
	[XmlEnum("Empty")]
	Empty = 0,
	[XmlEnum("Critical")]
	Critical = 1,
	[XmlEnum("Low")]
	Low = 3,
	[XmlEnum("VeryLow")]
	VeryLow = 2,
	[XmlEnum("Medium")]
	Medium = 4,
	[XmlEnum("High")]
	High = 5,
	[XmlEnum("Full")]
	Full = 6
}
