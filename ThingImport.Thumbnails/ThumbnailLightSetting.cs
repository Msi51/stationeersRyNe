using System.Xml.Serialization;

namespace ThingImport.Thumbnails;

public class ThumbnailLightSetting
{
	[XmlAttribute]
	public string Name;

	[XmlAttribute]
	public bool Enabled = true;

	[XmlAttribute]
	public float Intensity = 1f;

	[XmlAttribute]
	public float Pitch;

	[XmlAttribute]
	public float Yaw;

	[XmlAttribute]
	public float Roll;
}
