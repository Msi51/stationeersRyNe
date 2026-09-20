using Assets.Scripts.Objects;

namespace Assets.Scripts.Networking;

public class InteractionMessage : ProcessedMessage<InteractionMessage>
{
	public long DestinationId;

	public long SourceId;

	public int SourceSlotId;

	public int InteractionId;

	public int State;

	public bool Force;

	public bool AltKey;

	public bool InteractWith;

	private Interaction _interaction;

	private Interactable _interactable;

	public override void Process(long hostId)
	{
		if (InteractWith)
		{
			Thing thing = Thing.Find<Thing>(DestinationId);
			Thing thing2 = Thing.Find<Thing>(SourceId);
			_interactable = thing.Interactables[InteractionId];
			_interaction = new Interaction(thing2, thing2.Slots[SourceSlotId], thing, AltKey);
			if (!thing.PreventInteraction(out var _, _interactable, _interaction))
			{
				OnServer.InteractWith(_interactable, _interaction);
			}
		}
		else
		{
			OnServer.Interact(Thing.Find<Thing>(SourceId).Interactables[InteractionId], State);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		DestinationId = reader.ReadInt64();
		SourceId = reader.ReadInt64();
		SourceSlotId = reader.ReadInt32();
		InteractionId = reader.ReadInt32();
		State = reader.ReadInt32();
		Force = reader.ReadBoolean();
		AltKey = reader.ReadBoolean();
		InteractWith = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(DestinationId);
		writer.WriteInt64(SourceId);
		writer.WriteInt32(SourceSlotId);
		writer.WriteInt32(InteractionId);
		writer.WriteInt32(State);
		writer.WriteBoolean(Force);
		writer.WriteBoolean(AltKey);
		writer.WriteBoolean(InteractWith);
	}
}
