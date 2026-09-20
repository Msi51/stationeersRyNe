using System.Collections.Generic;

namespace TerrainSystem.Lods;

public class RollingDictionary<TKey, TValue>
{
	private Queue<TKey> _queue;

	public Dictionary<TKey, TValue> _dictionary;

	private int _maxCount;

	public RollingDictionary(int maxCount)
	{
		_maxCount = maxCount;
		_queue = new Queue<TKey>(maxCount);
		_dictionary = new Dictionary<TKey, TValue>(maxCount);
	}

	public bool TryGet(TKey key, out TValue value)
	{
		return _dictionary.TryGetValue(key, out value);
	}

	public void AddOrUpdate(TKey key, TValue value)
	{
		if (_dictionary.ContainsKey(key))
		{
			_dictionary[key] = value;
			return;
		}
		if (_queue.Count == _maxCount)
		{
			TKey key2 = _queue.Dequeue();
			_dictionary.Remove(key2);
		}
		_dictionary.Add(key, value);
		_queue.Enqueue(key);
	}

	public bool TryUpdate(TKey key, TValue value)
	{
		if (!_dictionary.ContainsKey(key))
		{
			return false;
		}
		_dictionary[key] = value;
		return true;
	}

	public void Clear()
	{
		_queue.Clear();
		_dictionary.Clear();
	}
}
