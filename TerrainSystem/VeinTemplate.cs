using UnityEngine;

namespace TerrainSystem;

public readonly struct VeinTemplate : IThreadable
{
	public VeinGenerationData Data { get; }

	public Vector3Int WorldPosition { get; }

	public Vector3Int ClusterPosition { get; }

	public int ThreadCost => 1;

	public VeinTemplate(VeinGenerationData data, Vector3Int position, Vector3Int clusterPosition)
	{
		Data = data;
		WorldPosition = position;
		ClusterPosition = clusterPosition;
	}

	public int GetSeed(int worldSeed)
	{
		return worldSeed ^ WorldPosition.GetHashCode();
	}

	public bool CanThread()
	{
		return true;
	}

	public string DebugName()
	{
		return $"VeinTemplate_{Data.Type}_{WorldPosition}";
	}
}
