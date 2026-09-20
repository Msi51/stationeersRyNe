using System.Xml.Serialization;

namespace TerrainSystem;

public class TreeDepthModifer
{
	public const string MULTIPLIER_VALUE = "Mul";

	[XmlAttribute("Value")]
	public int Value;

	[XmlAttribute("Mul")]
	public float Multiplier;

	public int GetChecksum()
	{
		return (((0 ^ Value) * 41) ^ (int)(Multiplier * 1000f)) * 41;
	}
}
