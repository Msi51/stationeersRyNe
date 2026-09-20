using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Networking;

public class SetLogicBatchSlotReaderMessage : ProcessedMessage<SetLogicBatchSlotReaderMessage>
{
	public long LogicBatchReaderId;

	public int PrefabHash;

	public int LogicSlotTypeInt;

	public int SlotId;

	public int BatchMethodInt;

	public override void Process(long hostId)
	{
		if (!GameManager.RunSimulation)
		{
			LogicBatchSlotReader logicBatchSlotReader = Thing.Find<LogicBatchSlotReader>(LogicBatchReaderId);
			if (logicBatchSlotReader == null)
			{
				DeferredMessageQueue.DeferUntilExists(this, hostId, LogicBatchReaderId, 10f, "SetLogicBatchSlotReaderMessage");
				return;
			}
			logicBatchSlotReader.CurrentPrefabHash = PrefabHash;
			logicBatchSlotReader.LogicSlotType = (LogicSlotType)LogicSlotTypeInt;
			logicBatchSlotReader.SlotIndex = SlotId;
			logicBatchSlotReader.BatchMethod = (LogicBatchMethod)BatchMethodInt;
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		LogicBatchReaderId = reader.ReadInt64();
		PrefabHash = reader.ReadInt32();
		LogicSlotTypeInt = reader.ReadInt32();
		SlotId = reader.ReadInt32();
		BatchMethodInt = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(LogicBatchReaderId);
		writer.WriteInt32(PrefabHash);
		writer.WriteInt32(LogicSlotTypeInt);
		writer.WriteInt32(SlotId);
		writer.WriteInt32(BatchMethodInt);
	}
}
