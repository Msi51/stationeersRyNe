using System;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Inventory;

public class HumanHandsBehaviour : MonoBehaviour
{
	private static readonly int UiSwapActiveHandHash = Animator.StringToHash("UI_SwapActiveHand");

	public static Action SwapHandsEvent;

	[SerializeField]
	private Human _human;

	private static InventoryManager.Mode CurrentMode => InventoryManager.CurrentMode;

	private static SlotDisplay ActiveHand
	{
		get
		{
			return InventoryManager.Instance.ActiveHand;
		}
		set
		{
			InventoryManager.Instance.ActiveHand = value;
		}
	}

	private static SlotDisplay InactiveHand
	{
		get
		{
			return InventoryManager.Instance.InactiveHand;
		}
		set
		{
			InventoryManager.Instance.InactiveHand = value;
		}
	}

	public void _SwapHandsOnKeyUp()
	{
		if (!InventoryManager.Instance.IsUsingSmartTool && !_human.IsUnresponsive && !_human.IsSleeping)
		{
			SwapHands();
		}
	}

	public void SwapHands()
	{
		if (CurrentMode == InventoryManager.Mode.PrecisionPlacement)
		{
			CancelPlacement();
		}
		CheckCancelMultiConstructor();
		SwapHandsEvent?.Invoke();
		SlotDisplay inactiveHand = InactiveHand;
		SlotDisplay activeHand = ActiveHand;
		ActiveHand = inactiveHand;
		InactiveHand = activeHand;
		PanelHands.Instance.HandleSwitchHands(ActiveHand.SlotDisplayButton.IsLeftHand);
		RefreshDisplaySlotBindings();
		AnimateActiveHands();
		if (CurrentMode == InventoryManager.Mode.Placement && ActiveHand.Slot.Occupant is Constructor constructor)
		{
			UpdatePlacement(constructor);
		}
		if (ActiveHand.Slot.Occupant is Pickaxe || ActiveHand.Slot.Occupant is MiningDrill)
		{
			CursorManager instance = CursorManager.Instance;
			instance.CursorHitMask = (int)instance.CursorHitMask | (int)LayerMasks.CursorVoxel;
		}
		else
		{
			CursorManager instance2 = CursorManager.Instance;
			instance2.CursorHitMask = (int)instance2.CursorHitMask & ~(int)LayerMasks.CursorVoxel;
		}
		if (ActiveHand?.Slot?.Occupant is Tablet tablet)
		{
			tablet.InActiveHand();
		}
		if (ActiveHand?.Slot?.Occupant is OreDetector oreDetector)
		{
			oreDetector.InActiveHand();
		}
		OnActiveEvent();
		UIAudioManager.Play(UiSwapActiveHandHash);
		if (ActiveHand?.Slot?.Occupant != null)
		{
			_human.PlayEquipSound(ActiveHand.Slot.Occupant);
		}
		if (InactiveHand?.Slot.Occupant != null)
		{
			_human.PlayUnEquipSound(InactiveHand.Slot.Occupant);
		}
	}

	public void ToggleActiveHandTool()
	{
		if (!_human.IsUnresponsive && !_human.IsSleeping && !InputMouse.IsMouseControl && !InputWindowBase.IsInputWindow)
		{
			Slot slot = ActiveHand.Slot;
			if ((bool)slot.Occupant && slot.Occupant.CheckTogglePower() && slot.Occupant.ShouldToggleOn())
			{
				int state = ((!slot.Occupant.OnOff) ? 1 : 0);
				slot.Occupant.Interact(InteractableType.OnOff, state);
				PanelHands.Instance.HandlePowerOnSwitch(slot.Display.SlotDisplayButton.IsLeftHand);
			}
		}
	}

	private static void CancelPlacement()
	{
		InventoryManager.Instance.CancelPlacement();
	}

	private static void CheckCancelMultiConstructor()
	{
		InventoryManager.Instance.CheckCancelMultiConstructor();
	}

	private static void RefreshDisplaySlotBindings()
	{
		InventoryManager.RefreshDisplaySlotBindings();
	}

	private static void AnimateActiveHands()
	{
		InventoryManager.AnimateActiveHands();
	}

	private static void UpdatePlacement(Constructor constructor)
	{
		InventoryManager.UpdatePlacement(constructor);
	}

	private static void OnActiveEvent()
	{
		InventoryManager.OnActiveEvent();
	}
}
