using Assets.Scripts.Objects;

namespace Assets.Scripts.Networking;

public class ForceKinematicMessage : ProcessedMessage<ForceKinematicMessage>
{
	public long ChildId;

	public long ParentId;

	public DynamicThingPosition position;

	public bool kinematic;

	public bool fromServer;

	public override void Process(long hostId)
	{
		if (GameManager.RunSimulation && fromServer)
		{
			return;
		}
		DynamicThing dynamicThing = Thing.Find<DynamicThing>(ChildId);
		Thing thing = Thing.Find<Thing>(ParentId);
		if (dynamicThing == null || thing == null)
		{
			if (!GameManager.RunSimulation)
			{
				DeferredMessageQueue.DeferUntilExists(this, hostId, ChildId, ParentId, 3f, "ForceKinematicMessage");
			}
			return;
		}
		dynamicThing.ForceKinematicClient(kinematic, localCheck: false);
		if (GameManager.RunSimulation && dynamicThing.IsEntity && !fromServer)
		{
			fromServer = true;
			NetworkServer.SendToClients(this, NetworkChannel.GeneralTraffic, -1L);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ChildId = reader.ReadInt64();
		ParentId = reader.ReadInt64();
		position = default(DynamicThingPosition);
		position.Read(reader);
		kinematic = reader.ReadBoolean();
		fromServer = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ChildId);
		writer.WriteInt64(ParentId);
		position.Write(writer);
		writer.WriteBoolean(kinematic);
		writer.WriteBoolean(fromServer);
	}
}
