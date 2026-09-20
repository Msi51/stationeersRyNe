using System.Xml.Serialization;

public class NoiseLayerData
{
	[XmlAttribute("Emissive")]
	public float Emissive;

	[XmlAttribute("Threshold")]
	public float Threshold;

	[XmlAttribute("Power")]
	public float Power;

	[XmlAttribute("Multiplier")]
	public float Multiplier;

	[XmlAttribute("MaskMultiplier")]
	public float MaskMultiplier;
}
