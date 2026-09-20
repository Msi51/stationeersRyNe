using Assets.Scripts.Objects;

namespace Assets.Scripts.Networking;

public class DestroyThingRequest : ProcessedMessage<DestroyThingRequest>
{
	public long ThingId;

	public long SourceItemId;

	public override void Process(long hostId)
	{
		Thing thing = Thing.Find<Thing>(ThingId);
		Thing thing2 = Thing.Find<Thing>(SourceItemId);
		if ((bool)thing && (bool)thing2)
		{
			thing.Delete(thing2);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ThingId = reader.ReadInt64();
		SourceItemId = reader.ReadInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ThingId);
		writer.WriteInt64(SourceItemId);
	}
}
