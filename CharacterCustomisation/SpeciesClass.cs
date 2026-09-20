using System.Xml.Serialization;

namespace CharacterCustomisation;

public enum SpeciesClass : byte
{
	[XmlEnum("None")]
	None,
	[XmlEnum("Human")]
	Human,
	[XmlEnum("Zrilian")]
	Zrilian,
	[XmlEnum("Robot")]
	Robot
}
