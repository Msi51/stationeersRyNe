using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Util;
using UnityEngine;

namespace TerrainSystem.Lods;

public class LodMeshCache
{
	public class CacheObject : IndexedLinkedListItem<Vector3Int, CacheObject>
	{
		public LodMeshRenderer LodMeshRenderer;

		public void Clear()
		{
			Key = default(Vector3Int);
			Next = null;
			Previous = null;
			LodMeshRenderer.Clear();
		}
	}

	private LodMeshRenderer _lodMeshRendererPrefab;

	private int _level;

	private int _totalCount;

	private ConcurrentDictionary<Vector3Int, CacheObject> _activeLookup = new ConcurrentDictionary<Vector3Int, CacheObject>();

	private IndexedLinkedList<Vector3Int, CacheObject> _deactivated = new IndexedLinkedList<Vector3Int, CacheObject>();

	private Queue<CacheObject> _uninitialized = new Queue<CacheObject>();

	public (int total, int active, int deactivated, int uninitialised) GetCounts()
	{
		return (total: _totalCount, active: _activeLookup.Count, deactivated: _deactivated.GetCount(), uninitialised: _uninitialized.Count);
	}

	public LodMeshCache(LodMeshRenderer lodMeshRendererPrefab, int level)
	{
		_lodMeshRendererPrefab = lodMeshRendererPrefab;
		_level = level;
	}

	public bool TryGetDeactivated(Vector3Int key, out CacheObject cacheObject)
	{
		return _deactivated.TryGetValue(key, out cacheObject);
	}

	public bool TryGetActive(Vector3Int key, out CacheObject cacheObject)
	{
		return _activeLookup.TryGetValue(key, out cacheObject);
	}

	public bool TryReactivate(Vector3Int key, out CacheObject cacheObject)
	{
		if (_deactivated.TryRemove(key, out var value))
		{
			if (!_activeLookup.TryAdd(key, value))
			{
				ConsoleWindow.PrintError($"Trying to add {key} to active lookup but it already exists.");
				cacheObject = null;
				return false;
			}
			cacheObject = value;
			return true;
		}
		cacheObject = null;
		return false;
	}

	public CacheObject GetUninitialized(Vector3Int key)
	{
		if (_uninitialized.Count > 0)
		{
			CacheObject cacheObject = _uninitialized.Dequeue();
			cacheObject.Key = key;
			if (!_activeLookup.TryAdd(key, cacheObject))
			{
				throw new Exception($"Trying to add {key} to active lookup but it already exists.");
			}
			return cacheObject;
		}
		CacheObject cacheObject2 = _deactivated.TakeFirst();
		if (cacheObject2 != null)
		{
			if (!_activeLookup.TryAdd(key, cacheObject2))
			{
				throw new Exception($"Trying to add {key} to active lookup but it already exists.");
			}
			cacheObject2.Key = key;
			return cacheObject2;
		}
		CacheObject cacheObject3 = CreateNew(key);
		if (!_activeLookup.TryAdd(key, cacheObject3))
		{
			throw new Exception($"Trying to add {key} to active lookup but it already exists.");
		}
		return cacheObject3;
	}

	public void Deactivate(CacheObject cacheObject)
	{
		if (!_activeLookup.TryRemove(cacheObject.Key, out var _))
		{
			throw new Exception($"Trying to deactivate key {cacheObject.Key} but it is not in the active lookup.");
		}
		_deactivated.AddToEnd(cacheObject);
	}

	public void SetUninitialized(CacheObject cacheObject)
	{
		if (!_activeLookup.TryRemove(cacheObject.Key, out var _))
		{
			throw new Exception($"Trying to set key {cacheObject.Key} to uninitialized but it is not in the active lookup.");
		}
		cacheObject.Clear();
		_uninitialized.Enqueue(cacheObject);
	}

	public void Reset()
	{
		foreach (KeyValuePair<Vector3Int, CacheObject> item in _activeLookup)
		{
			item.Value.Clear();
			_uninitialized.Enqueue(item.Value);
		}
		_activeLookup.Clear();
		while (true)
		{
			CacheObject cacheObject = _deactivated.TakeFirst();
			if (cacheObject == null)
			{
				break;
			}
			cacheObject.Clear();
			_uninitialized.Enqueue(cacheObject);
		}
		if (!_deactivated.IsEmpty())
		{
			throw new Exception("Deactivated list is not empty after clear");
		}
	}

	public void PrePopulate(int count)
	{
		for (int i = 0; i < count; i++)
		{
			CacheObject item = CreateNew(Vector3Int.zero);
			_uninitialized.Enqueue(item);
		}
	}

	private CacheObject CreateNew(Vector3Int position)
	{
		LodMeshRenderer lodMeshRenderer = LodManager.Instance.InstantiateLodMeshRenderer(position, _lodMeshRendererPrefab);
		lodMeshRenderer.name = $"~LodMesh_Level{StringManager.Get(_level)}_{_totalCount}";
		CacheObject cacheObject = new CacheObject();
		cacheObject.Key = position;
		cacheObject.LodMeshRenderer = lodMeshRenderer;
		lodMeshRenderer.CacheObject = cacheObject;
		_totalCount++;
		return cacheObject;
	}
}
