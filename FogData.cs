using System.Xml.Serialization;
using Assets.Scripts;

public class FogData
{
	[XmlElement("Color", typeof(ColorFloat4Reference))]
	[XmlElement("Color32", typeof(Color32Reference))]
	public ColorReference FogColor;

	[XmlAttribute("StartDistance")]
	public float StartDistance = 5f;

	[XmlAttribute("EndDistance")]
	public float EndDistance = 20f;
}
