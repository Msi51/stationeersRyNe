using System.Xml.Serialization;

namespace Objects.Rockets;

public enum RocketMode : byte
{
	[XmlEnum("Invalid")]
	Invalid,
	[XmlEnum("None")]
	None,
	[XmlEnum("Mine")]
	Mine,
	[XmlEnum("Survey")]
	Survey,
	[XmlEnum("Discover")]
	Discover,
	[XmlEnum("Chart")]
	Chart,
	[XmlEnum("Deploy")]
	Deploy,
	[XmlEnum("SurfaceScan")]
	SurfaceScan,
	[XmlEnum("Transfer")]
	Transfer
}
