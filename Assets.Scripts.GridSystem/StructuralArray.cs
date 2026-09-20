using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.GridSystem;

public struct StructuralArray
{
	public struct Enumerator
	{
		private StructuralArray _array;

		private int _index;

		public Structure Current => Unsafe.Add(ref _array._center, _index);

		internal Enumerator(StructuralArray array)
		{
			_array = array;
			_index = -1;
		}

		public bool MoveNext()
		{
			while (++_index < 7)
			{
				if (Unsafe.Add(ref _array._center, _index) != null)
				{
					return true;
				}
			}
			return false;
		}
	}

	public const int Capacity = 7;

	private Structure _center;

	private Structure _up;

	private Structure _down;

	private Structure _west;

	private Structure _east;

	private Structure _north;

	private Structure _south;

	public Structure this[StructureElement element]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if ((uint)element >= 7u)
			{
				return null;
			}
			return Unsafe.Add(ref _center, (int)element);
		}
	}

	public Structure this[Grid3 element]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return this[Get(element)];
		}
	}

	public Structure this[Vector3 element]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return this[Get(element.ToGridPosition())];
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Set(StructureElement element, Structure structure)
	{
		if ((uint)element >= 7u)
		{
			throw new IndexOutOfRangeException();
		}
		Unsafe.Add(ref _center, (int)element) = structure;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Set(Grid3 element, Structure structure)
	{
		Set(Get(element), structure);
	}

	public bool IsEmpty(StructureElement element)
	{
		return this[element] == null;
	}

	public bool IsEmpty(Grid3 element)
	{
		return this[Get(element)] == null;
	}

	public void ClearAll()
	{
		_center = null;
		_up = null;
		_down = null;
		_west = null;
		_east = null;
		_north = null;
		_south = null;
	}

	public void Clear(Grid3 grid)
	{
		Set(Get(grid), null);
	}

	public void Clear(StructureElement element)
	{
		Set(element, null);
	}

	public bool IsValid(Grid3 grid)
	{
		return Get(grid) != StructureElement.Invalid;
	}

	public bool Remove(Structure structure)
	{
		bool result = false;
		for (int i = 0; i < 7; i++)
		{
			ref Structure reference = ref Unsafe.Add(ref _center, i);
			if ((object)reference == structure)
			{
				reference = null;
				result = true;
			}
		}
		return result;
	}

	public ReadOnlySpan<Structure> AsSpan()
	{
		return MemoryMarshal.CreateReadOnlySpan(ref _center, 7);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static StructureElement Get(Grid3 grid)
	{
		grid.Deconstruct(out var x, out var y, out var z);
		switch (x)
		{
		case 0:
			switch (y)
			{
			case 10:
				if (z != 0)
				{
					break;
				}
				return StructureElement.Up;
			case -10:
				if (z != 0)
				{
					break;
				}
				return StructureElement.Down;
			case 0:
				switch (z)
				{
				case 10:
					return StructureElement.North;
				case -10:
					return StructureElement.South;
				case 0:
					return StructureElement.Center;
				}
				break;
			}
			break;
		case 10:
			if (y != 0 || z != 0)
			{
				break;
			}
			return StructureElement.East;
		case -10:
			if (y != 0 || z != 0)
			{
				break;
			}
			return StructureElement.West;
		}
		return StructureElement.Invalid;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}
}
