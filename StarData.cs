using System.Xml.Serialization;
using Assets.Scripts.Networking;

[XmlRoot("Star")]
public class StarData
{
	[XmlAttribute("Minimum")]
	public float Minimum;

	[XmlAttribute("Brightness")]
	public float Brightness = float.NaN;

	[XmlAttribute]
	public float FadeHeight;

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteFloatHalf(Minimum);
		writer.WriteFloatHalf(Brightness);
		writer.WriteFloatHalf(FadeHeight);
	}

	public void Read(RocketBinaryReader reader)
	{
		Minimum = reader.ReadFloatHalf();
		Brightness = reader.ReadFloatHalf();
		FadeHeight = reader.ReadFloatHalf();
	}
}
