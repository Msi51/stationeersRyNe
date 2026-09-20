using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;

namespace Assets.Scripts.Networking;

public class SetLogicProcessorMessage : ProcessedMessage<SetLogicProcessorMessage>
{
	public long LogicProcessorId;

	public long LogicUnitId;

	public byte Index;

	public override void Process(long hostId)
	{
		if (GameManager.RunSimulation)
		{
			return;
		}
		LogicUnitProcessor logicUnitProcessor = Thing.Find<LogicUnitProcessor>(LogicProcessorId);
		LogicUnitBase logicUnitBase = Thing.Find<LogicUnitBase>(LogicUnitId);
		if (logicUnitProcessor == null || logicUnitBase == null)
		{
			DeferredMessageQueue.DeferUntilExists(this, hostId, LogicProcessorId, LogicUnitId, 10f, "SetLogicProcessorMessage");
			return;
		}
		if (Index == 1)
		{
			logicUnitProcessor.Input1 = logicUnitBase;
		}
		if (Index == 2)
		{
			logicUnitProcessor.Input2 = logicUnitBase;
		}
		if (Index == 3)
		{
			logicUnitProcessor.Input3 = logicUnitBase;
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		LogicProcessorId = reader.ReadInt64();
		LogicUnitId = reader.ReadInt64();
		Index = reader.ReadByte();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(LogicProcessorId);
		writer.WriteInt64(LogicUnitId);
		writer.WriteByte(Index);
	}
}
