using System.Xml.Serialization;

namespace Assets.Scripts;

public enum ShuttleType : byte
{
	None,
	[XmlEnum("Small")]
	Small,
	[XmlEnum("SmallGas")]
	SmallGas,
	[XmlEnum("Medium")]
	Medium,
	[XmlEnum("MediumGas")]
	MediumGas,
	[XmlEnum("Large")]
	Large,
	[XmlEnum("LargeGas")]
	LargeGas,
	[XmlEnum("MediumPlane")]
	MediumPlane,
	[XmlEnum("LargePlane")]
	LargePlane
}
