namespace Assets.Scripts.Networking;

public class SyncGameMode : ProcessedMessage<SyncGameMode>
{
	public GameMode GameMode;

	public override void Process(long hostId)
	{
		WorldManager.Instance.GameMode = GameMode;
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		GameMode = (GameMode)reader.ReadByte();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteByte((byte)GameMode);
	}
}
