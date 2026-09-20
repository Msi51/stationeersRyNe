using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Vehicles;

public class Vehicle : WheeledBase
{
	public Transform CameraPointDriver;

	public Transform CameraPointPassenger;

	private Transform _cameraRig;

	public int DriverSlotIndex;

	public int PassengerSlotIndex = 1;

	private Vector3 _previousCameraPosition;

	public Slot DriverSlot => Slots[DriverSlotIndex];

	public Slot PassengerSlot => Slots[PassengerSlotIndex];

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		foreach (Slot slot in newChild.Slots)
		{
			if ((bool)slot.Occupant && slot.IsHiddenInSeat)
			{
				slot.Occupant.SetVisibility(isVisible: false, hideOnPlayer: true, isRecursive: true);
			}
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		foreach (Slot slot in previousChild.Slots)
		{
			if ((bool)slot.Occupant && slot.IsHiddenInSeat)
			{
				slot.Occupant.SetVisibility(isVisible: true, hideOnPlayer: false, isRecursive: true);
			}
		}
	}
}
