using System.Xml.Serialization;

namespace Trading;

[XmlRoot("Chance")]
public class ChanceData : IChecksum
{
	[XmlAttribute("Value")]
	public float Value = 1f;

	public int GetChecksum()
	{
		return (int)(Value * 1000f);
	}
}
