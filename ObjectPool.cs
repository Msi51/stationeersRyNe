using System;
using System.Collections.Generic;
using System.Linq;

public class ObjectPool<T> where T : IPoolable<T>, new()
{
	internal Queue<T> _queue;

	internal HashSet<T> _active;

	private readonly string _poolName;

	private readonly string _typeName;

	private readonly bool _expandable;

	private int _poolSize;

	public string DebugName => _poolName;

	public int Available => _queue.Count;

	public int Active => _active.Count;

	public int Size => Active + Available;

	public ObjectPool(string name, bool expandable = true)
	{
		_typeName = typeof(T).Name;
		_poolName = name + "Pool";
		_expandable = expandable;
	}

	public T Get()
	{
		T val = default(T);
		if (_queue.Count == 0)
		{
			if (_expandable)
			{
				val = PopulateOnce();
			}
		}
		else
		{
			val = _queue.Dequeue();
		}
		Prepare(val);
		return val;
	}

	private void Prepare(T item)
	{
		if (!item.IsVisible)
		{
			item.SetVisible(isVisible: true);
		}
		_active.Add(item);
		item.IsActive = true;
	}

	public void Return(T item)
	{
		if (item.Pool != this)
		{
			throw new Exception(item.DebugName + " is not a member of " + _poolName);
		}
		if (!_active.Contains(item))
		{
			throw new Exception(item.DebugName + " is not active for " + _poolName);
		}
		_active.Remove(item);
		_queue.Enqueue(item);
		item.IsActive = false;
	}

	public void ReturnAll()
	{
		foreach (T item in _active.ToList())
		{
			item.ReturnToPool();
		}
	}

	public void Initialize(int poolSize)
	{
		_active = new HashSet<T>(poolSize);
		_queue = new Queue<T>(poolSize);
		_poolSize = poolSize;
	}

	public T PopulateOnce()
	{
		T val = new T
		{
			PoolId = _queue.Count
		};
		string debugName = $"{typeof(T).Name}Source{val.PoolId}";
		val.DebugName = debugName;
		val.Pool = this;
		_queue.Enqueue(val);
		return val;
	}

	public void PopulateAll()
	{
		for (int i = 0; i < _poolSize; i++)
		{
			PopulateOnce();
		}
	}
}
