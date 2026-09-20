using System.Xml.Serialization;

namespace Assets.Scripts;

public enum SpawnPositionRule
{
	[XmlEnum("None")]
	None,
	[XmlEnum("Random")]
	Random,
	[XmlEnum("Radial")]
	Radial,
	[XmlEnum("Explicit")]
	Explicit
}
