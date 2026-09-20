namespace Assets.Scripts.Networking;

public class CommsMotherBoardMessage : ProcessedMessage<CommsMotherBoardMessage>
{
	public long MotherboardId { get; set; }

	public bool ToBroadcast { get; set; }

	public override void Process(long hostId)
	{
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		MotherboardId = reader.ReadInt64();
		ToBroadcast = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(MotherboardId);
		writer.WriteBoolean(ToBroadcast);
	}
}
