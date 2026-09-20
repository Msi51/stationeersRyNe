using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Pipes;

public class ChuteDevice : SmallDevice, ISmartRotatable, IChute
{
	[Header("Smart Rotate")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public int NextTickMove;

	public Slot TransportSlot => Slots[0];

	public List<Connection> SmallGridOpenEnds => OpenEnds;

	public void SetNeighbor(SmallGrid sendingNeighbor)
	{
	}

	public override void OnNeighborPlaced(SmallGrid neighbor)
	{
		base.OnNeighborPlaced(neighbor);
		if (neighbor is Chute chute)
		{
			chute.SetDropPoint();
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild.ParentSlot == TransportSlot && TransportSlot.Interactable != null && (bool)TransportSlot.Interactable.Collider)
		{
			Vector3 size = TransportSlot.Interactable.Collider.bounds.size;
			Vector3 size2 = newChild.Bounds.size;
			float num = Mathf.Min(size.x / size2.x, size.y / size2.y, size.z / size2.z, 1f);
			newChild.ThingTransform.localScale = Vector3.one * num;
		}
	}

	protected ChuteOpenEnd InitOpenEnd(ConnectionRole connectionRole)
	{
		foreach (Connection openEnd in OpenEnds)
		{
			if (openEnd.ConnectionRole == connectionRole)
			{
				return new ChuteOpenEnd
				{
					Connection = openEnd,
					DropPosition = openEnd.Transform.position,
					DropVelocity = openEnd.Transform.position - base.ThingTransformPosition
				};
			}
		}
		return null;
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = permutation;
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}
}
