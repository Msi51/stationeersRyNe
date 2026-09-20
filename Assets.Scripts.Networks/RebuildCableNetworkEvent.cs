using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;

namespace Assets.Scripts.Networks;

public readonly struct RebuildCableNetworkEvent(long originNetworkedStructureReference, long newNetworkReference, long oldNetworkReference) : ISyncListable
{
	public readonly long OriginNetworkedStructureReference = originNetworkedStructureReference;

	public readonly long NewNetworkReference = newNetworkReference;

	public readonly long OldNetworkReference = oldNetworkReference;

	public static SyncList<RebuildCableNetworkEvent> NewEvents = new SyncList<RebuildCableNetworkEvent>(DeserializeEvent);

	public void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, OriginNetworkedStructureReference);
		Network.WritePackedId(writer, NewNetworkReference);
		Network.WritePackedId(writer, OldNetworkReference);
	}

	private static void DeserializeEvent(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		Network.ReadPackedId(reader, out var referenceId2);
		Network.ReadPackedId(reader, out var referenceId3);
		Cable cable = Referencable.Find<Cable>(referenceId);
		CableNetwork newNetwork = Referencable.Find<CableNetwork>(referenceId2);
		CableNetwork oldNetwork = Referencable.Find<CableNetwork>(referenceId3);
		CableNetwork.RebuildCableNetworkClient(cable, newNetwork, oldNetwork);
	}
}
