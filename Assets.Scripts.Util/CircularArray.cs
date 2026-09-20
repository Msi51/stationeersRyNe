using System.Collections;
using System.Collections.Generic;

namespace Assets.Scripts.Util;

public class CircularArray<T> : IEnumerable<T>, IEnumerable
{
	private readonly T[] _InternalArray;

	public readonly int Count;

	private int _FirstIndex;

	public T this[int index]
	{
		get
		{
			index += _FirstIndex;
			index %= Count;
			if (index < 0)
			{
				index += Count;
			}
			return _InternalArray[index];
		}
		set
		{
			index += _FirstIndex;
			index %= Count;
			if (index < 0)
			{
				index += Count;
			}
			_InternalArray[index] = value;
		}
	}

	public CircularArray(int size)
	{
		Count = size;
		_InternalArray = new T[size];
	}

	public void Clear()
	{
		for (int i = 0; i < Count; i++)
		{
			_InternalArray[i] = default(T);
		}
	}

	public void AddToEnd(T value)
	{
		_InternalArray[_FirstIndex] = value;
		_FirstIndex++;
		if (_FirstIndex >= Count)
		{
			_FirstIndex -= Count;
		}
	}

	public IEnumerator<T> GetEnumerator()
	{
		for (int i = 0; i < Count; i++)
		{
			yield return this[i];
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
