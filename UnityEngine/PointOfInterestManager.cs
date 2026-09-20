using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Rockets;
using TerrainSystem;
using UI;
using Util;

namespace UnityEngine;

public static class PointOfInterestManager
{
	private static Dictionary<int, PointOfInterest> _regionToPoiLookup = new Dictionary<int, PointOfInterest>();

	private static HashSet<int> _discovered = new HashSet<int>();

	private static bool _canCheckPointsOfInterest = true;

	private static CancellationTokenWrapper _checkCancellation = new CancellationTokenWrapper();

	private static List<Region> _reusableRegionList = new List<Region>(4);

	private const int TIME_BETWEEN_POI_CHECKS = 5000;

	public static void DiscoverPoiAtStartLocationWithNoMessage()
	{
		Vector2Reference vector2Reference = WorldSetting.Current?.StartLocationData?.Position;
		if (vector2Reference != null)
		{
			Vector3 vector = vector2Reference.ToVector2();
			DiscoverAnyPoiWithNoMessage(new Vector3(vector.x, 0f, vector.y));
		}
	}

	public static void CheckPointsOfInterest(Vector3 position)
	{
		if (!(position.y >= 1000f) && _canCheckPointsOfInterest)
		{
			_canCheckPointsOfInterest = false;
			_checkCancellation.CancelAndInitialize();
			CheckTask(position, _checkCancellation.Token).Forget();
		}
	}

	public static void DiscoverAnyPoiWithNoMessage(Vector3 position)
	{
		_reusableRegionList.Clear();
		if (!RegionManager.TryGetRegionsAtWorldPosition(position, _reusableRegionList))
		{
			return;
		}
		foreach (Region reusableRegion in _reusableRegionList)
		{
			if (_regionToPoiLookup.TryGetValue(reusableRegion.IdHash, out var value))
			{
				_discovered.Add(value.IdHash);
			}
		}
	}

	public static bool GetPointOfInterest(Region region, out PointOfInterest poi)
	{
		return _regionToPoiLookup.TryGetValue(region.IdHash, out poi);
	}

	public static void AddPointOfInterest(PointOfInterest pointOfInterest)
	{
		_regionToPoiLookup.TryAdd(pointOfInterest.Region.IdHash, pointOfInterest);
	}

	private static void DiscoverPoi(PointOfInterest poi)
	{
		ShowPopup(poi.Name, poi.Description, UIAudioManager.PointOfInterestDiscovered);
		poi.OnDiscovered();
		_discovered.Add(poi.IdHash);
		AchievementChainData.CheckOnDiscovered(poi);
	}

	public static void ShowPopup(string name, string description, int soundHash, float time = 5f)
	{
		if (soundHash == UIAudioManager.PointOfInterestSpace && Singleton<AudioManager>.Instance != null)
		{
			Singleton<AudioManager>.Instance.SuppressMapMusic(time);
		}
		UIAudioManager.Play(soundHash);
		PointOfInterestMessage.Show(name, description, time);
	}

	public static bool IsDiscovered(string id)
	{
		return IsDiscovered(Animator.StringToHash(id));
	}

	public static bool IsDiscovered(int idHash)
	{
		return _discovered.Contains(idHash);
	}

	private static async UniTaskVoid CheckTask(Vector3 position, CancellationToken cancellationToken)
	{
		_reusableRegionList.Clear();
		if (!RegionManager.TryGetRegionsAtWorldPosition(position, _reusableRegionList))
		{
			return;
		}
		foreach (Region reusableRegion in _reusableRegionList)
		{
			if (_regionToPoiLookup.TryGetValue(reusableRegion.IdHash, out var value) && !_discovered.Contains(value.IdHash))
			{
				DiscoverPoi(value);
				break;
			}
		}
		await UniTask.Delay(5000, ignoreTimeScale: false, PlayerLoopTiming.Update, cancellationToken);
		_canCheckPointsOfInterest = true;
	}

	public static void ClearAll()
	{
		_checkCancellation.Cancel();
		_canCheckPointsOfInterest = true;
		_discovered.Clear();
		_reusableRegionList.Clear();
	}

	public static void Load(List<int> discoveredList)
	{
		_discovered.Clear();
		foreach (int discovered in discoveredList)
		{
			_discovered.Add(discovered);
		}
		AchievementChainData.CheckAll();
	}

	public static List<int> GetSaveData()
	{
		List<int> list = new List<int>(_discovered.Count);
		foreach (int item in _discovered)
		{
			list.Add(item);
		}
		return list;
	}
}
