using UnityEngine;

namespace TerrainSystem.Lods;

public static class LodObjectCache
{
	public static RollingDictionary<Vector3Int, LodObjectState>[] LodObjectStateLookup;

	public static BasicPool<Vector3Int, LodObject>[] LodObjectPools;

	public static int[] ObjectsPerLevel = new int[6] { 15000, 10000, 1500, 2000, 2000, 2000 };

	public static void Initialize()
	{
		LodObjectPools = new BasicPool<Vector3Int, LodObject>[6];
		LodObjectStateLookup = new RollingDictionary<Vector3Int, LodObjectState>[6];
		for (int i = 0; i < 6; i++)
		{
			LodObjectPools[i] = new BasicPool<Vector3Int, LodObject>();
			LodObjectPools[i].PrePopulate(ObjectsPerLevel[i]);
			LodObjectStateLookup[i] = new RollingDictionary<Vector3Int, LodObjectState>(10000);
		}
	}

	public static bool IsStateEmpty(Vector3Int index, int level)
	{
		if (LodObjectStateLookup[level].TryGet(index, out var value) && value == LodObjectState.Empty)
		{
			return true;
		}
		return false;
	}

	public static void SetStateEmpty(Vector3Int index, int level)
	{
		LodObjectStateLookup[level].AddOrUpdate(index, LodObjectState.Empty);
	}

	public static void SetStateUnknown(Vector3Int index, int level)
	{
		LodObjectStateLookup[level].TryUpdate(index, LodObjectState.Unknown);
	}

	public static void ClearAll()
	{
		if (LodObjectStateLookup != null)
		{
			RollingDictionary<Vector3Int, LodObjectState>[] lodObjectStateLookup = LodObjectStateLookup;
			for (int i = 0; i < lodObjectStateLookup.Length; i++)
			{
				lodObjectStateLookup[i].Clear();
			}
		}
		if (LodObjectPools != null)
		{
			BasicPool<Vector3Int, LodObject>[] lodObjectPools = LodObjectPools;
			for (int i = 0; i < lodObjectPools.Length; i++)
			{
				lodObjectPools[i].ReturnAll();
			}
		}
	}

	public static bool TryGetActive(Vector3Int index, int level, out LodObject lodObject)
	{
		return LodObjectPools[level].TryGetActive(index, out lodObject);
	}

	public static LodObject GetFromPool(Vector3Int index, int level)
	{
		LodObject lodObject = LodObjectPools[level].Get(index);
		lodObject.Set(index, level);
		return lodObject;
	}

	public static void Return(LodObject lodObject)
	{
		LodObjectPools[lodObject.Level].Return(lodObject);
	}
}
