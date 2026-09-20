using System.Xml.Serialization;

namespace Assets.Scripts.Serialization;

public enum DeleteSkeletonOnDecay
{
	[XmlEnum("false")]
	False,
	[XmlEnum("true")]
	True
}
