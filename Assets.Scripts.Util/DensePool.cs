using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Assets.Scripts.Networking;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Util;

public class DensePool<T> : IDensePool, IListable where T : class, IDensePoolable
{
	public readonly struct Ref(int slot)
	{
		public readonly int Slot = slot;

		public T GetFrom(DensePool<T> pool)
		{
			return pool._entries[Slot];
		}
	}

	public readonly ref struct ActiveEnumerable
	{
		public ref struct Enumerator
		{
			private readonly T[] _entries;

			private readonly int[] _active;

			private readonly int _count;

			private int _i;

			public T Current => _entries[_active[_i]];

			internal Enumerator(T[] e, int[] a, int c)
			{
				_entries = e;
				_active = a;
				_count = c;
				_i = -1;
			}

			public bool MoveNext()
			{
				return ++_i < _count;
			}
		}

		private readonly T[] _entries;

		private readonly int[] _active;

		private readonly int _count;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ActiveEnumerable(T[] e, int[] a, int c)
		{
			_entries = e;
			_active = a;
			_count = c;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Enumerator GetEnumerator()
		{
			return new Enumerator(_entries, _active, _count);
		}
	}

	private readonly string _poolName;

	private readonly T[] _entries;

	private readonly int[] _freeList;

	private readonly int[] _activeList;

	private readonly int[] _slotToActiveIndex;

	private int _freeCount;

	private int _activeCount;

	private static readonly Random _pickRandom = new Random();

	public string Name => _poolName;

	public int ActiveCount => _activeCount;

	public DensePool(string poolName, int capacity)
	{
		_poolName = poolName;
		_entries = new T[capacity];
		_freeList = new int[capacity];
		_activeList = new int[capacity];
		_slotToActiveIndex = new int[capacity];
		for (int i = 0; i < capacity; i++)
		{
			_freeList[i] = i;
			_slotToActiveIndex[i] = -1;
		}
		_freeCount = capacity;
		_activeCount = 0;
		DensePools.Register(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public virtual bool Add(T item)
	{
		lock (this)
		{
			if (_freeCount == 0)
			{
				throw new IndexOutOfRangeException("DensePool '" + _poolName + "' is full, cannot allocated a new '" + typeof(T).Name + "'");
			}
			int num = _freeList[_freeCount - 1];
			if (!item.OnAddToPool(this, num))
			{
				return false;
			}
			_freeCount--;
			_entries[num] = item;
			_slotToActiveIndex[num] = _activeCount;
			_activeList[_activeCount++] = num;
			return true;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public virtual void RemoveAt(int slot)
	{
		int num = _slotToActiveIndex[slot];
		if (num >= 0 && num < _activeCount)
		{
			int num2 = _activeList[--_activeCount];
			_activeList[num] = num2;
			_slotToActiveIndex[num2] = num;
			_slotToActiveIndex[slot] = -1;
			_entries[slot] = null;
			_freeList[_freeCount++] = slot;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public virtual void Remove(T item)
	{
		item?.OnRemoveFromPool(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public virtual void ForEach(Action<T> action)
	{
		if (action == null)
		{
			throw new ArgumentNullException("action");
		}
		T[] entries = _entries;
		int[] activeList = _activeList;
		int activeCount = _activeCount;
		for (int i = 0; i < activeCount; i++)
		{
			action(entries[activeList[i]]);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<T> Snapshot(Span<T> buffer)
	{
		T[] entries = _entries;
		int[] activeList = _activeList;
		int activeCount = _activeCount;
		if (buffer.Length < activeCount)
		{
			throw new ArgumentException($"Buffer too small: need {activeCount}, got {buffer.Length}", "buffer");
		}
		for (int i = 0; i < activeCount; i++)
		{
			buffer[i] = entries[activeList[i]];
		}
		Span<T> span = buffer;
		return span.Slice(0, activeCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public virtual ActiveEnumerable Active()
	{
		return new ActiveEnumerable(_entries, _activeList, _activeCount);
	}

	public virtual void RemoveWhere(Predicate<T> match)
	{
		if (match == null)
		{
			throw new ArgumentNullException("match");
		}
		T[] entries = _entries;
		int[] activeList = _activeList;
		for (int num = _activeCount - 1; num >= 0; num--)
		{
			T val = entries[activeList[num]];
			if (match(val))
			{
				Remove(val);
			}
		}
	}

	public virtual void Populate(Span<Ref> atmosBuf, ref int atmosCount)
	{
		int[] activeList = _activeList;
		int activeCount = _activeCount;
		for (int i = 0; i < activeCount; i++)
		{
			atmosBuf[atmosCount++] = new Ref(activeList[i]);
		}
	}

	public void ForEach<TInt>(RocketBinaryWriter writer, Func<RocketBinaryWriter, T, bool> action, ref TInt writtenCount) where TInt : unmanaged
	{
		T[] entries = _entries;
		int[] activeList = _activeList;
		int activeCount = _activeCount;
		Type typeFromHandle = typeof(TInt);
		if (typeFromHandle == typeof(byte))
		{
			ref byte reference = ref Unsafe.As<TInt, byte>(ref writtenCount);
			for (int i = 0; i < activeCount; i++)
			{
				if (action(writer, entries[activeList[i]]))
				{
					reference++;
				}
			}
			return;
		}
		if (typeFromHandle == typeof(ushort))
		{
			ref ushort reference2 = ref Unsafe.As<TInt, ushort>(ref writtenCount);
			for (int j = 0; j < activeCount; j++)
			{
				if (action(writer, entries[activeList[j]]))
				{
					reference2++;
				}
			}
			return;
		}
		if (typeFromHandle == typeof(uint))
		{
			ref uint reference3 = ref Unsafe.As<TInt, uint>(ref writtenCount);
			for (int k = 0; k < activeCount; k++)
			{
				if (action(writer, entries[activeList[k]]))
				{
					reference3++;
				}
			}
			return;
		}
		if (typeFromHandle == typeof(int))
		{
			ref int reference4 = ref Unsafe.As<TInt, int>(ref writtenCount);
			for (int l = 0; l < activeCount; l++)
			{
				if (action(writer, entries[activeList[l]]))
				{
					reference4++;
				}
			}
			return;
		}
		if (typeFromHandle == typeof(short))
		{
			ref short reference5 = ref Unsafe.As<TInt, short>(ref writtenCount);
			for (int m = 0; m < activeCount; m++)
			{
				if (action(writer, entries[activeList[m]]))
				{
					reference5++;
				}
			}
			return;
		}
		if (typeFromHandle == typeof(long))
		{
			ref long reference6 = ref Unsafe.As<TInt, long>(ref writtenCount);
			for (int n = 0; n < activeCount; n++)
			{
				if (action(writer, entries[activeList[n]]))
				{
					reference6++;
				}
			}
			return;
		}
		throw new NotSupportedException("Unsupported TInt " + typeFromHandle.Name);
	}

	public bool ForEach(Func<T, bool> action)
	{
		for (int i = 0; i < _activeCount; i++)
		{
			if (action(_entries[_activeList[i]]))
			{
				return true;
			}
		}
		return false;
	}

	public virtual void Clear()
	{
		int num = _entries.Length;
		_activeCount = 0;
		_freeCount = num;
		for (int i = 0; i < num; i++)
		{
			_entries[i] = null;
			_freeList[i] = i;
			_slotToActiveIndex[i] = -1;
			_activeList[i] = 0;
		}
	}

	public virtual void Cleanup()
	{
		for (int num = _activeCount - 1; num >= 0; num--)
		{
			int num2 = _activeList[num];
			if (_entries[num2] == null)
			{
				RemoveAt(num2);
			}
		}
	}

	public virtual TReturn FindUsing<TReturn, TThing>(Func<T, TThing, TReturn> action, TThing inputItem) where TReturn : T, IDensePoolable
	{
		for (int num = _activeCount - 1; num >= 0; num--)
		{
			int num2 = _activeList[num];
			if (_entries[num2] != null)
			{
				TReturn val = action(_entries[num2], inputItem);
				if (val != null)
				{
					return val;
				}
			}
		}
		return default(TReturn);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Pick()
	{
		if (_activeCount == 0)
		{
			return null;
		}
		int num = _pickRandom.Next(_activeCount);
		int num2 = _activeList[num];
		return _entries[num2];
	}

	public void DrawInList(ref int index)
	{
		ConsoleWindow.Print($"'{_poolName}' has {_activeCount} active items and {_freeCount} free slots");
	}

	public void DrawNullCountInList(ref int index)
	{
		int num = 0;
		for (int i = 0; i < _activeCount; i++)
		{
			if (_entries[_activeList[i]] == null)
			{
				num++;
			}
		}
		ConsoleWindow.Print($"'{_poolName}' has {num} null items");
	}

	public void DrawSummary()
	{
		ConsoleWindow.PrintAction("summary for '" + _poolName + "'");
		int num = 0;
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		for (int i = 0; i < _activeCount; i++)
		{
			if (_entries[_activeList[i]] == null)
			{
				num++;
				continue;
			}
			string name = _entries[_activeList[i]].GetType().Name;
			if (dictionary.TryGetValue(name, out var value))
			{
				dictionary[name] = value + 1;
			}
			else
			{
				dictionary[name] = 1;
			}
		}
		List<PoolSummary> list = new List<PoolSummary>(dictionary.Count);
		foreach (KeyValuePair<string, int> item in dictionary)
		{
			list.Add(new PoolSummary(item.Key, item.Value));
		}
		list.Sort(delegate(PoolSummary a, PoolSummary b)
		{
			int activeCount = b.ActiveCount;
			return activeCount.CompareTo(a.ActiveCount);
		});
		foreach (PoolSummary item2 in list)
		{
			ConsoleWindow.Print($"'{item2.Name}' has {item2.ActiveCount} items");
		}
	}

	public List<T> ToList()
	{
		List<T> list = new List<T>(_activeCount);
		for (int i = 0; i < _activeCount; i++)
		{
			int num = _activeList[i];
			if (_entries[num] != null)
			{
				list.Add(_entries[num]);
			}
		}
		return list;
	}

	public T Find(Func<T, bool> func)
	{
		for (int i = 0; i < _activeCount; i++)
		{
			int num = _activeList[i];
			T val = _entries[num];
			if (val != null && func(val))
			{
				return val;
			}
		}
		return null;
	}

	public async UniTask ForEachAsync(PlayerLoopTiming timing, CancellationToken cancellationToken, Func<T, bool> action)
	{
		if (action == null)
		{
			throw new ArgumentNullException("action");
		}
		T[] entries = _entries;
		int[] activeList = _activeList;
		int count = _activeCount;
		for (int i = 0; i < count; i++)
		{
			if (action(entries[activeList[i]]))
			{
				await UniTask.NextFrame(timing, cancellationToken);
			}
		}
	}
}
