using System;

namespace Assets.Scripts.GridSystem;

public sealed class Grid3Buffer
{
	private readonly Grid3[] _items;

	public int Count { get; private set; }

	public int Capacity => _items.Length;

	public Grid3 this[int index] => _items[index];

	public Grid3Buffer(int capacity = 6)
	{
		_items = new Grid3[capacity];
	}

	public Span<Grid3> AsSpan()
	{
		return _items.AsSpan(0, Count);
	}

	public static implicit operator Span<Grid3>(Grid3Buffer buffer)
	{
		return buffer.AsSpan();
	}

	public static implicit operator ReadOnlySpan<Grid3>(Grid3Buffer buffer)
	{
		return buffer.AsSpan();
	}

	public Span<Grid3>.Enumerator GetEnumerator()
	{
		return AsSpan().GetEnumerator();
	}

	public void CopyFrom(ReadOnlySpan<Grid3> source)
	{
		source.CopyTo(_items);
		Count = source.Length;
	}

	public bool Contains(Grid3 grid)
	{
		for (int i = 0; i < Count; i++)
		{
			if (_items[i] == grid)
			{
				return true;
			}
		}
		return false;
	}
}
