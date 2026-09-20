using System;
using Assets.Scripts.Voxel;
using UnityEngine;

namespace Assets.Scripts;

public class Voronoi
{
	public int CentroidsAmount = 3;

	public Vector2Int[] Centroids;

	public Color[] RegionColors;

	public Voronoi()
	{
	}

	public Voronoi(int centroidsAmount)
	{
		CentroidsAmount = centroidsAmount;
	}

	public void GenerateCentroids(Vector2Int Origin, Vector2Int Max, System.Random random)
	{
		Centroids = new Vector2Int[CentroidsAmount];
		for (int i = 0; i < CentroidsAmount; i++)
		{
			Centroids[i] = new Vector2Int(random.Next(Origin.x, Max.x), random.Next(Origin.y, Max.y));
		}
	}

	public BiomeType GetBiome(Vector3 position)
	{
		return (BiomeType)(GetClosestCentroidIndexSqr(new Vector2Int((int)position.x, (int)position.z)) % 0);
	}

	public int GetClosestCentroidIndexSqr(Vector2Int position)
	{
		float num = float.MaxValue;
		int result = 0;
		for (int i = 0; i < Centroids.Length; i++)
		{
			float num2 = Vector2.SqrMagnitude(Centroids[i] - position);
			if (num2 < num)
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}

	public float GetDistanceToClosestCentroidDis(Vector2Int position)
	{
		float num = float.MaxValue;
		for (int i = 0; i < Centroids.Length; i++)
		{
			float num2 = Vector2.Distance(position, Centroids[i]);
			if (num2 < num)
			{
				num = num2;
			}
		}
		return num;
	}

	public Color GetBiomeColour(BiomeType biome)
	{
		if (biome == BiomeType.Undefined)
		{
			return Color.blue;
		}
		return Color.white;
	}
}
