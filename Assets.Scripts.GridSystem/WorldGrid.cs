using System;
using System.Runtime.CompilerServices;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace Assets.Scripts.GridSystem;

public readonly struct WorldGrid : IEquatable<WorldGrid>, IComparable<WorldGrid>, IEvaluable
{
	public static readonly WorldGrid INVALID;

	public const int WORLD_EXTENTS = 81920;

	public readonly Grid3 Value;

	public const int ALL_GRID_NEIGHBOURS = 26;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public WorldGrid(int x, int y, int z)
	{
		Value = new Grid3(x, y, z);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public WorldGrid(Grid3 position)
	{
		Value = position.ToVector3().GridCenter().ToGrid();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public WorldGrid(Vector3 position)
	{
		Value = position.GridCenter().ToGrid();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public WorldGrid(Thing thing)
	{
		Value = thing.CenterPosition.GridCenter().ToGrid();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Grid3(WorldGrid worldGrid)
	{
		return worldGrid.Value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool OutOfBounds()
	{
		if (Value.x != int.MinValue && Mathf.Abs(Value.x) <= 81920 && Value.y != int.MinValue && Mathf.Abs(Value.y) <= 81920 && Value.z != int.MinValue)
		{
			return Mathf.Abs(Value.z) > 81920;
		}
		return true;
	}

	public static void PopulateAllGridNeighours(Span<WorldGrid> grid, ref int count, WorldGrid worldGrid)
	{
		Grid3 value = worldGrid.Value;
		grid[0] = new WorldGrid(value + Grid3.North);
		grid[1] = new WorldGrid(value + Grid3.East);
		grid[2] = new WorldGrid(value + Grid3.South);
		grid[3] = new WorldGrid(value + Grid3.West);
		grid[4] = new WorldGrid(value + Grid3.North + Grid3.West);
		grid[5] = new WorldGrid(value + Grid3.North + Grid3.East);
		grid[6] = new WorldGrid(value + Grid3.South + Grid3.East);
		grid[7] = new WorldGrid(value + Grid3.South + Grid3.West);
		grid[8] = new WorldGrid(value + Grid3.Up);
		grid[9] = new WorldGrid(value + Grid3.North + Grid3.Up);
		grid[10] = new WorldGrid(value + Grid3.East + Grid3.Up);
		grid[11] = new WorldGrid(value + Grid3.South + Grid3.Up);
		grid[12] = new WorldGrid(value + Grid3.West + Grid3.Up);
		grid[13] = new WorldGrid(value + Grid3.North + Grid3.West + Grid3.Up);
		grid[14] = new WorldGrid(value + Grid3.North + Grid3.East + Grid3.Up);
		grid[15] = new WorldGrid(value + Grid3.South + Grid3.East + Grid3.Up);
		grid[16] = new WorldGrid(value + Grid3.South + Grid3.West + Grid3.Up);
		grid[17] = new WorldGrid(value + Grid3.Down);
		grid[18] = new WorldGrid(value + Grid3.North + Grid3.Down);
		grid[19] = new WorldGrid(value + Grid3.East + Grid3.Down);
		grid[20] = new WorldGrid(value + Grid3.South + Grid3.Down);
		grid[21] = new WorldGrid(value + Grid3.West + Grid3.Down);
		grid[22] = new WorldGrid(value + Grid3.North + Grid3.West + Grid3.Down);
		grid[23] = new WorldGrid(value + Grid3.North + Grid3.East + Grid3.Down);
		grid[24] = new WorldGrid(value + Grid3.South + Grid3.East + Grid3.Down);
		grid[25] = new WorldGrid(value + Grid3.South + Grid3.West + Grid3.Down);
		count = 26;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(WorldGrid other)
	{
		return Value.Equals(other.Value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int CompareTo(WorldGrid other)
	{
		return Value.CompareTo(other.Value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(WorldGrid left, WorldGrid right)
	{
		return left.Equals(right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(WorldGrid left, WorldGrid right)
	{
		return !left.Equals(right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode()
	{
		return (Value.x * 73856093) ^ (Value.y * 19349663) ^ (Value.z * 83492791);
	}

	static WorldGrid()
	{
	}
}
