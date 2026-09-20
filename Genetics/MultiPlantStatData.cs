using System.Xml.Serialization;

namespace Genetics;

public class MultiPlantStatData
{
	[XmlAttribute("MidPoint")]
	public float MidPoint;

	[XmlAttribute("CenterSize")]
	public float CenterSize;

	[XmlAttribute("LowSize")]
	public float LowSize;

	[XmlAttribute("HighSize")]
	public float HighSize;

	[XmlAttribute("Clamp")]
	public bool Clamp;
}
