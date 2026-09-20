namespace Assets.Scripts.Networking;

public class ThingRenameMessage : ProcessedMessage<ThingRenameMessage>
{
	public long ThingId;

	public string ThingName;

	public override void Process(long hostId)
	{
		OnServer.SetCustomName(ThingId, ThingName);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ThingId = reader.ReadInt64();
		ThingName = reader.ReadString();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ThingId);
		writer.WriteString(ThingName);
	}
}
