using System.Collections.Generic;
using UnityEngine;

namespace TerrainSystem.Lods;

public readonly struct LodGroup(int level, HashSet<Vector3Int> indices)
{
	public readonly int Level = level;

	public readonly HashSet<Vector3Int> Indices = indices;

	public void Clear()
	{
		Indices.Clear();
	}
}
