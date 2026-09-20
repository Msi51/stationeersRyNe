namespace Assets.Scripts.Networking;

public class HumanSpawnEffect : ProcessedMessage<HumanSpawnEffect>
{
	public long ThingId;

	public bool IsHideEffect;

	public override void Process(long hostId)
	{
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ThingId = reader.ReadInt64();
		IsHideEffect = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ThingId);
		writer.WriteBoolean(IsHideEffect);
	}
}
