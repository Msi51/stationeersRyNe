using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Networking;

public class SetLogicWriterMessage : ProcessedMessage<SetLogicWriterMessage>
{
	public long LogicWriterId;

	public long DeviceId;

	public LogicType LogicType;

	public bool IsWrittenDevice = true;

	public override void Process(long hostId)
	{
		if (!GameManager.RunSimulation)
		{
			LogicWriterBase logicWriterBase = Thing.Find<LogicWriterBase>(LogicWriterId);
			Device device = Thing.Find<Device>(DeviceId);
			if (logicWriterBase == null || device == null)
			{
				DeferredMessageQueue.DeferUntilExists(this, hostId, LogicWriterId, DeviceId, 10f, "SetLogicWriterMessage");
			}
			else if (IsWrittenDevice)
			{
				logicWriterBase.CurrentOutput = device;
				logicWriterBase.LogicType = LogicType;
			}
			else
			{
				logicWriterBase.Input1 = device as LogicUnitBase;
			}
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out LogicWriterId);
		Network.ReadPackedId(reader, out DeviceId);
		LogicType = (LogicType)reader.ReadUInt16();
		IsWrittenDevice = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, LogicWriterId);
		Network.WritePackedId(writer, DeviceId);
		writer.WriteUInt16((ushort)LogicType);
		writer.WriteBoolean(IsWrittenDevice);
	}
}
