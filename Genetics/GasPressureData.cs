using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;

namespace Genetics;

public class GasPressureData
{
	[XmlAttribute("Type")]
	public Chemistry.GasType Type;

	[XmlAttribute("PartialPressure")]
	public float PartialPressure;
}
