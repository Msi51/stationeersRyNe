using System.Xml.Serialization;
using Assets.Scripts.Networking;
using UnityEngine;

namespace Objects.Rockets;

public class MapDisplayData
{
	[XmlAttribute("X")]
	public int X;

	[XmlAttribute("Y")]
	public int Y;

	[XmlElement("Icon")]
	public NodeIcon Icon;

	[XmlElement("Offset")]
	public Vector2Reference IconOffset;

	[XmlElement("DynamicPanel")]
	public DynamicPanelData DynamicPanel;

	[XmlIgnore]
	public Vector3 Position;

	[XmlIgnore]
	public Vector3 Offset;

	public void Initialise()
	{
		Position = new Vector3(X, Y, 0f);
		Icon?.Initialise();
		Offset = IconOffset?.ToVector2() ?? Vector3.zero;
	}

	public MapDisplayData()
	{
	}

	public MapDisplayData(NodeIcon icon)
	{
		Icon = icon;
	}

	public static MapDisplayData Create(RocketBinaryReader reader)
	{
		return new MapDisplayData(reader);
	}

	public MapDisplayData(RocketBinaryReader reader)
	{
		Position = reader.ReadVector3();
		int id = reader.ReadInt32();
		Icon = NodeIcon.Find(id);
		if (reader.ReadBoolean())
		{
			DynamicPanel = new DynamicPanelData(reader);
		}
		Offset = reader.ReadVector3();
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteVector3(Position);
		writer.WriteInt32(Icon?.IdHash ?? 0);
		writer.WriteBoolean(DynamicPanel != null);
		DynamicPanel?.Write(writer);
		writer.WriteVector3(Offset);
	}
}
