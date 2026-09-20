namespace Assets.Scripts.Networking;

public readonly struct DestroyEvent(IReferencable referencable) : ISyncListable
{
	private readonly long _referenceId = referencable.ReferenceId;

	public void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, _referenceId);
	}

	public static DestroyEvent Create(IReferencable spaceMapNode)
	{
		return new DestroyEvent(spaceMapNode);
	}
}
