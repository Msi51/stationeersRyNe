using System.Xml.Serialization;

namespace Genetics;

public class PlantStatData
{
	[XmlAttribute("Base")]
	public float Base;

	[XmlAttribute("Invert")]
	public bool Invert;
}
