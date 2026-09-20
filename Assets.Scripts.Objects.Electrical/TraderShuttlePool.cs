using System;
using System.Collections.Generic;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Electrical;

public sealed class TraderShuttlePool : Singleton<TraderShuttlePool>
{
	[Tooltip("Prefab references only")]
	[FormerlySerializedAs("_shuttles")]
	[SerializeField]
	private TraderShuttle[] _shuttlePrefabs;

	private List<TraderShuttle> _traderShuttles = new List<TraderShuttle>();

	private void Awake()
	{
		TraderShuttle[] shuttlePrefabs = _shuttlePrefabs;
		for (int i = 0; i < shuttlePrefabs.Length; i++)
		{
			shuttlePrefabs[i].CacheRenderers();
		}
	}

	public TraderShuttle RequestShuttle(ShuttleType shuttleType)
	{
		if (shuttleType == ShuttleType.None)
		{
			shuttleType = ShuttleType.Small;
		}
		TraderShuttle[] shuttlePrefabs = _shuttlePrefabs;
		foreach (TraderShuttle traderShuttle in shuttlePrefabs)
		{
			if (traderShuttle.ShuttleType == shuttleType)
			{
				TraderShuttle traderShuttle2 = UnityEngine.Object.Instantiate(traderShuttle);
				AddShuttle(traderShuttle2);
				return traderShuttle2;
			}
		}
		throw new Exception(string.Format("{0} '{1}' unavailable", "ShuttleType", shuttleType));
	}

	public void Clear()
	{
		foreach (TraderShuttle traderShuttle in _traderShuttles)
		{
			UnityEngine.Object.Destroy(traderShuttle.gameObject);
		}
		_traderShuttles.Clear();
	}

	public void AddShuttle(TraderShuttle shuttle)
	{
		_traderShuttles.Add(shuttle);
	}

	public void RemoveShuttle(TraderShuttle shuttle)
	{
		_traderShuttles.Remove(shuttle);
	}
}
