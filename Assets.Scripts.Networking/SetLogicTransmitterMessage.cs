using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;

namespace Assets.Scripts.Networking;

public class SetLogicTransmitterMessage : ProcessedMessage<SetLogicTransmitterMessage>
{
	public long LogicTransmitterId;

	public long DeviceId;

	public override void Process(long hostId)
	{
		if (GameManager.RunSimulation)
		{
			return;
		}
		LogicTransmitter logicTransmitter = Thing.Find<LogicTransmitter>(LogicTransmitterId);
		Thing thing = Thing.Find<Thing>(DeviceId);
		if (logicTransmitter == null || (thing == null && NetworkThing.IsValid(DeviceId)))
		{
			DeferredMessageQueue.DeferUntilExists(this, hostId, LogicTransmitterId, DeviceId, 10f, "SetLogicTransmitterMessage");
			return;
		}
		if (!NetworkThing.IsValid(DeviceId))
		{
			thing = null;
		}
		logicTransmitter.CurrentDevice = thing as ITransmitable;
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		LogicTransmitterId = reader.ReadInt64();
		DeviceId = reader.ReadInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(LogicTransmitterId);
		writer.WriteInt64(DeviceId);
	}
}
