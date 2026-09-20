using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Networking;

public class SetLogicBatchWriterMessage : ProcessedMessage<SetLogicBatchWriterMessage>
{
	public long LogicWriterId;

	public long DeviceId;

	public int PrefabHash;

	public LogicType LogicType;

	public bool IsWrittenDevice = true;

	public override void Process(long hostId)
	{
		if (GameManager.RunSimulation)
		{
			return;
		}
		LogicBatchWriter logicBatchWriter = Thing.Find<LogicBatchWriter>(LogicWriterId);
		Device device = ((!IsWrittenDevice) ? Thing.Find<Device>(DeviceId) : null);
		if (logicBatchWriter == null || (!IsWrittenDevice && device == null))
		{
			DeferredMessageQueue.DeferUntilExists(this, hostId, LogicWriterId, IsWrittenDevice ? 0 : DeviceId, 10f, "SetLogicBatchWriterMessage");
			return;
		}
		Thing thing = (IsWrittenDevice ? Prefab.Find(PrefabHash) : device);
		if ((bool)thing)
		{
			if (IsWrittenDevice)
			{
				logicBatchWriter.CurrentPrefabHash = thing.PrefabHash;
				logicBatchWriter.LogicType = LogicType;
			}
			else
			{
				logicBatchWriter.Input1 = thing as LogicUnitBase;
			}
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out LogicWriterId);
		Network.ReadPackedId(reader, out DeviceId);
		PrefabHash = reader.ReadInt32();
		LogicType = (LogicType)reader.ReadUInt16();
		IsWrittenDevice = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, LogicWriterId);
		Network.WritePackedId(writer, DeviceId);
		writer.WriteInt32(PrefabHash);
		writer.WriteUInt16((ushort)LogicType);
		writer.WriteBoolean(IsWrittenDevice);
	}
}
