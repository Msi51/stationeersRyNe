namespace TerrainSystem.Lods;

public interface IBasicPoolable<TKey>
{
	TKey Key { get; }

	bool IsActive { get; set; }

	void OnReturnedToPool();
}
