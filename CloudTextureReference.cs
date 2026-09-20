using System.Xml.Serialization;
using ThingImport;

public class CloudTextureReference : TextureReference
{
	[XmlAttribute]
	public float Speed;

	[XmlAttribute]
	public float Opacity = 1f;

	[XmlElement("Shadow")]
	public CloudShadowReference Shadow;

	[XmlElement("Distortion")]
	public FloatReference Distortion;
}
