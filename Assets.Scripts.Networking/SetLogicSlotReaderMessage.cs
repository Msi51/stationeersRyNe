using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Networking;

public class SetLogicSlotReaderMessage : ProcessedMessage<SetLogicSlotReaderMessage>
{
	public long LogicSlotReaderId;

	public long DeviceId;

	public int LogicSlotTypeInt;

	public int SlotInt;

	public override void Process(long hostId)
	{
		if (!GameManager.RunSimulation)
		{
			LogicSlotReader logicSlotReader = Thing.Find<LogicSlotReader>(LogicSlotReaderId);
			Device device = Thing.Find<Device>(DeviceId);
			if (logicSlotReader == null || device == null)
			{
				DeferredMessageQueue.DeferUntilExists(this, hostId, LogicSlotReaderId, DeviceId, 10f, "SetLogicSlotReaderMessage");
				return;
			}
			logicSlotReader.CurrentDevice = device;
			logicSlotReader.LogicSlotType = (LogicSlotType)LogicSlotTypeInt;
			logicSlotReader.SlotIndex = SlotInt;
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		LogicSlotReaderId = reader.ReadInt64();
		DeviceId = reader.ReadInt64();
		LogicSlotTypeInt = reader.ReadInt32();
		SlotInt = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(LogicSlotReaderId);
		writer.WriteInt64(DeviceId);
		writer.WriteInt32(LogicSlotTypeInt);
		writer.WriteInt32(SlotInt);
	}
}
