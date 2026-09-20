using System.Xml.Serialization;

namespace Genetics;

public class GasQuantityRatioData : GasQuantityData
{
	[XmlAttribute("Ratio")]
	public float Ratio;
}
