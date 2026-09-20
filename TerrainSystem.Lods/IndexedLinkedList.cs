using System;
using System.Collections.Concurrent;
using Assets.Scripts;

namespace TerrainSystem.Lods;

public class IndexedLinkedList<TKey, TValue> where TValue : IndexedLinkedListItem<TKey, TValue>
{
	private ConcurrentDictionary<TKey, TValue> _lookup = new ConcurrentDictionary<TKey, TValue>();

	private TValue _head;

	private TValue _tail;

	public bool IsEmpty()
	{
		if (_lookup.Count == 0 && _head == null)
		{
			return _tail == null;
		}
		return false;
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		return _lookup.TryGetValue(key, out value);
	}

	public int GetCount()
	{
		return _lookup.Count;
	}

	public bool TryRemove(TKey key, out TValue value)
	{
		if (_lookup.TryRemove(key, out value))
		{
			Remove(value);
			return true;
		}
		return false;
	}

	public void AddToEnd(TValue value)
	{
		if (!_lookup.TryAdd(value.Key, value))
		{
			ConsoleWindow.PrintError($"Key {value.Key} is already in the lookup");
			return;
		}
		if (_head == null)
		{
			if (_tail != null)
			{
				throw new Exception("Head is null but tail is not null - this should not be possible.");
			}
			_head = value;
			_tail = value;
			return;
		}
		if (_tail == null)
		{
			throw new Exception("Tail is null but head is not null - this should not be possible.");
		}
		_tail.Next = value;
		value.Previous = _tail;
		_tail = value;
	}

	public TValue TakeFirst()
	{
		if (_head == null)
		{
			return null;
		}
		TValue head = _head;
		if (!_lookup.TryRemove(head.Key, out var _))
		{
			throw new Exception($"Key {head.Key} does not exist in the lookup");
		}
		Remove(head);
		return head;
	}

	private void Remove(TValue value)
	{
		if (value.Next != null)
		{
			if (value.Previous != null)
			{
				value.Next.Previous = value.Previous;
				value.Previous.Next = value.Next;
			}
			else
			{
				if (value != _head)
				{
					throw new Exception("Cache object has no previous but is not the head.");
				}
				value.Next.Previous = null;
				_head = value.Next;
			}
		}
		else
		{
			if (value != _tail)
			{
				throw new Exception("Cache object has no next but is not the tail.");
			}
			if (value.Previous != null)
			{
				value.Previous.Next = null;
				_tail = value.Previous;
			}
			else
			{
				_head = null;
				_tail = null;
			}
		}
		value.Next = null;
		value.Previous = null;
	}
}
