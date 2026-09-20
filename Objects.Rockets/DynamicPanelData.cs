using System.Xml.Serialization;
using Assets.Scripts.Networking;

namespace Objects.Rockets;

public class DynamicPanelData
{
	[XmlAttribute("Orientation")]
	public int Orientation;

	[XmlAttribute("Offset")]
	public int Offset;

	[XmlAttribute("Grow")]
	public bool Grow;

	[XmlAttribute("ExtraSize")]
	public int ExtraSize;

	public DynamicPanelData()
	{
	}

	public DynamicPanelData(RocketBinaryReader reader)
	{
		Orientation = reader.ReadInt32();
		Offset = reader.ReadInt32();
		ExtraSize = reader.ReadInt32();
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteInt32(Orientation);
		writer.WriteInt32(Offset);
		writer.WriteInt32(ExtraSize);
	}
}
