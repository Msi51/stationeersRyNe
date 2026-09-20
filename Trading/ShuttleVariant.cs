using System.Xml.Serialization;

namespace Trading;

public enum ShuttleVariant
{
	None,
	[XmlEnum("Gas")]
	Gas
}
