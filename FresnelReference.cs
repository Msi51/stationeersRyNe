using System.Xml.Serialization;

public class FresnelReference : ColorPower
{
	[XmlAttribute]
	public float Emission;

	[XmlElement("RimLow")]
	public ColorRGB RimLow;

	[XmlElement("RimHigh")]
	public ColorRGB RimHigh;
}
