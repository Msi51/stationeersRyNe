using System.Xml.Serialization;

[XmlRoot("Material")]
public class MaterialReference
{
	[XmlAttribute]
	public float Smoothness;

	[XmlAttribute]
	public float Metallic;
}
