using System.Collections.Generic;
using Assets.Scripts;

namespace TerrainSystem.Lods;

public class BasicPool<TKey, TValue> where TValue : IBasicPoolable<TKey>, new()
{
	public Dictionary<TKey, TValue> Active;

	private Queue<TValue> _queue;

	private int _totalCount;

	public int GetTotalCount()
	{
		return _totalCount;
	}

	public void PrePopulate(int count)
	{
		_queue = new Queue<TValue>(count);
		Active = new Dictionary<TKey, TValue>(count);
		for (int i = 0; i < count; i++)
		{
			PopulateOnce();
		}
	}

	public bool TryGetActive(TKey key, out TValue value)
	{
		return Active.TryGetValue(key, out value);
	}

	public TValue Get(TKey key)
	{
		if (Active.ContainsKey(key))
		{
			ConsoleWindow.PrintError($"Can't get from pool - active already contains key {key}");
			return default(TValue);
		}
		if (_queue.Count == 0)
		{
			PopulateOnce();
		}
		TValue val = _queue.Dequeue();
		Active.Add(key, val);
		val.IsActive = true;
		return val;
	}

	public void Return(TValue value)
	{
		if (!Active.Remove(value.Key))
		{
			ConsoleWindow.PrintError($"Returning to pool but active does not contain key: {value.Key}");
		}
		ReturnToQueue(value);
	}

	public void ReturnAll()
	{
		foreach (KeyValuePair<TKey, TValue> item in Active)
		{
			ReturnToQueue(item.Value);
		}
		Active.Clear();
		if (_queue.Count != _totalCount)
		{
			ConsoleWindow.PrintError($"Queue count is {_queue.Count} but expected {_totalCount}");
		}
	}

	private void ReturnToQueue(TValue value)
	{
		_queue.Enqueue(value);
		value.IsActive = false;
		value.OnReturnedToPool();
	}

	private void PopulateOnce()
	{
		TValue item = new TValue();
		_queue.Enqueue(item);
		_totalCount++;
	}
}
