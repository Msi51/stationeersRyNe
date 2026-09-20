using System;
using Assets.Scripts.GridSystem;

namespace Assets.Scripts;

public struct RoomCheck : IEquatable<RoomCheck>
{
	public Grid3 Grid;

	public RoomChangeSource Source;

	public bool Equals(RoomCheck other)
	{
		return Grid.Equals(other.Grid);
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is RoomCheck)
		{
			return Equals((RoomCheck)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Grid.GetHashCode() * 397;
	}
}
