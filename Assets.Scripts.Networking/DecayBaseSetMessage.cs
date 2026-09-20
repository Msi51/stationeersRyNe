namespace Assets.Scripts.Networking;

public class DecayBaseSetMessage : ProcessedMessage<DecayBaseSetMessage>
{
	public float DecayTime;

	public override void Process(long hostId)
	{
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		DecayTime = reader.ReadSingle();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteSingle(DecayTime);
	}
}
