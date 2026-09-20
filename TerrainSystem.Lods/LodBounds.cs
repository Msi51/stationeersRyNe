using UnityEngine;

namespace TerrainSystem.Lods;

public struct LodBounds(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
{
	public Vector3Int Min = new Vector3Int(minX, minY, minZ);

	public Vector3Int Max = new Vector3Int(maxX, maxY, maxZ);

	public static LodBounds Zero()
	{
		return new LodBounds(0, 0, 0, 0, 0, 0);
	}

	public bool Outside(Vector3Int position)
	{
		if (position.x >= Max.x || position.x < Min.x)
		{
			return true;
		}
		if (position.y >= Max.y || position.y < Min.y)
		{
			return true;
		}
		if (position.z >= Max.z || position.z < Min.z)
		{
			return true;
		}
		return false;
	}
}
