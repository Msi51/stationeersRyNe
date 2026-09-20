using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Networking;

public class PinLogicValueMessage : ProcessedMessage<PinLogicValueMessage>
{
	public bool Pinned;

	public long MotherboardRefId;

	public long DeviceRefId;

	public LogicType LogicType;

	public override void Process(long hostId)
	{
		Referencable.Find<RocketMotherboard>(MotherboardRefId)?.PinLogicValue(Pinned, DeviceRefId, LogicType);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Pinned = reader.ReadBoolean();
		Network.ReadPackedId(reader, out MotherboardRefId);
		Network.ReadPackedId(reader, out DeviceRefId);
		LogicType = (LogicType)reader.ReadUInt16();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteBoolean(Pinned);
		Network.WritePackedId(writer, MotherboardRefId);
		Network.WritePackedId(writer, DeviceRefId);
		writer.WriteUInt16((ushort)LogicType);
	}
}
