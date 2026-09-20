using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Networking;

public class PinDeviceMessage : ProcessedMessage<PinDeviceMessage>
{
	public bool Pinned;

	public long MotherboardRefId;

	public long DeviceRefId;

	public override void Process(long hostId)
	{
		Referencable.Find<RocketMotherboard>(MotherboardRefId)?.PinDevice(Pinned, DeviceRefId);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Pinned = reader.ReadBoolean();
		Network.ReadPackedId(reader, out MotherboardRefId);
		Network.ReadPackedId(reader, out DeviceRefId);
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteBoolean(Pinned);
		Network.WritePackedId(writer, MotherboardRefId);
		Network.WritePackedId(writer, DeviceRefId);
	}
}
