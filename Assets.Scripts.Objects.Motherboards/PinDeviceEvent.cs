using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Objects.Motherboards;

public struct PinDeviceEvent(bool pinned, Motherboard motherboard, long deviceReferenceId) : ISyncListable
{
	public bool Pinned = pinned;

	public Motherboard Motherboard = motherboard;

	public long DeviceReferenceId = deviceReferenceId;

	public void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteBoolean(Pinned);
		Network.WritePackedId(writer, Motherboard);
		Network.WritePackedId(writer, DeviceReferenceId);
	}

	public static void Deserialize(RocketBinaryReader reader)
	{
		bool pinned = reader.ReadBoolean();
		Network.ReadPackedId(reader, out var referenceId);
		Network.ReadPackedId(reader, out var referenceId2);
		Referencable.Find<RocketMotherboard>(referenceId).PinDeviceClient(pinned, referenceId2);
	}
}
