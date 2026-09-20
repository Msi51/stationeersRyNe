namespace TerrainSystem;

public interface IOctTree
{
	int MaxDepth { get; }

	byte GetRootDensity();
}
