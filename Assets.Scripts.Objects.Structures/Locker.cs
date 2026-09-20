using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public class Locker : Device, ISmartRotatable
{
	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	private Vector3 ChildRotation = new Vector3(45f, 90f, 90f);

	[SerializeField]
	private LockerAnimComponent lockerAnimComponent;

	[SerializeField]
	private ShutterMaterialAnimComponent shutterMaterialAnimComponent;

	[SerializeField]
	private Transform soundPosition;

	public bool DontUseOpen;

	public override Transform SoundPosition => soundPosition;

	private void SetContentsVisibility(bool isVisible = true)
	{
		if (DontUseOpen)
		{
			return;
		}
		foreach (Interactable interactable in Interactables)
		{
			if (interactable.Action != InteractableType.Open && (bool)interactable.Collider)
			{
				interactable.Collider.enabled = isVisible;
			}
		}
		foreach (Slot slot in Slots)
		{
			if ((bool)slot.Occupant)
			{
				slot.Occupant.SetVisibility(isVisible);
			}
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		lockerAnimComponent?.RefreshState(skipAnimation);
		shutterMaterialAnimComponent?.RefreshState(skipAnimation);
	}

	public override void OnAnimationStart()
	{
		base.OnAnimationStart();
		if (IsOpen)
		{
			SetContentsVisibility();
		}
	}

	public override void OnAnimationStop()
	{
		base.OnAnimationStop();
		if (!IsOpen)
		{
			SetContentsVisibility(isVisible: false);
		}
	}

	public override void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		if ((object)newChild != null)
		{
			newChild.ScaleToSlot();
			newChild.ThingTransformLocalRotation = Quaternion.Euler(ChildRotation + newChild.ChildSlotOffset);
			newChild.ThingTransformLocalPosition = newChild.ChildSlotOffsetPosition;
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		previousChild.SetVisibility(isVisible: true);
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
