using UnityEngine;

namespace Assets.Scripts.Objects;

public struct Attack
{
	public Slot ActiveHand { get; }

	public DynamicThing SourceItem { get; }

	public Slot OtherHand { get; }

	public Vector3 Position { get; }

	public Thing DestinationThing { get; }

	public float CompletedRatio { get; }

	public Collider TargetCollider { get; }

	public bool IsDestroy { get; }

	public bool IsCopy { get; }

	public Attack(Slot activeHand, Slot otherHand, Vector3 position, Thing destinationThing, float completedRatio, Collider targetCollider, bool isDestroy, bool isCopy)
	{
		ActiveHand = activeHand;
		SourceItem = activeHand.Occupant;
		OtherHand = otherHand;
		Position = position;
		DestinationThing = destinationThing;
		CompletedRatio = completedRatio;
		TargetCollider = targetCollider;
		IsDestroy = isDestroy;
		IsCopy = isCopy;
	}

	public DynamicThing OtherHandOccupant()
	{
		return OtherHand?.Get();
	}
}
