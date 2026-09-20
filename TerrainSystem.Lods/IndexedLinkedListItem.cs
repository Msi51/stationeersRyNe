namespace TerrainSystem.Lods;

public abstract class IndexedLinkedListItem<TKey, TValue>
{
	public TKey Key;

	public TValue Next;

	public TValue Previous;
}
