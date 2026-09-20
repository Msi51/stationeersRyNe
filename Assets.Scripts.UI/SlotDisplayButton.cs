using System;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using CharacterCustomisation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class SlotDisplayButton : UserInterfaceBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IComparable<SlotDisplayButton>
{
	public new delegate void Event();

	public delegate void SmartStowEvent(Slot slot);

	public static SlotDisplayButton CurrentSlot;

	public bool SpeciesSpecific = true;

	[FormerlySerializedAs("Species")]
	public SpeciesClass SpeciesClass;

	public Animator Animator;

	public Image Image;

	public TextMeshProUGUI Text;

	public TextMeshProUGUI Text2;

	public TextMeshProUGUI SecondaryName;

	public Slider DamageSlider;

	public Image DamageSliderFill;

	public Image StateImage;

	public HotkeyGrid HotkeyGrid;

	public UserInterfaceAnimated StatusFire;

	public UserInterfaceAnimated StatusLeak;

	public UiComponentRenderer ActiveHand;

	[Header("Serialize these on the Prefab")]
	[SerializeField]
	private RectTransform quantityRectTransform;

	[SerializeField]
	private Image slotBackGround;

	[SerializeField]
	private Sprite normalBackgroundSprite;

	[SerializeField]
	private Sprite hoveredBackgroundSprite;

	[SerializeField]
	private Sprite disabledBackgroundSprite;

	[SerializeField]
	private Sprite pressedBackgroundSprite;

	[SerializeField]
	private TextMeshProUGUI QuantityText;

	[SerializeField]
	private Image slotOpenImage;

	private static readonly Color NormalColor = new Color(1f, 1f, 1f, 0.75f);

	private static readonly Color PressedColor = new Color(1f, 1f, 1f, 1f);

	private static readonly Color OpenImageClosed = new Color(1f, 1f, 1f, 0f);

	private static readonly Color OpenImageOpen = new Color(1f, 1f, 1f, 1f);

	private static readonly Color OpenImageHighlightedOpen = new Color(0.98431f, 0.6902f, 0.23137f, 1f);

	private static readonly Vector3 HighlightedScale = new Vector3(1.06f, 1.06f, 1f);

	private static readonly float QuantityFontMinSize = 14f;

	private static readonly float QuantityFontMaxSize = 20f;

	private static readonly float QuantitySizeDeltaX = 105f;

	private static readonly float QuantitySizeDeltaY = 36f;

	private SlotDisplayState _displayState;

	private bool _open;

	public static readonly int InventoryScrollHash = Animator.StringToHash("InventoryScroll");

	public SlotDisplay SlotDisplay { get; set; }

	public override bool IsVisible
	{
		get
		{
			if (!base.IsVisible || (object)ParentWindow != null)
			{
				if (base.IsVisible && (object)ParentWindow != null)
				{
					return ParentWindow.IsVisible;
				}
				return false;
			}
			return true;
		}
	}

	public Slot Slot => SlotDisplay?.Slot;

	public SlotDisplayButtonMirror MirrorChildSlotDisplayButton { get; set; }

	public Interactable Interactable { get; set; }

	public int ParentSlotId => ParentWindow.ParentSlot.SlotIndex;

	public virtual InventoryWindow SlotWindow { get; set; }

	public InventoryWindow ParentWindow { get; set; }

	public bool IsHandSlot
	{
		get
		{
			if (Slot != InventoryManager.LeftHandSlot)
			{
				return Slot == InventoryManager.RightHandSlot;
			}
			return true;
		}
	}

	public bool IsLeftHand => Slot == InventoryManager.LeftHandSlot;

	public SlotDisplayState DisplayState
	{
		get
		{
			return _displayState;
		}
		private set
		{
			_displayState = value;
			UpdateSlotAnimation();
		}
	}

	public bool Open
	{
		get
		{
			return _open;
		}
		set
		{
			if (value != _open)
			{
				_open = value;
				UpdateSlotAnimation();
			}
		}
	}

	public event Event OnPrimaryAction;

	public event SmartStowEvent OnPrimarySmartStow;

	public bool IsAvailableForSpecies(SpeciesClass speciesClass)
	{
		if (!SpeciesSpecific)
		{
			return true;
		}
		return SpeciesClass == speciesClass;
	}

	public virtual void PrimaryAction(bool isButtonPress)
	{
		if (!InputMouse.IsMouseControl || !KeyManager.GetButton(KeyMap.MouseInspect))
		{
			if (isButtonPress)
			{
				PerformSingleClick();
				return;
			}
			if (Slot != null)
			{
				PerformSingleClick();
				return;
			}
			this.OnPrimaryAction?.Invoke();
			Open = SlotWindow != null && SlotWindow.IsVisible;
		}
	}

	public virtual void SecondaryAction(bool isButtonPress)
	{
		if ((!InputMouse.IsMouseControl || !KeyManager.GetButton(KeyMap.MouseInspect)) && Slot != null)
		{
			PerformSmartSwapClick();
		}
	}

	public void PerformSmartSwapClick()
	{
		this.OnPrimarySmartStow?.Invoke(Slot);
	}

	private void PerformSingleClick()
	{
		this.OnPrimaryAction?.Invoke();
		Open = SlotWindow != null && SlotWindow.IsVisible;
	}

	public void SetDisplayAnimState(SlotDisplayState newState)
	{
		if (newState != DisplayState)
		{
			DisplayState = newState;
		}
	}

	private void UpdateSlotAnimation()
	{
		switch (DisplayState)
		{
		case SlotDisplayState.Normal:
			RectTransform.localScale = Vector3.one;
			if (slotBackGround != null)
			{
				slotBackGround.color = NormalColor;
				slotBackGround.sprite = normalBackgroundSprite;
			}
			if (quantityRectTransform != null)
			{
				quantityRectTransform.sizeDelta.Set(QuantitySizeDeltaX, QuantitySizeDeltaY);
			}
			if (QuantityText != null)
			{
				QuantityText.fontSizeMax = QuantityFontMaxSize;
				QuantityText.fontSizeMin = QuantityFontMinSize;
			}
			if (slotOpenImage != null)
			{
				slotOpenImage.color = (Open ? OpenImageOpen : OpenImageClosed);
			}
			if (ActiveHand != null)
			{
				ActiveHand.SetVisible(isVisble: false);
			}
			break;
		case SlotDisplayState.Highlighted:
			RectTransform.localScale = HighlightedScale;
			if (slotBackGround != null)
			{
				slotBackGround.sprite = hoveredBackgroundSprite;
			}
			if (slotOpenImage != null)
			{
				slotOpenImage.color = (Open ? OpenImageHighlightedOpen : OpenImageClosed);
			}
			if ((GameManager.GameState == GameState.Running && IsVisible) || this == InputMouse.Instance.CurrentSlotButton)
			{
				UIAudioManager.Play(InventoryScrollHash);
			}
			if (ActiveHand != null)
			{
				ActiveHand.SetVisible(isVisble: true);
			}
			break;
		case SlotDisplayState.Pressed:
			RectTransform.localScale = Vector3.one;
			if (slotBackGround != null)
			{
				slotBackGround.color = PressedColor;
				slotBackGround.sprite = pressedBackgroundSprite;
			}
			if (slotOpenImage != null)
			{
				slotOpenImage.color = (Open ? OpenImageOpen : OpenImageClosed);
			}
			break;
		case SlotDisplayState.Disabled:
			RectTransform.localScale = HighlightedScale;
			if (slotBackGround != null)
			{
				slotBackGround.color = NormalColor;
				slotBackGround.sprite = disabledBackgroundSprite;
			}
			if (slotOpenImage != null)
			{
				slotOpenImage.color = (Open ? OpenImageOpen : OpenImageClosed);
			}
			break;
		}
	}

	public void SetDropRatio(float ratio)
	{
		ratio = Mathf.Clamp01(ratio);
		Color color = slotBackGround.color;
		color.a = Mathf.Lerp(0.75f, 1f, ratio);
		slotBackGround.color = color;
	}

	public void RefreshAnimation(bool playSound = true)
	{
		Slot slot = Slot;
		if ((slot != null && slot.IsLocked) || (Interactable != null && Interactable.Display.IsDisabled))
		{
			SetDisplayAnimState(SlotDisplayState.Disabled);
		}
		else if (this == InputMouse.Instance.CurrentSlotButton || (!InputMouse.IsMouseControl && (this == InventoryWindowManager.CurrentScollButton || this == InventoryManager.ActiveHandSlot.Display.SlotDisplayButton)))
		{
			SetDisplayAnimState(SlotDisplayState.Highlighted);
		}
		else
		{
			SetDisplayAnimState(SlotDisplayState.Normal);
		}
	}

	public void Animate(int state, bool value)
	{
		if (IsVisible && !(Animator == null))
		{
			Animator.SetBool(state, value);
		}
	}

	private bool HandleDraggingFromWorld()
	{
		WorldMouseMode worldMode = InputMouse.Instance.WorldMode;
		if (worldMode != WorldMouseMode.Drag && worldMode != WorldMouseMode.DragSlot)
		{
			return false;
		}
		Thing draggedThing = InputMouse.Instance.DraggedThing;
		if (draggedThing == null || Slot == null)
		{
			return false;
		}
		CanEnterResult canEnterResult = draggedThing.CanEnter(Slot);
		if ((bool)canEnterResult)
		{
			return false;
		}
		PanelToolTip.Instance.SetUpTooltip(draggedThing.DisplayName, canEnterResult.Reason);
		return true;
	}

	private bool HandleDraggingFromSlot()
	{
		if (InputMouse.Instance.WorldMode != WorldMouseMode.Idle)
		{
			return false;
		}
		if (InputMouse.Instance.SelectedButton == null)
		{
			return false;
		}
		DynamicThing dynamicThing = InputMouse.Instance.SelectedButton.Slot?.Occupant;
		if (dynamicThing == null || Slot == null)
		{
			return false;
		}
		if (!(dynamicThing.RootParent is Human))
		{
			return false;
		}
		CanEnterResult canEnterResult = dynamicThing.CanEnter(Slot);
		if ((bool)canEnterResult)
		{
			return false;
		}
		PanelToolTip.Instance.SetUpTooltip(dynamicThing.DisplayName, canEnterResult.Reason);
		return true;
	}

	private void ShowSlotTooltip()
	{
		if (Settings.CurrentData.ShowSlotToolTips && !HandleDraggingFromWorld() && !HandleDraggingFromSlot() && Slot != null && Slot.Occupant != null)
		{
			PanelToolTip.Instance.SetUpTooltip(Slot.Occupant);
		}
	}

	public new void OnPointerEnter(PointerEventData eventData)
	{
		if ((Slot != null || Interactable != null) && (Interactable == null || !Interactable.Display.IsDisabled))
		{
			ShowSlotTooltip();
			CurrentSlot = this;
			RefreshAnimation();
		}
	}

	public new void OnPointerExit(PointerEventData eventData)
	{
		if (Slot != null || Interactable != null)
		{
			if (CurrentSlot == this)
			{
				CurrentSlot = null;
			}
			PanelToolTip.Instance.ClearToolTip();
			RefreshAnimation();
		}
	}

	private void PlayerMergeToSlot(SlotDisplayButton slotDisplayButton)
	{
		if (!Slot.IsLocked && !slotDisplayButton.Slot.IsLocked)
		{
			Slot.PlayerMergeToSlot(slotDisplayButton.Slot.Get<IMergeable>());
		}
	}

	private void PlayerMoveToSlot(SlotDisplayButton slotDisplayButton)
	{
		if (!Slot.IsLocked && !slotDisplayButton.Slot.IsLocked)
		{
			Slot.PlayerMoveToSlot(slotDisplayButton.Slot.Occupant);
		}
	}

	private void PlayerSwapToSlot(SlotDisplayButton slotDisplayButton)
	{
		if (!Slot.IsLocked && !slotDisplayButton.Slot.IsLocked)
		{
			Slot.PlayerSwapToSlot(slotDisplayButton.Slot);
		}
	}

	private void PlayerMoveToWorld()
	{
		if (!Slot.IsLocked)
		{
			Slot.PlayerMoveToWorld();
		}
	}

	private void PlayerInsertIntoFreeSlot(SlotDisplayButton slotDisplayButton)
	{
		Slot.PlayerInsertToFreeSlot(slotDisplayButton.Slot.Occupant);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (Slot != null && !(Slot.Occupant == null) && KeyManager.GetButton(KeyMap.PrimaryAction))
		{
			InputMouse.Instance.SelectedButton = this;
			Image.color = Color.gray.SetAlpha(0.5f);
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (Slot == null || Slot.Occupant == null)
		{
			return;
		}
		if (InputMouse.WorldSlot != null && CurrentSlot == null)
		{
			switch (InputMouse.IsValid(Slot.Occupant, InputMouse.WorldSlot))
			{
			case DragResult.Merge:
				InputMouse.WorldSlot.PlayerMergeToSlot(Slot.Get<IMergeable>());
				break;
			case DragResult.Swap:
				InputMouse.WorldSlot.PlayerSwapToSlot(Slot.Occupant.ParentSlot);
				break;
			case DragResult.Valid:
				InputMouse.WorldSlot.PlayerMoveToSlot(Slot.Occupant);
				break;
			case DragResult.Insert:
				InputMouse.WorldSlot.PlayerInsertToFreeSlot(Slot.Occupant);
				break;
			}
		}
		else
		{
			switch (InputMouse.IsValid())
			{
			case DragResult.Merge:
				CurrentSlot.PlayerMergeToSlot(this);
				break;
			case DragResult.Drop:
				PlayerMoveToWorld();
				break;
			case DragResult.Swap:
				PlayerSwapToSlot(CurrentSlot);
				break;
			case DragResult.Valid:
				CurrentSlot.PlayerMoveToSlot(this);
				break;
			case DragResult.Insert:
				CurrentSlot.PlayerInsertIntoFreeSlot(this);
				break;
			}
		}
		Slot.RefreshSlotDisplay();
		InputMouse.Instance.SelectedButton = null;
		RefreshAnimation();
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (Slot != null && !(Slot.Occupant == null))
		{
			RefreshAnimation();
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (Slot != null && (bool)Slot.Occupant && KeyManager.GetButton(KeyMap.MouseControl) && KeyManager.GetButton(KeyMap.MouseInspect))
		{
			Stationpedia.OpenAt(Slot.Occupant);
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (!(InputMouse.Instance.SelectedButton == this))
		{
			switch (eventData.button)
			{
			case PointerEventData.InputButton.Left:
				PrimaryAction(isButtonPress: false);
				break;
			case PointerEventData.InputButton.Right:
				SecondaryAction(isButtonPress: false);
				break;
			case PointerEventData.InputButton.Middle:
				break;
			}
		}
	}

	public void Awake()
	{
		UpdateSlotAnimation();
	}

	public void Update()
	{
		if (GameManager.GameState == GameState.Running && IsVisible && Slot != null && !(Slot.Occupant == null) && !WorldManager.IsGamePaused)
		{
			Slot.Occupant.OnDisplayInPlayerWindow();
		}
	}

	public void HandActive()
	{
		if (ActiveHand != null)
		{
			ActiveHand.SetVisible(isVisble: true);
		}
	}

	public void HandInactive()
	{
		if (ActiveHand != null)
		{
			ActiveHand.SetVisible(isVisble: false);
		}
	}

	private void OnDestroy()
	{
		if ((bool)ParentWindow)
		{
			InventoryWindowManager.Deregister(this);
		}
	}

	public int CompareTo(SlotDisplayButton other)
	{
		if (ParentSlotId < other.ParentSlotId)
		{
			return 1;
		}
		if (ParentSlotId > other.ParentSlotId)
		{
			return -1;
		}
		if (Slot == null && other.Slot != null)
		{
			return 1;
		}
		if (Slot != null && other.Slot == null)
		{
			return -1;
		}
		if (Slot != null && other.Slot != null)
		{
			if (Slot.SlotIndex < other.Slot.SlotIndex)
			{
				return 1;
			}
			if (Slot.SlotIndex > other.Slot.SlotIndex)
			{
				return -1;
			}
		}
		if (Interactable != null && other.Interactable != null)
		{
			if (Interactable.InteractableId < other.Interactable.InteractableId)
			{
				return 1;
			}
			if (Interactable.InteractableId > other.Interactable.InteractableId)
			{
				return -1;
			}
		}
		return 0;
	}
}
