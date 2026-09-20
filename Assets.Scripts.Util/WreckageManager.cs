using System.Collections.Generic;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Util;

public class WreckageManager : ManagerBase
{
	[SerializeField]
	private List<Wreckage> smallWreckagePrefabs = new List<Wreckage>();

	[SerializeField]
	private List<Wreckage> mediumWreckagePrefabs = new List<Wreckage>();

	public static WreckageManager Instance;

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		Instance = this;
	}

	public static void SpawnWreckage(IWreckage iWreckage)
	{
		iWreckage.HasSpawnedWreckage = true;
		for (int i = 0; i < iWreckage.WreckageQuantity; i++)
		{
			Thing thing = null;
			WreckageSize wreckageSize = iWreckage.WreckageSize;
			thing = (((uint)wreckageSize <= 1u || (uint)(wreckageSize - 2) > 1u) ? Instance.smallWreckagePrefabs.Pick() : Instance.mediumWreckagePrefabs.Pick());
			Spawn(thing, iWreckage);
		}
		if (iWreckage.WreckagePrefabs != null)
		{
			Wreckage[] wreckagePrefabs = iWreckage.WreckagePrefabs;
			for (int j = 0; j < wreckagePrefabs.Length; j++)
			{
				Spawn(wreckagePrefabs[j], iWreckage);
			}
		}
	}

	private static void Spawn(Thing prefab, IWreckage parent)
	{
		if ((object)prefab != null)
		{
			Vector3 vector = parent.GetLocalBounds.ClosestPoint(Vector3.up);
			Wreckage wreckage = OnServer.Create<Wreckage>(prefab.PrefabHash, vector + parent.Position, Random.rotation);
			if (parent.CustomColorIndex >= 0)
			{
				wreckage.SetCustomColor(parent.CustomColorIndex);
			}
			wreckage.WreckedParentPrefabHash = parent.GetPrefabHash();
		}
	}
}
