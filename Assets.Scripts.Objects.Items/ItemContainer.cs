using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class ItemContainer : Item
{
	[Header("Item Container")]
	public CrateType CrateContents;

	private List<Item> _buildingSupplies;

	[SerializeField]
	private ContainerAnimComponent openAnimComponent;

	public Thing GetSlotThing(int slot)
	{
		return Slots[slot].Occupant;
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		openAnimComponent?.RefreshState(skipAnimation);
	}

	public void InitContainer()
	{
		switch (CrateContents)
		{
		case CrateType.Empty:
			return;
		case CrateType.Eggs:
			_buildingSupplies = new List<Item>
			{
				Prefab.Find<Egg>("ItemEgg"),
				Prefab.Find<Egg>("ItemEgg"),
				Prefab.Find<Egg>("ItemEgg"),
				Prefab.Find<Egg>("ItemEgg"),
				Prefab.Find<Egg>("ItemEgg"),
				Prefab.Find<Egg>("ItemEgg")
			};
			break;
		case CrateType.Burger:
			_buildingSupplies = new List<Item> { Prefab.Find<Burger>("ItemBurger") };
			break;
		}
		for (int i = 0; i < Slots.Count; i++)
		{
			if (i < _buildingSupplies.Count)
			{
				DynamicThing dynamicThing = OnServer.CreateOld(_buildingSupplies[i], Slots[i]);
				dynamicThing.RigidBody.useGravity = false;
				Stackable stackable = dynamicThing as Stackable;
				if ((bool)stackable)
				{
					stackable.SetQuantity(stackable.MaxQuantity);
				}
			}
		}
	}

	public override void OnAnimationStart()
	{
		base.OnAnimationStart();
		if (IsOpen)
		{
			SetContentsVisibility(isVisible: true);
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

	public override void OnFinishedInteractionSync(Interactable interactable)
	{
		base.OnFinishedInteractionSync(interactable);
	}

	public void SetContentsVisibility(bool isVisible)
	{
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
}
