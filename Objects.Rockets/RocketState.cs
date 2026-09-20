using System.Xml.Serialization;

namespace Objects.Rockets;

public enum RocketState
{
	[XmlEnum("None")]
	None,
	[XmlEnum("OnLaunchMount")]
	OnLaunchMount,
	[XmlEnum("Launching")]
	Launching,
	[XmlEnum("InSpace")]
	InSpace,
	[XmlEnum("Landing")]
	Landing
}
