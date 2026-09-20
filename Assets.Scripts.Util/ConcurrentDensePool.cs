using System;
using System.Runtime.CompilerServices;

namespace Assets.Scripts.Util;

public class ConcurrentDensePool<T> : DensePool<T> where T : class, IDensePoolable
{
	public ConcurrentDensePool(string poolName, int capacity)
		: base(poolName, capacity)
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Add(T item)
	{
		lock (this)
		{
			return base.Add(item);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override void Remove(T item)
	{
		lock (this)
		{
			base.Remove(item);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override void RemoveAt(int slot)
	{
		lock (this)
		{
			base.RemoveAt(slot);
		}
	}

	public override void Clear()
	{
		lock (this)
		{
			base.Clear();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override ActiveEnumerable Active()
	{
		lock (this)
		{
			return base.Active();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override void Cleanup()
	{
		lock (this)
		{
			base.Cleanup();
		}
	}

	public override void RemoveWhere(Predicate<T> match)
	{
		lock (this)
		{
			base.RemoveWhere(match);
		}
	}

	public override void Populate(Span<Ref> atmosBuf, ref int atmosCount)
	{
		lock (this)
		{
			base.Populate(atmosBuf, ref atmosCount);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override void ForEach(Action<T> action)
	{
		lock (this)
		{
			base.ForEach(action);
		}
	}
}
