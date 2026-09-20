using System;
using Assets.Scripts.Objects;

namespace Assets.Scripts.GridSystem;

public readonly struct GridEvent(WorldGrid worldGrid, DynamicThing dynamicThing, GridEvent.GridEventType gridEventType) : IEquatable<GridEvent>
{
	public enum GridEventType
	{
		Enter,
		Leave,
		None
	}

	public readonly WorldGrid WorldGrid = worldGrid;

	public readonly DynamicThing DynamicThing = dynamicThing;

	public readonly GridEventType Type = gridEventType;

	public static readonly GridEvent Invalid = new GridEvent(WorldGrid.INVALID, null, GridEventType.None);

	public bool IsValid()
	{
		if (DynamicThing != Invalid.DynamicThing && WorldGrid != Invalid.WorldGrid)
		{
			return Type != Invalid.Type;
		}
		return false;
	}

	public bool Equals(GridEvent other)
	{
		if (DynamicThing == other.DynamicThing && WorldGrid == other.WorldGrid)
		{
			return Type == other.Type;
		}
		return false;
	}
}
