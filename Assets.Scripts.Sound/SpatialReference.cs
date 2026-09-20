using System.Xml.Serialization;

namespace Assets.Scripts.Sound;

public enum SpatialReference
{
	[XmlEnum("2D")]
	TwoD,
	[XmlEnum("3D")]
	ThreeD,
	[XmlEnum("Small")]
	Small,
	[XmlEnum("Large")]
	Large,
	[XmlEnum("Medium")]
	Medium,
	[XmlEnum("MediumSmall")]
	MediumSmall,
	[XmlEnum("MediumLarge")]
	MediumLarge
}
