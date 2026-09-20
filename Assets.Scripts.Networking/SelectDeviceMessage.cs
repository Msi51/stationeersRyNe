using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Networking;

public class SelectDeviceMessage : ProcessedMessage<SelectDeviceMessage>
{
	public long MotherboardRefId;

	public long DeviceRefId;

	public override void Process(long hostId)
	{
		Referencable.Find<RocketMotherboard>(MotherboardRefId)?.DeviceSelected(DeviceRefId);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out MotherboardRefId);
		Network.ReadPackedId(reader, out DeviceRefId);
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, MotherboardRefId);
		Network.WritePackedId(writer, DeviceRefId);
	}
}
