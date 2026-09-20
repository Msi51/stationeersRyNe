using System;
using UnityEngine;

namespace TerrainSystem;

public readonly struct CursorTerrain : IEquatable<CursorTerrain>
{
	public readonly Vein CursorVein;

	public readonly byte MinableIndex;

	public readonly bool IsTerrain;

	public readonly Vector3Int WorldPosition;

	public static readonly CursorTerrain Invalid = new CursorTerrain(null, 0, isTerrain: false, Vector3Int.zero);

	public bool IsValid => IsTerrain;

	public CursorTerrain(Vein vein, byte minableIndex, bool isTerrain, Vector3Int worldPos)
	{
		CursorVein = vein;
		MinableIndex = minableIndex;
		IsTerrain = isTerrain;
		WorldPosition = worldPos;
	}

	public bool Equals(CursorTerrain other)
	{
		if (((CursorVein == null && other.CursorVein == null) || CursorVein?.VeinWorldPosition == other.CursorVein?.VeinWorldPosition) && MinableIndex == other.MinableIndex)
		{
			return IsTerrain == other.IsTerrain;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is CursorTerrain other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine((CursorVein != null) ? CursorVein.VeinWorldPosition : WorldPosition, MinableIndex, IsTerrain);
	}

	public Vector3 GetCurrentWorldPosition()
	{
		throw new NotImplementedException();
	}
}
