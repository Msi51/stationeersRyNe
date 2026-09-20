using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Objects.Motherboards;

public struct PinLogicValueEvent(bool pinned, Motherboard motherboard, long deviceReferenceId, LogicType logicType) : ISyncListable
{
	public bool Pinned = pinned;

	public Motherboard Motherboard = motherboard;

	public long DeviceReferenceId = deviceReferenceId;

	public LogicType LogicType = logicType;

	public void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteBoolean(Pinned);
		Network.WritePackedId(writer, Motherboard);
		Network.WritePackedId(writer, DeviceReferenceId);
		writer.WriteUInt16((ushort)LogicType);
	}

	public static void Deserialize(RocketBinaryReader reader)
	{
		bool pinned = reader.ReadBoolean();
		Network.ReadPackedId(reader, out var referenceId);
		Network.ReadPackedId(reader, out var referenceId2);
		LogicType logicType = (LogicType)reader.ReadUInt16();
		Referencable.Find<RocketMotherboard>(referenceId).PinLogicValueClient(pinned, referenceId2, logicType);
	}
}
