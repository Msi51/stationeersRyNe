using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Networking;

public class LogicStateMessage : ProcessedMessage<LogicStateMessage>
{
	public long MotherboardId;

	public string DisplayName;

	public int Index;

	public int NextState;

	public int FalseState;

	public bool IsDeleting;

	public override void Process(long hostId)
	{
		LogicMotherboard logicMotherboard = Thing.Find<LogicMotherboard>(MotherboardId);
		if (IsDeleting)
		{
			logicMotherboard.RemoveLogicState(Index);
		}
		else
		{
			logicMotherboard.AddOrUpdateState(Index, DisplayName, NextState, FalseState);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		MotherboardId = reader.ReadInt64();
		DisplayName = reader.ReadString();
		Index = reader.ReadInt32();
		NextState = reader.ReadInt32();
		FalseState = reader.ReadInt32();
		IsDeleting = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(MotherboardId);
		writer.WriteString(DisplayName);
		writer.WriteInt32(Index);
		writer.WriteInt32(NextState);
		writer.WriteInt32(FalseState);
		writer.WriteBoolean(IsDeleting);
	}
}
