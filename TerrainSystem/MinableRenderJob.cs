using System;
using System.Collections.Generic;
using Assets.Scripts.Util;
using UnityEngine;

namespace TerrainSystem;

public readonly struct MinableRenderJob(DrawCallBlock block, BoundsInt bounds, MinableRenderJob.BoundsRule rule) : IThreadable, IEquatable<MinableRenderJob>
{
	public enum BoundsRule
	{
		None,
		RootInBounds,
		AnyInBounds
	}

	private readonly DrawCallBlock _block = block;

	private readonly BoundsInt _bounds = bounds;

	private readonly BoundsRule _rule = rule;

	public int ThreadCost => 1;

	public void DoWork()
	{
		_block.Clear();
		AddMinablesInBounds(_bounds, _block.DrawCalls, _rule, _block.RandomRotation, _block.HideInTerrain);
		_block.BlockBounds = _bounds;
		_block.CacheRenderBounds();
	}

	public void AddMinablesInBounds(BoundsInt boundsInt, InstancedIndirectDrawCall[] drawCalls, BoundsRule rule, bool randomRotation = true, bool hideInTerrain = true)
	{
		Vector3Int vector3Int = VoxelTerrain.WorldToOctreeSpaceClamped(boundsInt.min);
		Vector3Int vector3Int2 = VoxelTerrain.WorldToOctreeSpaceClamped(boundsInt.max);
		vector3Int /= 32;
		vector3Int *= 32;
		vector3Int2 /= 32;
		vector3Int2 *= 32;
		HashSet<Vein> hashSet = new HashSet<Vein>();
		for (int i = vector3Int.x; i <= vector3Int2.x; i += 32)
		{
			for (int j = vector3Int.y; j <= vector3Int2.y; j += 32)
			{
				for (int k = vector3Int.z; k <= vector3Int2.z; k += 32)
				{
					if (!Vein.VeinsLookup.TryGetValue(new Vector3Int(i, j, k), out List<Vein> value))
					{
						continue;
					}
					foreach (Vein item in value)
					{
						if (!hashSet.Contains(item) && (rule != BoundsRule.RootInBounds || boundsInt.ContainsXZ(item.VeinWorldPosition)))
						{
							hashSet.Add(item);
							item.AddToDrawCall(drawCalls[(uint)item.Type], randomRotation, hideInTerrain);
						}
					}
				}
			}
		}
		hashSet.Clear();
	}

	public bool CanThread()
	{
		return true;
	}

	public string DebugName()
	{
		return string.Empty;
	}

	public bool Equals(MinableRenderJob other)
	{
		return object.Equals(_block, other._block);
	}

	public override bool Equals(object obj)
	{
		if (obj is MinableRenderJob other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _block.GetHashCode();
	}
}
