namespace Assets.Scripts.Networking;

public class SyncTimeMessage : ProcessedMessage<SyncTimeMessage>
{
	public float ServerTime;

	public override void Process(long hostId)
	{
		NetworkTime.UpdateServerTimeOffset(ServerTime);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ServerTime = reader.ReadSingle();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteSingle(ServerTime);
	}
}
