using System.Xml.Serialization;

[XmlRoot("CelestialConstants")]
public class CelestialConstantsReference : CelestialReference
{
	public static CelestialConstantsReference Default = new CelestialConstantsReference
	{
		Id = "Default"
	};

	[XmlAttribute]
	public float BodyScale = 1f;

	[XmlAttribute]
	public float SkyboxScale = 1f;

	[XmlElement("TimeOffset")]
	public TimeSpanReference TimeOffset = new TimeSpanReference();
}
