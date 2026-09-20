using System.Xml.Serialization;

public class ColorPower : ColorRGB
{
	[XmlAttribute]
	public float Power = 1f;
}
