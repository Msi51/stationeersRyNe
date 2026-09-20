using System.Xml.Serialization;

namespace Objects.Rockets;

public enum NodeType
{
	[XmlEnum("None")]
	None,
	[XmlEnum("Entry")]
	Entry,
	[XmlEnum("Static")]
	Static,
	[XmlEnum("Generated")]
	Generated,
	[XmlEnum("LaunchPad")]
	LaunchPad,
	[XmlEnum("LowOrbitHub")]
	LowOrbitHub,
	[XmlEnum("LowOrbitLaunchPad")]
	LowOrbitLaunchPad
}
