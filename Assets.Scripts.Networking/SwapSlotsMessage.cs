namespace Assets.Scripts.Networking;

public class SwapSlotsMessage : ProcessedMessage<SwapSlotsMessage>
{
	public long ThingId1;

	public long ThingId2;

	public int SlotId1;

	public int SlotId2;

	public override void Process(long hostId)
	{
		OnServer.SwapSlots(ThingId1, ThingId2, SlotId1, SlotId2);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ThingId1 = reader.ReadInt64();
		ThingId2 = reader.ReadInt64();
		SlotId1 = reader.ReadInt32();
		SlotId2 = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ThingId1);
		writer.WriteInt64(ThingId2);
		writer.WriteInt32(SlotId1);
		writer.WriteInt32(SlotId2);
	}
}
