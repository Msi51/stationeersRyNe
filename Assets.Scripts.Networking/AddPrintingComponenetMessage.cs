using Assets.Scripts.Objects;

namespace Assets.Scripts.Networking;

public class AddPrintingComponenetMessage : ProcessedMessage<AddPrintingComponenetMessage>
{
	public long DynamicThingId;

	public override void Process(long hostId)
	{
		PrintingComponent.AttachPrintingComponent(Thing.Find<DynamicThing>(DynamicThingId));
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		DynamicThingId = reader.ReadInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(DynamicThingId);
	}
}
