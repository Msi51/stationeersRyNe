using System.Collections.Generic;
using UnityEngine;

namespace TerrainSystem.Lods;

public static class LodHelper
{
	public static HashSet<Vector3Int>[] InitArray(int count)
	{
		HashSet<Vector3Int>[] array = new HashSet<Vector3Int>[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = new HashSet<Vector3Int>();
		}
		return array;
	}

	public static void Reset(this HashSet<Vector3Int>[] array)
	{
		for (int i = 0; i < array.Length; i++)
		{
			array[i]?.Clear();
		}
	}

	public static void Reset(this List<Matrix4x4>[] array)
	{
		for (int i = 0; i < array.Length; i++)
		{
			array[i]?.Clear();
		}
	}

	public static void RemoveRequested(this ILodRequester requester, LodObject lodObject)
	{
		requester.RequestedLods[lodObject.Level].Remove(lodObject.Index);
	}
}
