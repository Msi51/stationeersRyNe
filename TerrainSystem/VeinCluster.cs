using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Util;
using TerrainSystem.Lods;
using Trading;
using UnityEngine;

namespace TerrainSystem;

public class VeinCluster : IThreadable, IEvaluable
{
	public const float DEFAULT_CLUSTER_SIZE = 12f;

	public Vector3Int WorldPosition;

	public Vector3 CenterPosition;

	private readonly List<Vein> _veins;

	public VeinClusterStatus Status;

	public const int LOOKUP_SIZE = 32768;

	private static readonly ConcurrentDictionary<Vector3Int, VeinCluster> Lookup = new ConcurrentDictionary<Vector3Int, VeinCluster>(Environment.ProcessorCount, 32768);

	private static List<MinablesGenerationData> _datas = new List<MinablesGenerationData>(8);

	private static HashSet<Vector3Int> _positionsInCluster = new HashSet<Vector3Int>();

	public int ThreadCost => _veins.Count;

	public bool CanThread()
	{
		return Status != VeinClusterStatus.Generating;
	}

	public string DebugName()
	{
		return $"VeinCluster_{WorldPosition}";
	}

	public VeinCluster(Vector3Int worldPosition)
	{
		WorldPosition = worldPosition;
		CenterPosition = worldPosition + new Vector3(16f, 16f, 16f);
		_veins = new List<Vein>(Mathf.CeilToInt(12f));
	}

	public void Clear()
	{
		lock (_veins)
		{
			foreach (Vein vein in _veins)
			{
				vein.Clear();
			}
			_veins.Clear();
		}
	}

	public bool IsModifed()
	{
		lock (_veins)
		{
			foreach (Vein vein in _veins)
			{
				if (vein.IsModified)
				{
					return true;
				}
			}
			return false;
		}
	}

	public static bool GetCluster(Vector3Int worldPosition, out VeinCluster cluster)
	{
		Vector3Int key = VoxelTerrain.WorldToOctreeSpaceClamped(worldPosition) / 32;
		return Lookup.TryGetValue(key, out cluster);
	}

	public static bool Register(VeinCluster cluster)
	{
		Vector3Int vector3Int = VoxelTerrain.WorldToOctreeSpaceClamped(cluster.WorldPosition) / 32;
		if (Lookup.TryGetValue(vector3Int, out VeinCluster value))
		{
			ConsoleWindow.PrintError($"{cluster.DebugName()} failed to register at {vector3Int}. {value.DebugName()} Already registered at position");
			return false;
		}
		return Lookup.TryAdd(vector3Int, cluster);
	}

	public static void Register(Vein vein)
	{
		if (!GetCluster(vein.ClusterPosition, out VeinCluster cluster))
		{
			cluster = new VeinCluster(vein.ClusterPosition)
			{
				Status = VeinClusterStatus.Partial
			};
			Register(cluster);
		}
		cluster.AddVein(vein);
	}

	public void AddVein(Vein newVein)
	{
		lock (_veins)
		{
			_veins.Add(newVein);
		}
	}

	public static void ClearAll()
	{
		foreach (VeinCluster value in Lookup.Values)
		{
			value.Clear();
		}
		Lookup.Clear();
	}

	public static void Generate(Vector3Int worldPosition)
	{
		if (GetCluster(worldPosition, out VeinCluster cluster) && cluster.Status != VeinClusterStatus.Partial)
		{
			return;
		}
		if (cluster == null)
		{
			cluster = new VeinCluster(worldPosition);
			if (!Register(cluster))
			{
				cluster.Clear();
				return;
			}
		}
		_datas.Clear();
		_positionsInCluster.Clear();
		foreach (Vein vein in cluster._veins)
		{
			_positionsInCluster.Add(vein.VeinWorldPosition);
		}
		foreach (MinablesGenerationData minablesDatum in WorldSetting.Current.Data.MinablesData)
		{
			if (minablesDatum.Evaluate(cluster))
			{
				_datas.Add(minablesDatum);
			}
		}
		cluster.Status = VeinClusterStatus.Generating;
		System.Random random = new System.Random(cluster.Seed());
		foreach (MinablesGenerationData data in _datas)
		{
			float num = 12f * data.OreDensityClamped();
			int num2 = (int)num;
			float num3 = num - (float)num2;
			if (random.NextDouble() < (double)num3)
			{
				num2++;
			}
			for (int i = 0; i < num2; i++)
			{
				Vector3Int max = worldPosition + new Vector3Int(32, 32, 32);
				Vector3Int vector3Int = random.RandomInBox(worldPosition, max);
				VeinGenerationData randomVeinType = MinablesGenerationData.GetRandomVeinType(data, random);
				if (VoxelTerrain.GetReadonlyDensityWorldSpace(vector3Int) >= 127 && !WorldSetting.Current.IsUnderLava(vector3Int) && !cluster.GetVein(vector3Int, out Vein _) && !_positionsInCluster.Contains(vector3Int))
				{
					_positionsInCluster.Add(vector3Int);
					VeinGenerationWorker.Assign(new VeinTemplate(randomVeinType, vector3Int, cluster.WorldPosition));
				}
			}
		}
	}

	private int Seed()
	{
		return WorldManager.Seed ^ WorldPosition.GetHashCode();
	}

	public void GetModified(List<Vein> modified)
	{
		lock (_veins)
		{
			foreach (Vein vein in _veins)
			{
				if (vein.IsModified)
				{
					modified.Add(vein);
				}
			}
		}
	}

	private bool GetVein(Vector3Int position, out Vein foundVein)
	{
		lock (_veins)
		{
			foundVein = null;
			foreach (Vein vein in _veins)
			{
				if (vein.VeinWorldPosition == position)
				{
					foundVein = vein;
					return true;
				}
			}
			return false;
		}
	}

	public static List<Vein> GetModifiedVeins()
	{
		List<Vein> list = new List<Vein>(1024);
		foreach (KeyValuePair<Vector3Int, VeinCluster> item in Lookup)
		{
			item.Value.GetModified(list);
		}
		return list;
	}

	public static void DeduplicateMinables(VeinCluster veinCluster, ref List<Vein> neighbourVeins)
	{
		foreach (Vein vein in veinCluster._veins)
		{
			Vein.DeduplicateMinables(vein, ref neighbourVeins);
		}
	}

	public bool HasVeins()
	{
		return _veins.Count > 0;
	}
}
