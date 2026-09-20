using System.Xml.Serialization;

namespace Assets.Scripts;

public enum TraderType
{
	[XmlEnum("None")]
	None,
	[XmlEnum("Ore")]
	Ore,
	[XmlEnum("Alloy")]
	Alloy,
	[XmlEnum("Construction")]
	Construction,
	[XmlEnum("Gas")]
	Gas,
	[XmlEnum("Hydroponics")]
	Hydroponics
}
