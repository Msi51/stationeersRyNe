using Objects.Electrical;

namespace Assets.Scripts.Networking;

public class LinkPylonsMessage : ProcessedMessage<LinkPylonsMessage>
{
	public byte FromIndex;

	public long FromRefId;

	public byte ToIndex;

	public long ToRefId;

	public override void Process(long hostId)
	{
		PylonNode pylonNode = PylonHelper.FindNode(FromRefId, FromIndex);
		PylonNode pylonNode2 = PylonHelper.FindNode(ToRefId, ToIndex);
		if (pylonNode != null && pylonNode2 != null)
		{
			PylonHelper.ServerConnectNodes(pylonNode, pylonNode2);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		FromIndex = reader.ReadByte();
		Network.ReadPackedId(reader, out FromRefId);
		ToIndex = reader.ReadByte();
		Network.ReadPackedId(reader, out ToRefId);
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteByte(FromIndex);
		Network.WritePackedId(writer, FromRefId);
		writer.WriteByte(ToIndex);
		Network.WritePackedId(writer, ToRefId);
	}
}
