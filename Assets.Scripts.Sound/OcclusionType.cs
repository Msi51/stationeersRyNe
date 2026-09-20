using System.Xml.Serialization;

namespace Assets.Scripts.Sound;

public enum OcclusionType
{
	[XmlEnum("LOS")]
	Los,
	[XmlEnum("Room")]
	Room,
	[XmlEnum("None")]
	None
}
