namespace Assets.Scripts.Networking;

public class SpawnIncidentRequestToServer : ProcessedMessage<SpawnIncidentRequestToServer>
{
	public long playerNetId;

	public override void Deserialize(RocketBinaryReader reader)
	{
		playerNetId = reader.ReadInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(playerNetId);
	}
}
