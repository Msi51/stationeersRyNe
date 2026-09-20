using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;

public class PanelHands : UserInterfaceBase
{
	public static PanelHands Instance;

	public List<HandInfoContainer> HandArray = new List<HandInfoContainer>();

	private int ActiveHandIndex;

	private bool _IsHiding;

	public void Awake()
	{
		if (!Instance)
		{
			Instance = this;
		}
	}

	public bool HideToggleOn(Thing thing)
	{
		if (!(thing is IDraggable) && !(thing is RoadFlare) && !(thing is ItemContainer) && !(thing is FertilizedEgg))
		{
			return thing is GasCanister;
		}
		return true;
	}

	public bool ShowSecondaryAction(Thing thing)
	{
		if (!(thing is INutrition) || thing is Flower)
		{
			if (!(thing is RoadFlare) && !(thing is ISuitReparier))
			{
				return thing is IConstructionKit;
			}
			return true;
		}
		return true;
	}

	public bool ShowPrecisionPlacement(Thing thing)
	{
		return !(thing is IDraggable);
	}

	public void SetUpHandImages(Thing newThing, bool isLeftHand)
	{
		HandInfoContainer handInfoContainer = HandArray[(!isLeftHand) ? 1 : 0];
		if (newThing != null)
		{
			handInfoContainer.HandHotKeyPanel.SetVisible(isVisble: false);
			handInfoContainer.HandPrecisionPlace.SetVisible(ShowPrecisionPlacement(newThing));
			handInfoContainer.HandThrow.SetVisible(isVisble: true);
			handInfoContainer.HandToggleOn.PrimaryImage.SetVisible(!newThing.OnOff);
			handInfoContainer.HandToggleOn.SecondaryImage.SetVisible(newThing.OnOff);
			handInfoContainer.ThingInHand = newThing as DynamicThing;
			handInfoContainer.HandToggleOn.SetVisible(handInfoContainer.ThingInHand.HasOnOffState);
			handInfoContainer.HandSecondaryAction.SetVisible(ShowSecondaryAction(newThing));
			if (HideToggleOn(newThing))
			{
				handInfoContainer.HandToggleOn.SetVisible(isVisble: false);
			}
		}
		else
		{
			handInfoContainer.ThingInHand = null;
			handInfoContainer.HandHotKeyPanel.SetVisible(isVisble: false);
			handInfoContainer.HandSecondaryAction.SetVisible(isVisble: false);
			handInfoContainer.HandPrecisionPlace.SetVisible(isVisble: false);
			handInfoContainer.HandThrow.SetVisible(isVisble: false);
			handInfoContainer.HandToggleOn.SetVisible(isVisble: false);
		}
		if ((bool)InventoryManager.Instance.ActiveHand.Slot.Occupant)
		{
			HandArray[InventoryManager.Instance.ActiveHand.SlotDisplayButton.IsLeftHand ? 1 : 0].HandSwapKeyPanel.SetVisible(isVisble: true);
			HandArray[(!InventoryManager.Instance.ActiveHand.SlotDisplayButton.IsLeftHand) ? 1 : 0].HandHotKeyPanel.SetVisible(isVisble: true);
		}
		else
		{
			HandArray[InventoryManager.Instance.ActiveHand.SlotDisplayButton.IsLeftHand ? 1 : 0].HandHotKeyPanel.SetVisible(isVisble: false);
		}
		if ((bool)newThing)
		{
			handInfoContainer.SlotHintInteractable.SetVisible(newThing.HasInteractions);
			handInfoContainer.SlotHintHasStorage.SetVisible(newThing.HasSlots);
		}
		else
		{
			handInfoContainer.SlotHintInteractable.SetVisible(isVisble: false);
			handInfoContainer.SlotHintHasStorage.SetVisible(isVisble: false);
		}
		HandleSlotInfoDisplay(handInfoContainer);
		if (InventoryManager.Instance.ActiveHand != null)
		{
			HandleSwitchHands(InventoryManager.Instance.ActiveHand.SlotDisplayButton.IsLeftHand);
		}
	}

	public void HandleSlotInfoDisplay(HandInfoContainer currentHandToModify)
	{
		if (!currentHandToModify.SlotHintHasStorage.IsVisibleSelf && !currentHandToModify.SlotHintInteractable.IsVisibleSelf)
		{
			currentHandToModify.SlotHints.SetVisible(isVisble: false);
		}
		else
		{
			currentHandToModify.SlotHints.SetVisible(isVisble: true);
		}
	}

	public void HideSlotInfo()
	{
		_IsHiding = true;
		foreach (HandInfoContainer item in HandArray)
		{
			item.HandHotKeyPanel.SetVisible(isVisble: false);
			item.SlotHints.SetVisible(isVisble: false);
			item.HandSwapKeyPanel.SetVisible(isVisble: false);
		}
	}

	public void ShowSlotInfo()
	{
		for (int i = 0; i < HandArray.Count; i++)
		{
			HandArray[i].HandHotKeyPanel.SetVisible(i == ActiveHandIndex);
			if (i == ActiveHandIndex)
			{
				HandleSlotInfoDisplay(HandArray[i]);
			}
			HandArray[i].HandSwapKeyPanel.SetVisible(i != ActiveHandIndex);
		}
		_IsHiding = false;
	}

	public void HandlePowerOnSwitch(bool isLeftHand)
	{
		HandInfoContainer handInfoContainer = HandArray[ActiveHandIndex];
		if (handInfoContainer.ThingInHand != null && handInfoContainer.ThingInHand.HasOnOffState)
		{
			handInfoContainer.HandToggleOn.PrimaryImage.SetVisible(!handInfoContainer.ThingInHand.OnOff);
			handInfoContainer.HandToggleOn.SecondaryImage.SetVisible(handInfoContainer.ThingInHand.OnOff);
		}
	}

	public void HandleSwitchHands(bool isLeftHand)
	{
		ActiveHandIndex = ((!isLeftHand) ? 1 : 0);
		HandInfoContainer handInfoContainer = HandArray[ActiveHandIndex];
		int index = ((ActiveHandIndex != 1) ? 1 : 0);
		HandArray[index].HandHotKeyPanel.SetVisible(isVisble: false);
		HandArray[InventoryManager.Instance.ActiveHand.SlotDisplayButton.IsLeftHand ? 1 : 0].HandSwapKeyPanel.SetVisible(isVisble: true);
		HandArray[(!InventoryManager.Instance.ActiveHand.SlotDisplayButton.IsLeftHand) ? 1 : 0].HandSwapKeyPanel.SetVisible(isVisble: false);
		if (!handInfoContainer.SlotHintHasStorage.IsVisibleSelf && !handInfoContainer.SlotHintInteractable.IsVisibleSelf)
		{
			handInfoContainer.SlotHints.SetVisible(isVisble: false);
		}
		else
		{
			handInfoContainer.SlotHints.SetVisible(isVisble: true);
		}
		if (!(handInfoContainer.ThingInHand == null))
		{
			HandlePowerOnSwitch(handInfoContainer);
			handInfoContainer.HandHotKeyPanel.SetVisible(isVisble: true);
		}
	}
}
