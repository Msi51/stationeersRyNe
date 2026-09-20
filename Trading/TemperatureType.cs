using System.Xml.Serialization;

namespace Trading;

public enum TemperatureType
{
	[XmlEnum("Kelvin")]
	Kelvin,
	[XmlEnum("Celsius")]
	Celsius
}
