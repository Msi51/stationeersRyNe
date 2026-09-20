using System.Xml.Serialization;

namespace Objects.RoboticArm;

public enum ArmState : byte
{
	[XmlEnum("Undefined")]
	Undefined,
	[XmlEnum("Up")]
	Up,
	[XmlEnum("Down")]
	Down,
	[XmlEnum("AnimatingDown")]
	AnimatingDown,
	[XmlEnum("AnimatingUp")]
	AnimatingUp
}
