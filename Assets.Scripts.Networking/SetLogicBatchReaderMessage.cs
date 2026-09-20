using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Networking;

public class SetLogicBatchReaderMessage : ProcessedMessage<SetLogicBatchReaderMessage>
{
	public long LogicBatchReaderId;

	public int PrefabHash;

	public LogicType LogicType;

	public LogicBatchMethod BatchMethod;

	public override void Process(long hostId)
	{
		if (!GameManager.RunSimulation)
		{
			LogicBatchReader logicBatchReader = Thing.Find<LogicBatchReader>(LogicBatchReaderId);
			if (logicBatchReader == null)
			{
				DeferredMessageQueue.DeferUntilExists(this, hostId, LogicBatchReaderId, 10f, "SetLogicBatchReaderMessage");
				return;
			}
			logicBatchReader.CurrentPrefabHash = PrefabHash;
			logicBatchReader.LogicType = LogicType;
			logicBatchReader.BatchMethod = BatchMethod;
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		LogicBatchReaderId = reader.ReadInt64();
		PrefabHash = reader.ReadInt32();
		LogicType = (LogicType)reader.ReadUInt16();
		BatchMethod = (LogicBatchMethod)reader.ReadByte();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(LogicBatchReaderId);
		writer.WriteInt32(PrefabHash);
		writer.WriteUInt16((ushort)LogicType);
		writer.WriteByte((byte)BatchMethod);
	}
}
