using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public class Shelf : SmallGrid, ISmartRotatable
{
	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	[SerializeField]
	private Vector3 ChildRotation = new Vector3(45f, 90f, 90f);

	[SerializeField]
	private bool ScaleToSlot = true;

	public override void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		if ((object)newChild != null)
		{
			if (ScaleToSlot)
			{
				newChild.ScaleToSlot();
			}
			newChild.ThingTransformLocalRotation = Quaternion.Euler(ChildRotation + newChild.ChildSlotOffset);
			newChild.ThingTransformLocalPosition = newChild.ChildSlotOffsetPosition;
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		previousChild.SetVisibility(isVisible: true);
	}

	public override void OnDestroy()
	{
		foreach (Slot slot in Slots)
		{
			if ((bool)slot.Occupant && GameManager.RunSimulation)
			{
				OnServer.MoveToWorld(slot.Occupant);
			}
		}
		base.OnDestroy();
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
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
