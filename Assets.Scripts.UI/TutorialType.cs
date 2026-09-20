using System.Xml.Serialization;

namespace Assets.Scripts.UI;

public enum TutorialType
{
	[XmlEnum("Undefined")]
	Undefined,
	[XmlEnum("Basic")]
	Basic,
	[XmlEnum("Advanced")]
	Advanced,
	[XmlEnum("Deprecated")]
	Deprecated,
	[XmlEnum("Max")]
	Max
}
