using System.Xml.Serialization;

namespace Assets.Scripts.Voxel;

public enum MinableType : byte
{
	[XmlEnum("None")]
	None = 0,
	[XmlEnum("Stone")]
	Stone = 1,
	[XmlEnum("Iron")]
	Iron = 2,
	[XmlEnum("Ice")]
	Ice = 3,
	[XmlEnum("Gold")]
	Gold = 4,
	[XmlEnum("Coal")]
	Coal = 5,
	[XmlEnum("Copper")]
	Copper = 6,
	[XmlEnum("Uranium")]
	Uranium = 7,
	[XmlEnum("Nickel")]
	Nickel = 8,
	[XmlEnum("Lead")]
	Lead = 9,
	[XmlEnum("Silver")]
	Silver = 10,
	[XmlEnum("Silicon")]
	Silicon = 11,
	[XmlEnum("Oxite")]
	Oxite = 12,
	[XmlEnum("Volatiles")]
	Volatiles = 13,
	[XmlEnum("GeyserHydrogen")]
	GeyserHydrogen = 14,
	[XmlEnum("Cobalt")]
	Cobalt = 15,
	[XmlEnum("Nitrice")]
	Nitrice = 16,
	[XmlEnum("LastMinable")]
	LastMinable = 17,
	[XmlEnum("Crater")]
	Crater = 254,
	[XmlEnum("Bedrock")]
	Bedrock = byte.MaxValue
}
