using System.Xml.Serialization;

namespace TerrainSystem;

public class SeekDepthData : SeekData
{
	[XmlAttribute("Value")]
	public int Value;
}
