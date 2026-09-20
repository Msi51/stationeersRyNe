using System.Xml.Serialization;

namespace ThingImport;

public enum TextureLoadType
{
	[XmlEnum("None")]
	None,
	[XmlEnum("Preload")]
	Preload,
	[XmlEnum("OnRequest")]
	OnRequest
}
