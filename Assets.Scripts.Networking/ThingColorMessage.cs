using Assets.Scripts.Objects;

namespace Assets.Scripts.Networking;

public class ThingColorMessage : ProcessedMessage<ThingColorMessage>
{
	public long ThingId;

	public int ColorIndex;

	public override void Process(long hostId)
	{
		Thing.Find<Thing>(ThingId).SetCustomColor(ColorIndex);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ThingId = reader.ReadInt64();
		ColorIndex = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ThingId);
		writer.WriteInt32(ColorIndex);
	}
}
