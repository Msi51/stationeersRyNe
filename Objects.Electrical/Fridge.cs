using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Objects.Electrical;

public class Fridge : DeviceInternal
{
	public bool DontUseOpen;

	private Vector3 _childRotation = new Vector3(45f, 90f, 90f);

	public override void OnAtmosphericTick()
	{
		if (!IsOperable)
		{
			return;
		}
		foreach (PipeNetwork connectedPipeNetwork in ConnectedPipeNetworks)
		{
			if (connectedPipeNetwork.Atmosphere != null)
			{
				AtmosphereHelper.Mix(connectedPipeNetwork.Atmosphere, base.InternalAtmosphere, MatterState);
			}
		}
	}

	public override void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		if ((object)newChild != null)
		{
			newChild.ScaleToSlot();
			newChild.ThingTransformLocalRotation = Quaternion.Euler(_childRotation + newChild.ChildSlotOffset);
			newChild.ThingTransformLocalPosition = newChild.ChildSlotOffsetPosition;
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		previousChild.SetVisibility(isVisible: true);
	}

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
}
