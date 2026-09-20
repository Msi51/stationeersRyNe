using Objects.Rockets;

namespace Assets.Scripts.Networking;

public class AbandonRocketMessage : ProcessedMessage<AbandonRocketMessage>
{
	public long RocketId;

	public override void Process(long hostId)
	{
		Referencable.Find<Rocket>(RocketId)?.AbandonRocket();
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		RocketId = reader.ReadInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(RocketId);
	}
}
