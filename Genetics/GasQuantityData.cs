using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;

namespace Genetics;

public class GasQuantityData
{
	[XmlAttribute("Type")]
	public Chemistry.GasType Type;

	[XmlAttribute("Moles")]
	public float Quantity;
}
