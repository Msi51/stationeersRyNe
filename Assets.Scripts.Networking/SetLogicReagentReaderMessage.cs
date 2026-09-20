using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Reagents;

namespace Assets.Scripts.Networking;

public class SetLogicReagentReaderMessage : ProcessedMessage<SetLogicReagentReaderMessage>
{
	public long LogicReagentReaderId;

	public long DeviceId;

	public int LogicReagentModeInt;

	public int ReagentHashInt;

	public override void Process(long hostId)
	{
		if (GameManager.RunSimulation)
		{
			return;
		}
		ReagentReader reagentReader = Thing.Find<ReagentReader>(LogicReagentReaderId);
		Device device = Thing.Find<Device>(DeviceId);
		if (reagentReader == null || device == null)
		{
			DeferredMessageQueue.DeferUntilExists(this, hostId, LogicReagentReaderId, DeviceId, 10f, "SetLogicReagentReaderMessage");
			return;
		}
		reagentReader.CurrentDevice = device;
		reagentReader.LogicReagentMode = (LogicReagentMode)LogicReagentModeInt;
		reagentReader.LogicReagent = Reagent.AllReagents.Find((Reagent r) => r.GetHashCode() == ReagentHashInt);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		LogicReagentReaderId = reader.ReadInt64();
		DeviceId = reader.ReadInt64();
		LogicReagentModeInt = reader.ReadInt32();
		ReagentHashInt = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(LogicReagentReaderId);
		writer.WriteInt64(DeviceId);
		writer.WriteInt32(LogicReagentModeInt);
		writer.WriteInt32(ReagentHashInt);
	}
}
