namespace TerrainSystem;

public interface IThreadable
{
	int ThreadCost { get; }

	bool CanThread();

	string DebugName();
}
