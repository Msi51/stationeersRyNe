namespace Assets.Scripts.Networking;

public class PingObjectMessage : ProcessedMessage<PingObjectMessage>
{
	public override void Process(long hostId)
	{
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
	}
}
