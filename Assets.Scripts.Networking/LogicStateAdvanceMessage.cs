using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Networking;

public class LogicStateAdvanceMessage : ProcessedMessage<LogicStateAdvanceMessage>
{
	public long MotherboardId;

	public int CurrentStateIndex;

	public override void Process(long hostId)
	{
		Thing.Find<LogicMotherboard>(MotherboardId).ProcessRefresh(CurrentStateIndex);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		MotherboardId = reader.ReadInt64();
		CurrentStateIndex = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(MotherboardId);
		writer.WriteInt32(CurrentStateIndex);
	}
}
