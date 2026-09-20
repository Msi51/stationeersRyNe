using System.Xml.Serialization;

namespace Assets.Scripts.UI;

public enum SPDAEntryType
{
	[XmlEnum("Undefined")]
	Undefined,
	[XmlEnum("Guides")]
	Guides,
	[XmlEnum("Lore")]
	Lore,
	[XmlEnum("Maximum")]
	Maximum
}
