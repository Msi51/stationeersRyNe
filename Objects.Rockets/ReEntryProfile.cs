using System.Xml.Serialization;

namespace Objects.Rockets;

public enum ReEntryProfile
{
	[XmlEnum("None")]
	None,
	[XmlEnum("Optimal")]
	Low,
	[XmlEnum("Medium")]
	Medium,
	[XmlEnum("High")]
	High,
	[XmlEnum("Max")]
	Max
}
