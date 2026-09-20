using System;
using System.Xml.Serialization;

namespace Assets.Scripts.Networks;

[Flags]
public enum PipeBurst : byte
{
	[XmlEnum("None")]
	None = 0,
	[XmlEnum("false")]
	False = 0,
	[XmlEnum("Pressure")]
	Pressure = 1,
	[XmlEnum("true")]
	True = 1,
	[XmlEnum("Liquid")]
	Liquid = 2,
	[XmlEnum("Solid")]
	Solid = 4
}
