using System;
using Assets.Scripts.GridSystem;

namespace Assets.Scripts.Objects;

public readonly struct ConnectionRef(Connection connectedEnd) : IEquatable<ConnectionRef>
{
	public readonly long ReferenceId = connectedEnd.Parent.ReferenceId;

	public readonly ConnectionRole Role = connectedEnd.ConnectionRole;

	public readonly Grid3 LocalGrid = connectedEnd.LocalGrid;

	public readonly Grid3 FacingGrid = connectedEnd.FacingGrid;

	public const int BUFFER_SIZE = 64;

	public bool Equals(ConnectionRef other)
	{
		if (ReferenceId == other.ReferenceId && Role == other.Role && LocalGrid.Equals(other.LocalGrid))
		{
			return FacingGrid.Equals(other.FacingGrid);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ConnectionRef other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		long referenceId = ReferenceId;
		return (int)(((((uint)(referenceId.GetHashCode() * 397) ^ (uint)Role) * 397) ^ (uint)LocalGrid.GetHashCode()) * 397) ^ FacingGrid.GetHashCode();
	}

	public bool Matches(Connection connection)
	{
		if (ReferenceId == connection.Parent.ReferenceId && Role == connection.ConnectionRole && LocalGrid.Equals(connection.LocalGrid))
		{
			return FacingGrid.Equals(connection.FacingGrid);
		}
		return false;
	}
}
