using System.Xml.Serialization;

namespace Assets.Scripts.Atmospherics;

public enum MixRule
{
	[XmlEnum("None")]
	None,
	[XmlEnum("Pure")]
	Pure
}
