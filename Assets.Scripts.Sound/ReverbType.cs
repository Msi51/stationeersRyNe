using System.Xml.Serialization;

namespace Assets.Scripts.Sound;

public enum ReverbType
{
	[XmlEnum("Flat")]
	Flat,
	[XmlEnum("Spatial")]
	Spatial,
	[XmlEnum("None")]
	None,
	[XmlEnum("Quiet")]
	Quiet,
	[XmlEnum("FlatLoud")]
	FlatLoud
}
