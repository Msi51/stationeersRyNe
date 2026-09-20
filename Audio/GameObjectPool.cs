using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audio;

public class GameObjectPool<T> where T : GameBase, IGamePoolable<T>, new()
{
	private UnityReference _parentQueue;

	internal List<T> _all;

	internal Queue<T> _queue;

	internal List<T> _active;

	private string _poolName;

	private string _typeName;

	private int _poolSize;

	public string DebugName => _poolName;

	public int Available => _queue.Count;

	public int Active => _active.Count;

	public GameObjectPool(string name)
	{
		_typeName = typeof(T).Name;
		_poolName = name + "Pool";
		_parentQueue = UnityReference.Create("~" + _poolName, isActive: false);
	}

	public T Get()
	{
		if (_queue.Count == 0)
		{
			return null;
		}
		T val = _queue.Dequeue();
		if (val == null || val.Transform == null)
		{
			return null;
		}
		if (!val.IsVisible)
		{
			val.SetVisible(isVisble: true);
		}
		_active.Add(val);
		val.Transform.SetParent(null);
		return val;
	}

	public void Return(T item)
	{
		if (item.GamePool != this)
		{
			throw new Exception(item.name + " is not a member of " + _poolName);
		}
		if (!_active.Contains(item))
		{
			if (!_queue.Contains(item))
			{
				throw new Exception(item.name + " is not a member of " + _poolName);
			}
		}
		else
		{
			_active.Remove(item);
		}
		item.Transform.SetParent(_parentQueue.Transform);
		if (!_queue.Contains(item))
		{
			_queue.Enqueue(item);
		}
		item.Transform.SetParent(_parentQueue.Transform);
	}

	public void ReturnAll()
	{
		for (int num = _all.Count - 1; num >= 0; num--)
		{
			_all[num].ReturnToPool();
		}
	}

	public void Initialize(int poolSize)
	{
		_all = new List<T>(poolSize);
		_active = new List<T>(poolSize);
		_queue = new Queue<T>(poolSize);
		_poolSize = poolSize;
	}

	public T PopulateOnce(T prefab)
	{
		T val = UnityEngine.Object.Instantiate(prefab, _parentQueue.Transform);
		val.PoolId = _all.Count;
		val.DebugName = $"{typeof(T).Name}Source{val.PoolId}";
		val.name = "~" + val.DebugName;
		val.GamePool = this;
		_queue.Enqueue(val);
		_all.Add(val);
		return val;
	}

	public void PopulateAll(T prefab)
	{
		for (int i = 0; i < _poolSize; i++)
		{
			PopulateOnce(prefab);
		}
	}
}
