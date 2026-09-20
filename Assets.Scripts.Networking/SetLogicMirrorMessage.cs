using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Networking;

public class SetLogicMirrorMessage : ProcessedMessage<SetLogicMirrorMessage>
{
	public long LogicReaderId;

	public long DeviceId;

	public override void Process(long hostId)
	{
		if (!GameManager.RunSimulation)
		{
			LogicMirror logicMirror = Thing.Find<LogicMirror>(LogicReaderId);
			Device device = Thing.Find<Device>(DeviceId);
			if (logicMirror == null || device == null)
			{
				DeferredMessageQueue.DeferUntilExists(this, hostId, LogicReaderId, DeviceId, 10f, "SetLogicMirrorMessage");
			}
			else
			{
				logicMirror.CurrentDevice = device;
			}
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		LogicReaderId = reader.ReadInt64();
		DeviceId = reader.ReadInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(LogicReaderId);
		writer.WriteInt64(DeviceId);
	}
}
