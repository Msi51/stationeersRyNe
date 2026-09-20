using System.Xml.Serialization;

namespace Assets.Scripts.Objects;

public enum MachineTier
{
	[XmlEnum("Undefined")]
	Undefined,
	[XmlEnum("TierOne")]
	TierOne,
	[XmlEnum("TierTwo")]
	TierTwo,
	[XmlEnum("TierThree")]
	TierThree,
	Max
}
