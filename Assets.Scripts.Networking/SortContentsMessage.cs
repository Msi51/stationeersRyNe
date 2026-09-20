namespace Assets.Scripts.Networking;

public class SortContentsMessage : ProcessedMessage<SortContentsMessage>
{
	public long ThingId1;

	public override void Process(long hostId)
	{
		OnServer.SortContents(ThingId1);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ThingId1 = reader.ReadInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ThingId1);
	}
}
