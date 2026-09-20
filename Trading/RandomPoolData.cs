using System.Xml.Serialization;

namespace Trading;

[XmlRoot("RandomPool")]
public class RandomPoolData : IChecksum
{
	[XmlAttribute("Index")]
	public int Index;

	[XmlAttribute("Weight")]
	public int Weight = 1000;

	public int GetChecksum()
	{
		return ((Index * 41) ^ Weight) * 41;
	}
}
