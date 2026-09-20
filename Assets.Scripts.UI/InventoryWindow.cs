using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class InventoryWindow : DraggableWindow
{
	public static InventoryWindow CurrentWindow;

	public SlotDisplayButton ButtonPrefab;

	public SlotDisplayButton InteractionPrefab;

	public WindowLayoutGroup SlotGroup;

	public WindowLayoutGroup InteractionGroup;

	public Slot ParentSlot;

	public List<SlotDisplayButton> DisplayedSlots = new List<SlotDisplayButton>();

	public List<SlotDisplayButton> DisplayedInteractions = new List<SlotDisplayButton>();

	public UserInterfaceAnimated ButtonDock;

	public UserInterfaceAnimated ButtonSort;

	private List<InventoryWindow> _childWindows = new List<InventoryWindow>();

	[SerializeField]
	private Canvas canvas;

	[SerializeField]
	private LayoutGroup layoutGroup;

	[SerializeField]
	private RectTransform rectTransform;

	private Thing _previousParent;

	public static InventoryWindow LastOpenedWindow;

	private readonly int _uiInventoryPanelOpenHash = Animator.StringToHash("SFX_UI_InventoryPanelOpen");

	private readonly int _uiInventoryPanelCloseHash = Animator.StringToHash("SFX_UI_InventoryPanelClose");

	private readonly int _uiAddToInventoryHash = Animator.StringToHash("SFX_UI_AddToInventory");

	public override bool IsVisible
	{
		get
		{
			if (canvas != null)
			{
				return canvas.enabled;
			}
			return false;
		}
	}

	public DynamicThing Parent => ParentSlot.Occupant;

	public SlotDisplayButton ParentDisplayButton => ParentSlot.Display.SlotDisplayButton;

	public bool IsUndocked => RectTransform?.parent != InventoryWindowManager.Instance.RectTransform;

	public void Assign(Slot parentSlot)
	{
		if ((bool)Parent)
		{
			Unassign();
		}
		base.name = "Window" + parentSlot.StringKey;
		ParentSlot = parentSlot;
		ParentDisplayButton.SlotWindow = this;
		HandleOccupantChange();
		parentSlot.OnOccupantChange += HandleOccupantChange;
		ParentDisplayButton.OnPrimaryAction += ToggleVisibility;
		ParentDisplayButton.OnPrimarySmartStow += InventoryManager.SmartStow;
		SetVisible(isVisble: false);
		ButtonDock.SetVisible(isVisble: false);
		ButtonSort.SetVisible(IsSortable(parentSlot));
		if (ParentSlot.IsHandSlot)
		{
			InventoryManager.OnActiveHandChanged += UpdateTitle;
		}
		KeyManager.OnControlsChanged += RefreshLanguageOrControls;
		Localization.OnLanguageChanged = (Action)Delegate.Combine(Localization.OnLanguageChanged, new Action(RefreshLanguageOrControls));
		parentSlot.Parent.OnDestroyed += Destroy;
	}

	private bool IsSortable(Slot parentSlot)
	{
		DynamicThing dynamicThing = parentSlot.Get<DynamicThing>();
		if (dynamicThing == null)
		{
			return false;
		}
		if (!dynamicThing.HasAnySlots)
		{
			return false;
		}
		int num = 0;
		foreach (Slot slot in dynamicThing.Slots)
		{
			if (slot.IsSortable)
			{
				num++;
			}
			if (num > 1)
			{
				return true;
			}
		}
		return false;
	}

	public void Destroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			UnityEngine.Object.Destroy(GameObject);
		}
	}

	public void Unassign()
	{
		ParentSlot.OnOccupantChange -= HandleOccupantChange;
		ParentDisplayButton.OnPrimaryAction -= ToggleVisibility;
		ParentDisplayButton.OnPrimarySmartStow -= InventoryManager.SmartStow;
		if (ParentDisplayButton.SlotWindow == this)
		{
			ParentDisplayButton.SlotWindow = null;
		}
		ClearInteractions();
		ClearSlots();
		ClearChildWindows();
		KeyManager.OnControlsChanged -= RefreshLanguageOrControls;
		Localization.OnLanguageChanged = (Action)Delegate.Remove(Localization.OnLanguageChanged, new Action(RefreshLanguageOrControls));
		KeyManager.OnControlsChanged -= UpdateTitle;
		Localization.OnLanguageChanged = (Action)Delegate.Remove(Localization.OnLanguageChanged, new Action(UpdateTitle));
		InventoryManager.OnActiveHandChanged -= UpdateTitle;
	}

	public bool RegisterChildWindow(InventoryWindow window)
	{
		if (window == null)
		{
			return false;
		}
		_childWindows.Add(window);
		InventoryWindowManager.Instance.Windows.Add(window);
		return true;
	}

	public override void ToggleVisibility()
	{
		if (DisplayedSlots.Count != 0 || DisplayedInteractions.Count != 0)
		{
			base.ToggleVisibility();
		}
	}

	public override void SetActive(bool active)
	{
		if ((bool)canvas)
		{
			canvas.enabled = active;
		}
	}

	public void OnDestroy()
	{
		if (InventoryWindowManager.Instance != null)
		{
			InventoryWindowManager.Instance.Windows.Remove(this);
		}
		Unassign();
	}

	private void ClearSlots()
	{
		int count = DisplayedSlots.Count;
		while (count-- > 0)
		{
			SlotDisplayButton slotDisplayButton = DisplayedSlots[count];
			InventoryWindowManager.Deregister(slotDisplayButton);
			UnityEngine.Object.Destroy(slotDisplayButton.GameObject);
		}
		DisplayedSlots.Clear();
	}

	private void ClearInteractions()
	{
		int count = DisplayedInteractions.Count;
		while (count-- > 0)
		{
			SlotDisplayButton slotDisplayButton = DisplayedInteractions[count];
			InventoryWindowManager.Deregister(slotDisplayButton);
			UnityEngine.Object.Destroy(slotDisplayButton.GameObject);
		}
		DisplayedInteractions.Clear();
	}

	private void ClearChildWindows()
	{
		_childWindows.ForEach(delegate(InventoryWindow w)
		{
			w.Destroy();
		});
		_childWindows.Clear();
	}

	private void SetInteractions()
	{
		if ((bool)_previousParent)
		{
			_previousParent.OnInteractable -= RefreshInteractables;
		}
		int num = 0;
		foreach (Interactable interactable in Parent.Interactables)
		{
			if (interactable.CanKeyInteract && interactable.Slot == null)
			{
				SlotDisplayButton slotDisplayButton = UnityEngine.Object.Instantiate(InteractionPrefab, InteractionGroup.transform, worldPositionStays: false);
				SlotDisplay slotDisplay = new SlotDisplay(slotDisplayButton);
				slotDisplayButton.name = "Interaction" + interactable.Action;
				interactable.Display = slotDisplay;
				if ((bool)slotDisplay.SlotText)
				{
					slotDisplay.SlotText.text = interactable.DisplayName;
				}
				slotDisplayButton.Interactable = interactable;
				slotDisplayButton.ParentWindow = this;
				slotDisplayButton.OnPrimaryAction += interactable.PlayerInteractWith;
				slotDisplayButton.OnPrimarySmartStow += InventoryManager.SmartStow;
				DisplayedInteractions.Add(slotDisplayButton);
				num++;
				interactable.UpdateDisplay();
				InventoryWindowManager.Register(slotDisplayButton);
			}
		}
		InteractionGroup.Group.constraintCount = Mathf.CeilToInt(Mathf.Max((float)SlotGroup.Group.constraintCount / 2f, 2f));
		InteractionGroup.SetVisible(num > 0);
		ButtonSort.SetVisible(IsSortable(ParentSlot));
	}

	private void Update()
	{
		if (!WorldManager.IsGamePaused && IsVisible)
		{
			RefreshInteractables();
		}
	}

	public void RefreshInteractables()
	{
		foreach (SlotDisplayButton displayedInteraction in DisplayedInteractions)
		{
			displayedInteraction.Interactable.UpdateDisplay();
		}
	}

	public void RefreshSlotDisplays()
	{
		foreach (SlotDisplayButton displayedSlot in DisplayedSlots)
		{
			if ((bool)displayedSlot.Slot.Occupant)
			{
				displayedSlot.Slot.Occupant.OnDisplayInPlayerWindow();
			}
		}
	}

	public void RefreshSlots()
	{
		foreach (SlotDisplayButton displayedSlot in DisplayedSlots)
		{
			if ((bool)displayedSlot.SlotDisplay.SlotText)
			{
				displayedSlot.SlotDisplay.SlotText.text = displayedSlot.Slot.DisplayName;
			}
		}
		ButtonSort.SetVisible(IsSortable(ParentSlot));
	}

	private void SetSlots()
	{
		int num = 0;
		foreach (Slot slot in Parent.Slots)
		{
			if (slot.IsInteractable)
			{
				SlotDisplayButton slotDisplayButton = UnityEngine.Object.Instantiate(ButtonPrefab, SlotGroup.transform, worldPositionStays: false);
				slotDisplayButton.name = "Slot" + slot.Action;
				SlotDisplay slotDisplay = (slot.Display = new SlotDisplay(slotDisplayButton, slot));
				if ((bool)slotDisplay.SlotText)
				{
					slotDisplay.SlotText.text = ((slot.Type == Slot.Class.None) ? string.Empty : slot.DisplayName);
				}
				if ((bool)slotDisplay.QuantityText)
				{
					slotDisplay.QuantityText.text = string.Empty;
				}
				slotDisplayButton.ParentWindow = this;
				slotDisplay.RectTransform.localScale = Vector3.one;
				slot.RefreshSlotDisplay();
				DisplayedSlots.Add(slotDisplayButton);
				num++;
				InventoryWindowManager.Register(slotDisplayButton);
			}
		}
		if (num == 4 || num == 8 || num == 12 || num == 16 || num == 18 || num == 28 || num == 24 || num == 32)
		{
			SlotGroup.Group.constraintCount = 4;
		}
		else if (num < 4)
		{
			SlotGroup.Group.constraintCount = num;
		}
		else if (num == 10 || num == 15 || num == 20)
		{
			SlotGroup.Group.constraintCount = 5;
		}
		else
		{
			SlotGroup.Group.constraintCount = 3;
		}
		SlotGroup.SetVisible(num > 0);
	}

	public void HandleOccupantChange()
	{
		ClearSlots();
		ClearInteractions();
		ClearChildWindows();
		UpdateTitle();
		if ((bool)PanelHands.Instance && ParentDisplayButton != null && ParentDisplayButton.IsHandSlot)
		{
			PanelHands.Instance.SetUpHandImages(ParentSlot.Occupant, ParentDisplayButton.IsLeftHand);
		}
		if (!Parent)
		{
			SetVisible(isVisble: false);
			InventoryWindowManager.RefreshSize();
			return;
		}
		SetSlots();
		SetInteractions();
		ButtonSort.SetVisible(IsSortable(ParentSlot));
		InventoryWindowManager.RefreshSize();
	}

	public void UpdateTitle()
	{
		if (ParentSlot.Display.SwapButton == string.Empty || (ParentSlot.IsHandSlot && InventoryWindowManager.ActiveHand != ParentSlot))
		{
			WindowTitleBar.SetTitle(string.Format("<color=#C8C8C8>{0} ></color> {1}", ParentSlot.GetSafeName(), ParentSlot.Occupant ? ParentSlot.Occupant.DisplayName : "Empty"));
		}
		else if (ParentSlot.Display.SwapButton == null)
		{
			if ((object)ParentSlot.Occupant != null)
			{
				WindowTitleBar.SetTitle(string.Format("<color=#C8C8C8>{0} ></color> {1}", ParentSlot.Parent.DisplayName, ParentSlot.Occupant ? ParentSlot.Occupant.DisplayName : "Empty"));
			}
		}
		else
		{
			string keyName = Localization.GetKeyName(KeyManager.GetKey(ParentSlot.Display.SwapButton));
			WindowTitleBar.SetTitle(string.Format("{2} <color=#C8C8C8>{0} ></color> {1}", ParentSlot.GetSafeName(), ParentSlot.Occupant ? ParentSlot.Occupant.DisplayName : "Empty", keyName));
		}
	}

	public void RefreshLanguageOrControls()
	{
		UpdateTitle();
		RefreshInteractables();
		RefreshSlots();
	}

	public override void SetVisible(bool isVisble)
	{
		if (!IsVisible && isVisble)
		{
			if (InventoryWindowManager._inventoryFinishedLoading)
			{
				UIAudioManager.Play(_uiInventoryPanelOpenHash);
			}
		}
		else if (IsVisible && !isVisble && InventoryWindowManager._inventoryFinishedLoading)
		{
			UIAudioManager.Play(_uiInventoryPanelCloseHash);
		}
		if ((bool)canvas)
		{
			canvas.enabled = isVisble;
		}
		if ((bool)layoutGroup)
		{
			layoutGroup.enabled = isVisble;
		}
		base.SetVisible(isVisble);
		if (GameManager.GameState != GameState.Running)
		{
			return;
		}
		InventoryWindowManager.RefreshSize();
		if (ParentSlot != null)
		{
			ParentSlot.ParentWindowVisibilityChange();
		}
		if (LastOpenedWindow != null)
		{
			if (this == LastOpenedWindow && !isVisble)
			{
				LastOpenedWindow = null;
			}
			else if (LastOpenedWindow != this && isVisble && Settings.CurrentData.LegacyInventory)
			{
				LastOpenedWindow.SetVisible(isVisble: false);
			}
		}
		if (isVisble)
		{
			LastOpenedWindow = this;
		}
		else
		{
			CurrentWindow = null;
		}
		ParentDisplayButton.Open = isVisble;
		foreach (SlotDisplayButton displayedSlot in DisplayedSlots)
		{
			displayedSlot.RefreshAnimation();
		}
		if (isVisble)
		{
			InventoryWindowManager.Instance.RegisterVisibleSlots(this);
		}
		else
		{
			InventoryWindowManager.Instance.UnregisterVisibleSlots(this);
		}
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		base.OnPointerEnter(eventData);
		CurrentWindow = this;
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		base.OnPointerExit(eventData);
		CurrentWindow = null;
	}

	public override void OnBeginDrag(PointerEventData eventData)
	{
		base.OnBeginDrag(eventData);
		Undocked();
	}

	public override void OnEndDrag(PointerEventData eventData)
	{
		InventoryWindowManager.Instance.OrderVisibleSlots();
		base.OnEndDrag(eventData);
		if (!InventoryWindowManager.Instance.UndockedWindows.Contains(this))
		{
			InventoryWindowManager.Instance.UndockedWindows.Add(this);
			if (InventoryWindowManager._inventoryFinishedLoading)
			{
				UIAudioManager.Play(_uiAddToInventoryHash);
			}
		}
	}

	public void Undocked()
	{
		ButtonDock.SetVisible(isVisble: true);
		InventoryWindowManager.Instance.UndockedWindows.Add(this);
		Transform.SetParent(InventoryWindowManager.Instance.RectTransform.parent, worldPositionStays: false);
	}

	public void ButtonDockClicked()
	{
		ButtonDock.SetVisible(isVisble: false);
		InventoryWindowManager.Instance.UndockedWindows.Remove(this);
		RectTransform.SetParent(InventoryWindowManager.Instance.RectTransform, worldPositionStays: false);
		InventoryWindowManager.Instance.ReorderWindows();
	}

	public void ButtonSortClicked()
	{
		ParentSlot.SortContents();
	}
}
