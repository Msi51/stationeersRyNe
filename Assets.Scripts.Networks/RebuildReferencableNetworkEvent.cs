using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Networks;

namespace Assets.Scripts.Networks;

public readonly struct RebuildReferencableNetworkEvent(long originMemberReference, long newNetworkReference, long oldNetworkReference) : ISyncListable
{
	public readonly long OriginMemberReference = originMemberReference;

	public readonly long NewNetworkReference = newNetworkReference;

	public readonly long OldNetworkReference = oldNetworkReference;

	public static SyncList<RebuildReferencableNetworkEvent> NewEvents = new SyncList<RebuildReferencableNetworkEvent>(DeserializeEvent);

	public void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, OriginMemberReference);
		Network.WritePackedId(writer, NewNetworkReference);
		Network.WritePackedId(writer, OldNetworkReference);
	}

	private static void DeserializeEvent(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		Network.ReadPackedId(reader, out var referenceId2);
		Network.ReadPackedId(reader, out var referenceId3);
		if (GameManager.GameState != GameState.None)
		{
			INetworkMember networkMember = Referencable.Find<INetworkMember>(referenceId);
			ReferencableNetwork referencableNetwork = Referencable.Find<ReferencableNetwork>(referenceId2);
			ReferencableNetwork oldNetwork = Referencable.Find<ReferencableNetwork>(referenceId3);
			if (referencableNetwork == null)
			{
				ConsoleWindow.PrintError("Network null during rebuild");
			}
			else if (networkMember == null)
			{
				ConsoleWindow.PrintError("Network member is null during rebuild");
			}
			else
			{
				referencableNetwork.RebuildNetworkClient(networkMember, oldNetwork);
			}
		}
	}
}
