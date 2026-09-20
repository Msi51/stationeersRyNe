using Assets.Scripts.Objects;

namespace Assets.Scripts.Networking;

public class RequestInteractionToServer : ProcessedMessage<RequestInteractionToServer>
{
	public long InteractThingId;

	public int InteractionId;

	public int NewState;

	public override void Process(long hostId)
	{
		OnServer.Interact(Thing.Find<Thing>(InteractThingId).Interactables[InteractionId], NewState);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		InteractThingId = reader.ReadInt64();
		InteractionId = reader.ReadInt32();
		NewState = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(InteractThingId);
		writer.WriteInt32(InteractionId);
		writer.WriteInt32(NewState);
	}
}
