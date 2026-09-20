namespace Assets.Scripts.Networking;

public class SpawnSpawnDataMessage : ProcessedMessage<SpawnSpawnDataMessage>
{
	public long ParentId { get; set; }

	public int DataIdHash { get; set; }

	public override void Process(long hostId)
	{
		base.Process(hostId);
		OnServer.SpawnSpawnData(ParentId, DataIdHash);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ParentId = reader.ReadInt64();
		DataIdHash = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ParentId);
		writer.WriteInt32(DataIdHash);
	}
}
