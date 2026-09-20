using System.Xml.Serialization;

namespace ThingImport;

public class TextureReferenceWithValue : TextureReference
{
	[XmlAttribute("Value")]
	public float Value = 1f;
}
