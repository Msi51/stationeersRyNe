using System.Collections.Concurrent;
using Assets.Scripts.Objects.Entities;

namespace TerrainSystem.Lods;

public readonly struct VeinSeedJob : IThreadable
{
	private readonly ConcurrentQueue<IGenerateMinables> _generateMinablesQueue;

	public int ThreadCost => 1;

	public bool CanThread()
	{
		return true;
	}

	public string DebugName()
	{
		return "Seed Vein Job";
	}

	public void DoWork()
	{
		VoxelTerrain.ClusterSeeding();
	}
}
