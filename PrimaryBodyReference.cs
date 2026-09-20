using System.Xml.Serialization;

public class PrimaryBodyReference : CelestialReference
{
	[XmlAttribute("SolarConstant")]
	public float SolarConstant = 1367f;
}
