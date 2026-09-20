using System.Xml.Serialization;

[XmlRoot("Celestial")]
public class CelestialReference
{
	[XmlAttribute]
	public string Id = "Earth";

	[XmlElement]
	public LocalizedStringReference Name;

	[XmlAttribute]
	public bool Hidden;

	public virtual void LoadData()
	{
	}
}
