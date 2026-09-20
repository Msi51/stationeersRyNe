using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class InputMouse : UserInterfaceBase
{
	public static InputMouse Instance;

	public static Slot WorldSlot;

	public static bool IsMouseControl;

	public SlotDisplayButton DragSlotDisplay;

	public CanvasScaler CanvasScaler;

	public TMP_Text MouseControlKey;

	[ReadOnly]
	public Thing CursorThing;

	[ReadOnly]
	public Item CursorItem;

	[ReadOnly]
	public Transform CursorTransform;

	[ReadOnly]
	public WorldMouseMode WorldMode;

	[ReadOnly]
	public Vector2 MousePosition;

	private static Interactable WorldInteractable;

	private SlotDisplayButton _selectedButton;

	private static readonly int InWorldState = Animator.StringToHash("IsWorld");

	private static EventSystem _currentEventSystem;

	public Thing DraggedThing { get; set; }

	public SlotDisplayButton CurrentSlotButton => SlotDisplayButton.CurrentSlot;

	public SlotDisplayButton SelectedButton
	{
		get
		{
			return _selectedButton;
		}
		set
		{
			_selectedButton = value;
			if ((bool)_selectedButton)
			{
				DragSlotDisplay.Image.sprite = _selectedButton.Image.sprite;
			}
			DragSlotDisplay.SetVisible(_selectedButton);
		}
	}

	private static float MaxInteractDistance => CursorManager.MaxInteractDistance;

	private static bool IsMouseOverUi
	{
		get
		{
			if ((!(SlotDisplayButton.CurrentSlot != null) || !SlotDisplayButton.CurrentSlot.isActiveAndEnabled) && (!(InventoryWindow.CurrentWindow != null) || !InventoryWindow.CurrentWindow.isActiveAndEnabled))
			{
				return _currentEventSystem.IsPointerOverGameObject();
			}
			return true;
		}
	}

	private Slot CurrentSlot
	{
		get
		{
			if (!(CurrentSlotButton == null))
			{
				return CurrentSlotButton.Slot;
			}
			return null;
		}
	}

	public void Initialize()
	{
		Instance = this;
		DragSlotDisplay.SetVisible(isVisble: false);
		ScaleDragCursor();
		WorldManager.OnHudScaleUpdate += ScaleDragCursor;
		_currentEventSystem = EventSystem.current;
	}

	public static void SetMouseControl(bool useMouse)
	{
		if (useMouse)
		{
			IsMouseControl = true;
			InventoryManager.Instance?.ActiveHand?.SlotDisplayButton?.RefreshAnimation();
			InventoryWindowManager.RefreshScrolledButton();
			Instance.MouseControlKey.enabled = false;
			CursorManager.Instance.CursorHighlighter.SetActive(value: false);
			CursorManager.SetSelectionVisibility(isVisible: false);
			InventoryManager.Instance.CancelPlacement();
			InventoryManager.Instance.CancelSmartTool();
		}
		else
		{
			IsMouseControl = false;
			InventoryManager.Instance?.ActiveHand?.SlotDisplayButton?.RefreshAnimation();
			InventoryWindowManager.RefreshScrolledButton();
			Instance.MouseControlKey.enabled = true;
			Instance.WorldMode = WorldMouseMode.Idle;
			Instance.DragSlotDisplay.SetVisible(isVisble: false);
		}
	}

	public static DragResult IsValid(DynamicThing dynamicThing, Slot destinationSlot)
	{
		if (dynamicThing == null)
		{
			return DragResult.Invalid;
		}
		if (dynamicThing.ParentSlot != null && destinationSlot == dynamicThing.ParentSlot)
		{
			return DragResult.Self;
		}
		if (destinationSlot == null)
		{
			return DragResult.Drop;
		}
		if (destinationSlot.Type == Slot.Class.Plant)
		{
			return DragResult.Invalid;
		}
		if (dynamicThing is Plant { IsPlanted: not false })
		{
			return DragResult.Invalid;
		}
		if (Slot.CanInsert(dynamicThing, destinationSlot))
		{
			return DragResult.Insert;
		}
		if (dynamicThing.ParentSlot != null && !Slot.AllowSwap(dynamicThing.ParentSlot, destinationSlot))
		{
			return DragResult.Invalid;
		}
		if ((bool)destinationSlot.Occupant)
		{
			if (Slot.CanMerge(dynamicThing, destinationSlot))
			{
				return DragResult.Merge;
			}
			if (!Slot.AllowSwap(destinationSlot, dynamicThing))
			{
				return DragResult.Invalid;
			}
			return DragResult.Swap;
		}
		if (!Slot.AllowMove(dynamicThing, destinationSlot))
		{
			return DragResult.Invalid;
		}
		return DragResult.Valid;
	}

	public static DragResult IsValid()
	{
		if (Instance == null || Instance.SelectedButton == null || Instance.SelectedButton.Slot == null)
		{
			return DragResult.Invalid;
		}
		Slot slot = Instance.SelectedButton.Slot;
		if (slot == null)
		{
			return DragResult.Invalid;
		}
		if (SlotDisplayButton.CurrentSlot == null && InventoryWindow.CurrentWindow != null && InventoryWindow.CurrentWindow == slot.Display.SlotWindow)
		{
			return DragResult.Self;
		}
		if (SlotDisplayButton.CurrentSlot == Instance.SelectedButton && SlotDisplayButton.CurrentSlot != null)
		{
			return DragResult.Self;
		}
		if (SlotDisplayButton.CurrentSlot == null)
		{
			return DragResult.Drop;
		}
		Slot slot2 = SlotDisplayButton.CurrentSlot.Slot;
		if (SlotDisplayButton.CurrentSlot.Interactable != null)
		{
			return DragResult.Invalid;
		}
		if (Slot.CanInsert(slot.Occupant, slot2))
		{
			return DragResult.Insert;
		}
		if (!Slot.AllowSwap(slot, slot2))
		{
			return DragResult.Invalid;
		}
		if ((bool)slot2.Occupant)
		{
			if (Slot.CanMerge(slot.Occupant, slot2))
			{
				return DragResult.Merge;
			}
			if (!Slot.AllowSwap(slot2, slot))
			{
				return DragResult.Invalid;
			}
			return DragResult.Swap;
		}
		if (!Slot.AllowMove(slot.Occupant, slot2))
		{
			return DragResult.Invalid;
		}
		return DragResult.Valid;
	}

	private void HandleMouseInteraction(Interactable interactable)
	{
		Interaction interaction = new Interaction(InventoryManager.Parent, InventoryManager.ActiveHandSlot, CursorThing, KeyManager.GetButton(KeyMap.QuantityModifier));
		Color color = Color.blue;
		Thing.DelayedActionInstance delayedActionInstance = null;
		delayedActionInstance = ((!CursorThing.PreventInteraction(out var failResult, interactable, interaction)) ? CursorThing.InteractWith(interactable, interaction, doAction: false) : failResult);
		if ((delayedActionInstance != null && delayedActionInstance.IsDisabled) || !CursorThing.AllowInteraction)
		{
			color = Color.red;
		}
		else if (delayedActionInstance != null)
		{
			color = ((!InventoryManager.WillStackFromInteractable(interactable)) ? ((delayedActionInstance.Duration > 0f) ? Color.yellow : Color.green) : Color.yellow);
		}
		CursorManager.SetSelection(interactable.GetSelection(), color);
	}

	private static Slot GetHoverWorldSlot()
	{
		if (Physics.Raycast(CameraController.CurrentCamera.ScreenPointToRay(Input.mousePosition), out var hitInfo, MaxInteractDistance, CursorManager.Instance.CursorHitMask))
		{
			Thing componentInParent = hitInfo.transform.GetComponentInParent<Thing>();
			if (componentInParent != null)
			{
				Interactable interactable = componentInParent.GetInteractable(hitInfo.collider);
				if (interactable != null && interactable.Slot != null)
				{
					return interactable.Slot;
				}
			}
		}
		return null;
	}

	private Color GetWorldResultColor(DragResult result)
	{
		return result switch
		{
			DragResult.Swap => Color.green, 
			DragResult.Valid => Color.green, 
			DragResult.Merge => Color.yellow, 
			DragResult.Insert => Color.blue, 
			_ => Color.red, 
		};
	}

	public static void ResetMouseOverUI()
	{
		SlotDisplayButton.CurrentSlot = null;
		InventoryWindow.CurrentWindow = null;
	}

	private void ScaleDragCursor()
	{
		CameraController.SetHudScale(CanvasScaler);
	}

	private void Update()
	{
		if (GameManager.IsBatchMode || !IsMouseControl || InputWindowBase.IsInputWindow || Stationpedia.IsOpenAndLocked || CursorManager.Instance.BlockCursorRaycast || WorldManager.IsGamePaused)
		{
			return;
		}
		HandleSlotDisplay();
		if (DragSlotDisplay.IsVisible)
		{
			WorldMouseMode worldMode = WorldMode;
			if (worldMode != WorldMouseMode.Drag && worldMode != WorldMouseMode.DragSlot)
			{
				return;
			}
		}
		switch (WorldMode)
		{
		case WorldMouseMode.Idle:
			Idle();
			break;
		case WorldMouseMode.Click:
			Click();
			break;
		case WorldMouseMode.Drag:
			Drag();
			break;
		case WorldMouseMode.DragSlot:
			DragSlot();
			break;
		}
	}

	private void Idle()
	{
		PassiveTooltip passiveTooltip = default(PassiveTooltip);
		DraggedThing = null;
		if (Physics.Raycast(CameraController.CurrentCamera.ScreenPointToRay(Input.mousePosition), out var hitInfo, MaxInteractDistance, CursorManager.Instance.CursorHitMask))
		{
			CursorTransform = hitInfo.transform;
			CursorThing = Thing.Find(hitInfo.collider);
			CursorItem = CursorThing as Item;
			Interactable interactable = null;
			if (CursorThing != null && !IsMouseOverUi)
			{
				passiveTooltip = CursorThing.GetPassiveTooltip(hitInfo.collider);
				interactable = CursorThing.GetInteractable(hitInfo.collider);
				if (interactable != null && interactable.Slot == null && !IsMouseOverUi)
				{
					HandleMouseInteraction(interactable);
					WorldInteractable = interactable;
				}
				else if (interactable != null && interactable.Slot != null && !IsMouseOverUi)
				{
					WorldInteractable = interactable;
					HandleMouseInteraction(interactable);
				}
				else if (CursorItem != null)
				{
					Color color = (InventoryManager.ActiveHandSlot.Occupant ? Color.red : Color.green);
					CursorManager.SetSelection(CursorItem.GetSelection(), color);
					WorldInteractable = null;
					Tooltip.SetColorForItemAction(ref passiveTooltip, CursorThing);
				}
				else
				{
					CursorManager.SetSelectionVisibility(isVisible: false);
					CursorManager.ClearLastSelectionId();
					WorldInteractable = null;
				}
				if (interactable != null)
				{
					Tooltip.SetValuesForInteractable(ref passiveTooltip, CursorThing, interactable);
				}
				passiveTooltip.FollowMouseMovement = true;
				InventoryManager.Instance.TooltipRef.HandleToolTipDisplay(passiveTooltip);
			}
			else
			{
				CursorManager.SetSelectionVisibility(isVisible: false);
				CursorManager.ClearLastSelectionId();
				ClearTooltip();
				WorldInteractable = null;
			}
			if (KeyManager.GetMouseDown("Primary") && !IsMouseOverUi)
			{
				if (interactable != null && interactable.Slot == null)
				{
					interactable.PlayerInteractWith(InventoryManager.ActiveHandSlot);
				}
				else if (interactable != null && interactable.Slot != null && SlotDisplayButton.CurrentSlot == null && InventoryWindow.CurrentWindow == null)
				{
					WorldMode = WorldMouseMode.Click;
					MousePosition = Input.mousePosition;
				}
				else if ((bool)CursorItem)
				{
					WorldMode = WorldMouseMode.Click;
					MousePosition = Input.mousePosition;
					return;
				}
			}
		}
		else
		{
			CursorManager.SetSelectionVisibility(isVisible: false);
			if (IsMouseOverUi)
			{
				ClearTooltip();
			}
		}
		CursorTransform = null;
		CursorItem = null;
	}

	private void ClearTooltip()
	{
		if (InventoryManager.Instance.TooltipRef.IsVisible)
		{
			InventoryManager.Instance.TooltipRef.ClearTooltip();
			InventoryManager.Instance.TooltipRef.DrawTooltip();
		}
	}

	private void Click()
	{
		if (KeyManager.GetMouseUp("Primary"))
		{
			WorldMode = WorldMouseMode.Idle;
			if (WorldInteractable != null)
			{
				WorldInteractable.PlayerInteractWith(InventoryManager.ActiveHandSlot);
				CursorManager.SetSelectionVisibility(isVisible: false);
			}
			else if (CursorItem != null)
			{
				MoveCurrentItemToHand();
			}
		}
		else if (WorldInteractable != null && WorldInteractable.Slot != null && (bool)(WorldInteractable.Slot.Occupant as Item) && Vector2.Distance(MousePosition, Input.mousePosition) > 0.1f)
		{
			WorldMode = WorldMouseMode.DragSlot;
			CursorItem = (Item)WorldInteractable.Slot.Occupant;
			DraggedThing = WorldInteractable.Slot.Occupant;
			DragSlotDisplay.Image.sprite = CursorItem.GetThumbnail();
			DragSlotDisplay.SetVisible(isVisble: true);
			CursorManager.SetSelectionVisibility(isVisible: false);
		}
		else if ((bool)CursorItem && Vector2.Distance(MousePosition, Input.mousePosition) > 0.1f)
		{
			WorldMode = WorldMouseMode.Drag;
			DraggedThing = CursorThing;
			DragSlotDisplay.Image.sprite = CursorItem.GetThumbnail();
			DragSlotDisplay.SetVisible(isVisble: true);
		}
	}

	private void Drag()
	{
		ClearTooltip();
		if (KeyManager.GetMouseUp("Primary"))
		{
			WorldMode = WorldMouseMode.Idle;
			Slot slot = CurrentSlotButton?.Slot ?? WorldSlot;
			switch (IsValid(CursorItem, slot))
			{
			case DragResult.Swap:
				slot.PlayerSwapToWorld(CursorItem);
				break;
			case DragResult.Valid:
				slot.PlayerMoveToSlot(CursorItem);
				break;
			case DragResult.Merge:
				slot.PlayerMergeToSlot(CursorItem as IMergeable);
				break;
			case DragResult.Insert:
				slot.PlayerInsertToFreeSlot(CursorItem);
				break;
			}
			DragSlotDisplay.SetVisible(isVisble: false);
			CursorManager.SetSelectionVisibility(isVisible: false);
		}
	}

	private void DragSlot()
	{
		ClearTooltip();
		if (!KeyManager.GetMouseUp("Primary"))
		{
			return;
		}
		WorldMode = WorldMouseMode.Idle;
		Slot slot = CurrentSlotButton?.Slot ?? WorldSlot;
		switch (IsValid(CursorItem, slot))
		{
		case DragResult.Swap:
			if (CursorItem.ParentSlot != null)
			{
				slot.PlayerSwapToSlot(CursorItem.ParentSlot);
			}
			else
			{
				slot.PlayerSwapToWorld(CursorItem);
			}
			break;
		case DragResult.Valid:
			slot.PlayerMoveToSlot(CursorItem);
			break;
		case DragResult.Merge:
			slot.PlayerMergeToSlot(CursorItem as IMergeable);
			break;
		case DragResult.Insert:
			slot.PlayerInsertToFreeSlot(CursorItem);
			break;
		}
		DragSlotDisplay.SetVisible(isVisble: false);
	}

	private void HandleSlotDisplay()
	{
		if (!DragSlotDisplay.IsVisible)
		{
			return;
		}
		DragSlotDisplay.transform.position = ((CurrentSlotButton != null && CurrentSlotButton.Slot != null) ? CurrentSlotButton.Image.transform.position : Input.mousePosition);
		if (WorldMode == WorldMouseMode.Idle)
		{
			WorldSlot = GetHoverWorldSlot();
			if (WorldSlot != null && CurrentSlot == null && Instance.SelectedButton != null && Instance.SelectedButton.Slot != null)
			{
				DragSlotDisplay.Animator.SetBool(InWorldState, value: true);
				DragResult result = IsValid(Instance.SelectedButton.Slot.Occupant, WorldSlot);
				DragSlotDisplay.Animator.SetTrigger(IsValid(Instance.SelectedButton.Slot.Occupant, WorldSlot).ToString());
				CursorManager.SetSelection(WorldSlot.Interactable.GetSelection(), GetWorldResultColor(result));
				return;
			}
			if (DragSlotDisplay.Animator.HasState(0, InWorldState))
			{
				DragSlotDisplay.Animator.SetBool(InWorldState, value: false);
			}
			DragSlotDisplay.Animator.SetTrigger(IsValid().ToString());
			CursorManager.SetSelectionVisibility(isVisible: false);
			return;
		}
		WorldSlot = GetHoverWorldSlot();
		if (SlotDisplayButton.CurrentSlot != null && SlotDisplayButton.CurrentSlot.Interactable == null)
		{
			DragSlotDisplay.Animator.SetBool(InWorldState, value: false);
			DragSlotDisplay.Animator.SetTrigger(IsValid(CursorItem, SlotDisplayButton.CurrentSlot.Slot).ToString());
			CursorManager.SetSelectionVisibility(isVisible: false);
		}
		else if (WorldSlot != null)
		{
			DragSlotDisplay.Animator.SetBool(InWorldState, value: true);
			DragResult result2 = IsValid(CursorItem, WorldSlot);
			CursorManager.SetSelection(WorldSlot.Interactable.GetSelection(), GetWorldResultColor(result2));
			DragSlotDisplay.Animator.SetTrigger(IsValid(CursorItem, WorldSlot).ToString());
		}
		else
		{
			DragSlotDisplay.Animator.SetBool(InWorldState, value: false);
			DragSlotDisplay.Animator.SetTrigger(DragResult.Self.ToString());
			CursorManager.SetSelectionVisibility(isVisible: false);
		}
	}

	private void MoveCurrentItemToHand()
	{
		Stackable stackable = CursorThing as Stackable;
		SlotDisplay activeHand = InventoryManager.Instance.ActiveHand;
		if ((bool)stackable && activeHand.Slot.IsNotEmpty() && activeHand.Slot.Contains<Stackable>(out var occupant) && stackable.CanStack(occupant))
		{
			Thing.Merge(stackable, occupant);
			activeHand.Slot.PlaySlotEnterUiSound();
			AddVelocityOnPickup();
		}
		else if (activeHand.Slot.IsEmpty())
		{
			OnServer.MoveToSlot(CursorThing as DynamicThing, activeHand.Slot);
			InventoryManager.Parent.Animator.SetBool(MovementController.HasItemHash, value: true);
			activeHand.Slot.PlaySlotEnterUiSound();
			AddVelocityOnPickup();
		}
	}

	private void AddVelocityOnPickup()
	{
		if (!InventoryManager.Parent.RigidBody.isKinematic)
		{
			float num = (InventoryManager.Parent.RigidBody.useGravity ? 10f : 2f);
			InventoryManager.Parent.RigidBody.velocity += (CursorItem.RigidBody.velocity - InventoryManager.Parent.RigidBody.velocity) / num;
		}
	}
}
