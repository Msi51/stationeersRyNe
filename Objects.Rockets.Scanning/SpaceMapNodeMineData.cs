using System.Xml.Serialization;
using Assets.Scripts.Networking;

namespace Objects.Rockets.Scanning;

public class SpaceMapNodeMineData : SpaceMapNodeActionData
{
	[XmlAttribute("DrillLevel")]
	public int DrillLevel;

	public override RocketAction ToInstance(SpaceMapNode node)
	{
		return new RocketMine(this, node);
	}

	public SpaceMapNodeMineData(RocketBinaryReader reader)
		: base(reader)
	{
		DrillLevel = reader.ReadByte();
	}

	public SpaceMapNodeMineData()
	{
	}

	public override void Write(RocketBinaryWriter writer)
	{
		base.Write(writer);
		writer.WriteByte((byte)DrillLevel);
	}
}
