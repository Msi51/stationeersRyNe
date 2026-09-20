using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Networking.Servers;
using Objects.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class InventoryWindowManager : UserInterfaceBase
{
	public static InventoryWindowManager Instance;

	private static List<SlotDisplayButton> AllButtons = new List<SlotDisplayButton>();

	public static SlotDisplayButton CurrentScollButton;

	public static bool _inventoryFinishedLoading = true;

	private static List<SlotDisplayButton> _visibleSlots = new List<SlotDisplayButton>();

	private static int _currentSlotIndex;

	public VerticalLayoutGroup WindowGrid;

	public InventoryWindow WindowPrefab;

	public UserInterfaceAnimated CloseAllButton;

	public List<InventoryWindow> UndockedWindows = new List<InventoryWindow>();

	public RectTransform CanvasRectTransform;

	private Entity _parent;

	public List<InventoryWindow> Windows = new List<InventoryWindow>();

	private static int CurrentSlotIndex
	{
		get
		{
			return _currentSlotIndex;
		}
		set
		{
			if (value >= _visibleSlots.Count)
			{
				_currentSlotIndex = 0;
			}
			else if (value < 0)
			{
				_currentSlotIndex = _visibleSlots.Count - 1;
			}
			else
			{
				_currentSlotIndex = value;
			}
		}
	}

	private static Slot CurrentScrollSlot
	{
		get
		{
			if (!(CurrentScollButton != null))
			{
				return null;
			}
			return CurrentScollButton.Slot;
		}
	}

	public static int CurrentButtonIndex => AllButtons.FindIndex((SlotDisplayButton b) => b == CurrentScollButton);

	public Entity Parent
	{
		get
		{
			return _parent;
		}
		private set
		{
			if (!(_parent == value))
			{
				if ((bool)_parent)
				{
					Destroy();
				}
				_parent = value;
			}
		}
	}

	public static UserInterfaceSaveData WorldXmlUISaveData { get; set; }

	public static Slot ActiveHand => InventoryManager.ActiveHandSlot;

	private static DynamicThing ActiveHandOccupant => ActiveHand.Occupant;

	private static DynamicThing SelectedOccupant
	{
		get
		{
			if (!(CurrentScollButton != null))
			{
				return null;
			}
			return CurrentScollButton.Slot.Occupant;
		}
	}

	public static event Event OnUIClose;

	public void RegisterVisibleSlots(InventoryWindow inventoryWindow)
	{
		_visibleSlots.AddRange(inventoryWindow.DisplayedInteractions);
		_visibleSlots.AddRange(inventoryWindow.DisplayedSlots);
		OrderVisibleSlots();
	}

	public void UnregisterVisibleSlots(InventoryWindow inventoryWindow)
	{
		foreach (SlotDisplayButton displayedInteraction in inventoryWindow.DisplayedInteractions)
		{
			_visibleSlots.Remove(displayedInteraction);
		}
		foreach (SlotDisplayButton displayedSlot in inventoryWindow.DisplayedSlots)
		{
			_visibleSlots.Remove(displayedSlot);
		}
		OrderVisibleSlots();
	}

	public void OrderVisibleSlots()
	{
		_visibleSlots = (from s in _visibleSlots
			orderby s.ParentWindow.transform.position.y descending, s.ParentWindow.transform.position.x, s.transform.position.y descending, s.transform.position.x
			select s).ToList();
	}

	public static void RefreshScrolledButton()
	{
		if ((bool)CurrentScollButton)
		{
			CurrentScollButton.RefreshAnimation();
		}
	}

	public static void Register(SlotDisplayButton button)
	{
		AllButtons.Add(button);
		if (CurrentScollButton == null)
		{
			CurrentScollButton = button;
		}
		AllButtons.Sort((SlotDisplayButton x, SlotDisplayButton y) => x.CompareTo(y));
	}

	public static void Deregister(SlotDisplayButton button)
	{
		_visibleSlots.Remove(button);
		AllButtons.Remove(button);
		if (CurrentScollButton == null)
		{
			CurrentScollButton = button;
		}
	}

	public static void NextButton()
	{
		if (!InputMouse.IsMouseControl)
		{
			CurrentSlotIndex--;
			SwapCurrentSlot();
		}
	}

	public static void PreviousButton()
	{
		if (!InputMouse.IsMouseControl)
		{
			CurrentSlotIndex++;
			SwapCurrentSlot();
		}
	}

	private static void SwapCurrentSlot()
	{
		if (_visibleSlots.Any())
		{
			SlotDisplayButton currentScollButton = CurrentScollButton;
			CurrentScollButton = _visibleSlots[CurrentSlotIndex];
			CurrentScollButton.RefreshAnimation();
			if ((bool)currentScollButton)
			{
				currentScollButton.RefreshAnimation();
			}
		}
	}

	public void Initialize()
	{
		Instance = this;
	}

	public static void ReShowCloseAllButton()
	{
		if (Instance.Windows.FindIndex((InventoryWindow w) => w.IsVisible) >= 0)
		{
			Instance.CloseAllButton.Animator.SetTrigger("Normal");
			Instance.CloseAllButton.SetVisible(isVisble: true);
			Instance.CloseAllButton.SetIsShown(isShown: true);
		}
	}

	public static void RefreshSize()
	{
		Instance.WindowGrid.childControlWidth = true;
		Instance.WindowGrid.childControlWidth = false;
		bool flag = Instance.Windows.FindIndex((InventoryWindow w) => w.IsVisible) >= 0;
		if (Instance.CloseAllButton.IsVisible != flag)
		{
			if (flag)
			{
				Instance.CloseAllButton.Animator.SetTrigger("Normal");
			}
			Instance.CloseAllButton.SetVisible(flag);
			Instance.CloseAllButton.SetIsShown(flag);
		}
	}

	public static void LoadUserInterfaceData(UserInterfaceSaveData userInterfaceSaveData)
	{
		if (userInterfaceSaveData == null)
		{
			return;
		}
		foreach (WindowSaveData openSlot in userInterfaceSaveData.OpenSlots)
		{
			InventoryWindow inventoryWindow = Instance.Windows.Find((InventoryWindow w) => w.ParentSlot.StringHash == openSlot.StringHash);
			if (inventoryWindow != null)
			{
				inventoryWindow.SetVisible(openSlot.IsOpen);
				if (openSlot.IsUndocked)
				{
					LayoutRebuilder.ForceRebuildLayoutImmediate(inventoryWindow.RectTransform);
					inventoryWindow.Undocked();
					inventoryWindow.RectTransform.position = openSlot.Position;
					inventoryWindow.ClampToScreen();
				}
			}
		}
		SlotDisplayButton currentScollButton = CurrentScollButton;
		if (AllButtons.Count > 0)
		{
			if (userInterfaceSaveData.SelectedButton >= AllButtons.Count || userInterfaceSaveData.SelectedButton < 0)
			{
				userInterfaceSaveData.SelectedButton = 0;
			}
			CurrentScollButton = AllButtons[userInterfaceSaveData.SelectedButton];
			CurrentScollButton.RefreshAnimation(playSound: false);
		}
		if ((bool)currentScollButton)
		{
			currentScollButton.RefreshAnimation(playSound: false);
		}
		if (ActiveHand.SlotIndex == userInterfaceSaveData.ActiveHandSlot)
		{
			if (ActiveHand.Occupant is Tablet tablet)
			{
				tablet.InActiveHand();
			}
			if (ActiveHand.Occupant is OreDetector oreDetector)
			{
				oreDetector.InActiveHand();
			}
			InventoryManager.OnActiveEvent();
		}
		if (ActiveHand.SlotIndex != userInterfaceSaveData.ActiveHandSlot)
		{
			Human.LocalHuman.SwapHands();
		}
		WorldXmlUISaveData = null;
	}

	public void ReorderWindows()
	{
		foreach (InventoryWindow window in Windows)
		{
			if (!window.IsUndocked)
			{
				window.Transform.SetAsLastSibling();
			}
		}
	}

	public void ToggleWindows(bool show)
	{
		foreach (InventoryWindow window in Windows)
		{
			if (!(window == null))
			{
				window.GameObject.SetActive(show);
			}
		}
	}

	public void AssignParent(Entity parent)
	{
		if (Parent == parent)
		{
			return;
		}
		Parent = parent;
		Parent.OnDestroyed += Destroy;
		int count = Windows.Count;
		while (count-- > 0)
		{
			InventoryWindow inventoryWindow = Windows[count];
			inventoryWindow.ParentSlot?.Display?.ClearLocalWindow();
			UnityEngine.Object.Destroy(inventoryWindow.GameObject);
		}
		Windows.Clear();
		_inventoryFinishedLoading = false;
		foreach (Slot slot in Parent.Slots)
		{
			if (slot.Display != null)
			{
				slot.Display.InitialiseRoot();
			}
		}
		_inventoryFinishedLoading = true;
		LoadUserInterfaceData(PlayerCookie.Current?.GetWorldPrefsInterfaceData() ?? WorldXmlUISaveData);
	}

	private void Destroy()
	{
		int count = Windows.Count;
		while (count-- > 0)
		{
			InventoryWindow inventoryWindow = Windows[count];
			inventoryWindow.ParentSlot?.Display?.ClearLocalWindow();
			UnityEngine.Object.Destroy(inventoryWindow.GameObject);
		}
		Windows.Clear();
		Parent.OnDestroyed -= Destroy;
	}

	private static KeyResult IsValid()
	{
		if (InventoryManager.Instance.IsUsingSmartTool)
		{
			return KeyResult.Invalid;
		}
		if (CurrentScollButton == null || CurrentScollButton.Slot == null)
		{
			return KeyResult.Invalid;
		}
		if ((bool)ActiveHandOccupant && (bool)SelectedOccupant)
		{
			if (Slot.CanMerge(CurrentScrollSlot.Occupant, ActiveHand))
			{
				return KeyResult.Merge;
			}
			if (!Slot.AllowSwap(CurrentScrollSlot, ActiveHand))
			{
				return KeyResult.Invalid;
			}
			return KeyResult.Swap;
		}
		if (!ActiveHandOccupant && (bool)SelectedOccupant)
		{
			if (!Slot.AllowSwap(CurrentScrollSlot, ActiveHand))
			{
				return KeyResult.Invalid;
			}
			return KeyResult.SlotToHand;
		}
		if ((bool)ActiveHandOccupant && !SelectedOccupant)
		{
			if (!Slot.AllowSwap(ActiveHand, CurrentScrollSlot))
			{
				return KeyResult.Invalid;
			}
			return KeyResult.HandToSlot;
		}
		return KeyResult.None;
	}

	public void InventorySelect()
	{
		if (!(Parent == null) && !WorldManager.IsGamePaused && !InputMouse.IsMouseControl && !InputWindowBase.IsInputWindow)
		{
			SinglePressInteraction();
			if (CurrentScollButton != null && !CurrentScollButton.IsVisible)
			{
				NextButton();
			}
		}
	}

	private void SinglePressInteraction()
	{
		if (CurrentScollButton.IsVisible)
		{
			if (CurrentScollButton.Interactable != null)
			{
				CurrentScollButton.Interactable.PlayerInteractWith();
			}
			if (CurrentScrollSlot != null)
			{
				switch (IsValid())
				{
				case KeyResult.Merge:
					ActiveHand.PlayerMergeToSlot(CurrentScrollSlot.Get<IMergeable>());
					break;
				case KeyResult.Swap:
					ActiveHand.PlayerSwapToSlot(CurrentScrollSlot);
					break;
				case KeyResult.HandToSlot:
					CurrentScrollSlot.PlayerMoveToSlot(ActiveHandOccupant);
					break;
				case KeyResult.SlotToHand:
					ActiveHand.PlayerMoveToSlot(CurrentScrollSlot.Occupant);
					break;
				case KeyResult.Invalid:
				case KeyResult.None:
					break;
				}
			}
		}
		else if (CurrentScrollSlot != null && CurrentScollButton.IsVisible)
		{
			if (IsValid() == KeyResult.Invalid)
			{
				CurrentScollButton.SetDisplayAnimState(SlotDisplayState.Disabled);
			}
			else
			{
				CurrentScollButton.RefreshAnimation(playSound: false);
			}
		}
	}

	public void SmartStow()
	{
		if (!(Parent == null) && !WorldManager.IsGamePaused && !InputMouse.IsMouseControl && !InputWindowBase.IsInputWindow && (bool)ActiveHand.Occupant)
		{
			ActiveHand.Button.PerformSmartSwapClick();
		}
	}

	public void TryUpdateSelectedInventorySlot(Slot selectedSlot)
	{
		for (int i = 0; i < _visibleSlots.Count; i++)
		{
			if (_visibleSlots[i].Slot == selectedSlot)
			{
				CurrentSlotIndex = i;
				SwapCurrentSlot();
			}
		}
	}

	public static void HideAll()
	{
		Instance.CloseAllButton.Animator.SetTrigger("Normal");
		foreach (InventoryWindow window in Instance.Windows)
		{
			if (window != null)
			{
				window.SetVisible(isVisble: false);
				window.ParentSlot?.Display?.SetSlotDisplayOpenState(value: false);
			}
		}
		Instance.CloseAllButton.SetVisible(isVisble: false);
		Instance.CloseAllButton.SetIsShown(isShown: false);
		if (InventoryWindowManager.OnUIClose != null)
		{
			InventoryWindowManager.OnUIClose();
		}
	}

	public void ButtonHideAll()
	{
		HideAll();
	}

	public static void ClearAll()
	{
		_visibleSlots.Clear();
	}

	public UserInterfaceSaveData GenerateUISaveData()
	{
		UserInterfaceSaveData userInterfaceSaveData = new UserInterfaceSaveData();
		foreach (InventoryWindow window in Windows)
		{
			if (window.GameObject != null)
			{
				userInterfaceSaveData.OpenSlots.Add(new WindowSaveData
				{
					SlotId = window.ParentSlot.SlotIndex,
					StringHash = window.ParentSlot.StringHash,
					IsOpen = window.IsVisible,
					IsUndocked = window.IsUndocked,
					Position = window.RectTransform.position
				});
			}
		}
		userInterfaceSaveData.SelectedButton = CurrentButtonIndex;
		userInterfaceSaveData.ActiveHandSlot = ActiveHand?.SlotIndex ?? 0;
		return userInterfaceSaveData;
	}

	public void MoveAllOfType()
	{
		throw new NotImplementedException();
	}
}
