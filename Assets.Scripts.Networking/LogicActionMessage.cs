using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Networking;

public class LogicActionMessage : ProcessedMessage<LogicActionMessage>
{
	public long MotherboardId;

	public long DeviceId;

	public int LogicStateIndex;

	public int Index;

	public LogicType LogicType;

	public double Value;

	public bool IsDeleting;

	public override void Process(long hostId)
	{
		LogicMotherboard logicMotherboard = Thing.Find<LogicMotherboard>(MotherboardId);
		if (IsDeleting)
		{
			logicMotherboard.RemoveAction(LogicStateIndex, Index);
			return;
		}
		Thing device = Thing.Find<Thing>(DeviceId);
		logicMotherboard.AddOrNewAction(LogicStateIndex, Index, device, LogicType, Value);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out MotherboardId);
		Network.ReadPackedId(reader, out DeviceId);
		LogicStateIndex = reader.ReadInt32();
		Index = reader.ReadInt32();
		Network.ReadLogicValue(reader, out LogicType, out Value);
		IsDeleting = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, MotherboardId);
		Network.WritePackedId(writer, DeviceId);
		writer.WriteInt32(LogicStateIndex);
		writer.WriteInt32(Index);
		Network.WriteLogicValue(writer, LogicType, Value);
		writer.WriteBoolean(IsDeleting);
	}
}
