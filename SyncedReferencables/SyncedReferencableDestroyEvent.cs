using Assets.Scripts;
using Assets.Scripts.Networking;

namespace SyncedReferencables;

public readonly struct SyncedReferencableDestroyEvent(IReferencable referencable) : ISyncListable
{
	private readonly long _referenceId = referencable.ReferenceId;

	public void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, _referenceId);
	}

	public static void DeserializeDestroy(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		Referencable.Find<SyncedReferencable>(referenceId)?.Destroy();
	}
}
