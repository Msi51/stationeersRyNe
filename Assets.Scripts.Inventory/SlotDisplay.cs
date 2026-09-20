using System;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Inventory;

[Serializable]
public class SlotDisplay
{
	public delegate void Event();

	public bool KeyDownSwap;

	public SlotDisplayButton SlotDisplayButton;

	public int SlotId;

	public string SwapButton;

	private bool _isDisabled;

	private InventoryWindow _localInventoryWindow;

	public virtual Slot Slot { get; set; }

	public SlotDisplayMirror MirrorChildSlotDisplay { get; set; }

	public InventoryWindow ParentWindow => SlotDisplayButton.ParentWindow;

	public InventoryWindow SlotWindow => SlotDisplayButton.SlotWindow;

	public RectTransform RectTransform => SlotDisplayButton.RectTransform;

	public Image DisplayImage => SlotDisplayButton.Image;

	public Slider DamageSlider => SlotDisplayButton.DamageSlider;

	public Image DamageSliderFill => SlotDisplayButton.DamageSliderFill;

	public Image StateImage => SlotDisplayButton.StateImage;

	public Animator Animator => SlotDisplayButton.Animator;

	public TextMeshProUGUI SlotText => SlotDisplayButton.Text;

	public TextMeshProUGUI QuantityText => SlotDisplayButton.Text2;

	public TextMeshProUGUI SecondaryName => SlotDisplayButton.SecondaryName;

	public HotkeyGrid HotkeyGrid
	{
		get
		{
			if (!SlotDisplayButton)
			{
				return null;
			}
			return SlotDisplayButton.HotkeyGrid;
		}
	}

	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			_isDisabled = value;
			if (SlotDisplayButton.IsVisible)
			{
				if (value)
				{
					SlotDisplayButton.SetDisplayAnimState(SlotDisplayState.Disabled);
				}
				else if (SlotDisplayButton.DisplayState == SlotDisplayState.Disabled)
				{
					SlotDisplayButton.SetDisplayAnimState(SlotDisplayState.Normal);
				}
			}
		}
	}

	public void ClearLocalWindow()
	{
		_localInventoryWindow = null;
	}

	public SlotDisplay(SlotDisplayButton parent)
	{
		parent.SlotDisplay = this;
		SlotDisplayButton = parent;
		Initialise();
	}

	public SlotDisplay(SlotDisplayButton parent, Slot slot)
	{
		parent.SlotDisplay = this;
		SlotDisplayButton = parent;
		Slot = slot;
		Initialise();
	}

	public void Animate(SlotDisplayState slotDisplayState)
	{
		if (SlotDisplayButton != null)
		{
			SlotDisplayButton.SetDisplayAnimState(slotDisplayState);
		}
	}

	public void SetSlotDisplayOpenState(bool value)
	{
		SlotDisplayButton.Open = value;
	}

	public void RefreshAnimation()
	{
		if (SlotDisplayButton != null)
		{
			SlotDisplayButton.RefreshAnimation();
		}
	}

	public void LinkToSlot(DynamicThing parent)
	{
		Slot = parent.Slots[SlotId];
		SlotDisplayButton.SlotDisplay = this;
		parent.Slots[SlotId].Display = this;
	}

	public void InitialiseRoot()
	{
		if (!GameManager.IsBatchMode && Slot != null && Slot.IsInteractable && Slot.Parent.HasSlots)
		{
			SetupWindow(isRoot: true);
		}
	}

	private void Initialise()
	{
		if (!GameManager.IsBatchMode)
		{
			SlotDisplayButton.OnPrimaryAction += OnPlayerInteract;
			SlotDisplayButton.OnPrimarySmartStow += InventoryManager.SmartStow;
		}
	}

	private void SetupWindow(bool isRoot = false)
	{
		if (!(_localInventoryWindow != null))
		{
			_localInventoryWindow = UnityEngine.Object.Instantiate(InventoryWindowManager.Instance.WindowPrefab, InventoryWindowManager.Instance.WindowGrid.transform, worldPositionStays: false);
			_localInventoryWindow.Assign(Slot);
			if (isRoot)
			{
				InventoryWindowManager.Instance.Windows.Add(_localInventoryWindow);
				return;
			}
			ParentWindow.RegisterChildWindow(_localInventoryWindow);
			_localInventoryWindow.name = ParentWindow.name + "_" + Slot.GetSafeName();
			_localInventoryWindow.SetVisible(isVisble: true);
		}
	}

	private void OnPlayerInteract()
	{
		if (!(_localInventoryWindow != null) && Slot != null && !(Slot.Occupant == null) && Slot.IsInteractable && (Slot.Occupant.HasSlots || Slot.Occupant.HasKeyInteractions))
		{
			SetupWindow();
			Slot.OnExit += OnOccupantExit;
		}
	}

	private void OnOccupantExit()
	{
		Slot.OnExit -= OnOccupantExit;
		if (!(_localInventoryWindow == null))
		{
			_localInventoryWindow.Unassign();
			_localInventoryWindow.SetVisible(isVisble: false);
			Slot.OnEnter += OnOccupantEnter;
		}
	}

	private void OnOccupantEnter()
	{
		Slot.OnEnter -= OnOccupantEnter;
		if (!(_localInventoryWindow == null))
		{
			_localInventoryWindow.Assign(Slot);
			Slot.OnExit += OnOccupantExit;
		}
	}

	public void RefreshDisplay()
	{
		if (MirrorChildSlotDisplay != null)
		{
			MirrorChildSlotDisplay.RefreshDisplay();
		}
		if (DisplayImage == null || Slot == null)
		{
			return;
		}
		DynamicThing occupant = Slot.Occupant;
		if ((bool)occupant)
		{
			if ((bool)occupant.Thumbnail)
			{
				DisplayImage.sprite = occupant.GetThumbnail();
			}
			DisplayImage.color = Color.white;
			RefreshQuantity().Forget();
			RefreshDamage().Forget();
		}
		else
		{
			Sprite sprite = Slot.SlotTypeIcon;
			if (Slot.SpecificTypePrefabHashes != null && Slot.SpecificTypePrefabHashes.Length != 0)
			{
				Thing thing = Prefab.Find(Slot.SpecificTypePrefabHashes[0]);
				if (thing != null)
				{
					Sprite thumbnail = thing.GetThumbnail();
					if (thumbnail != null)
					{
						sprite = thumbnail;
					}
				}
			}
			DisplayImage.sprite = sprite;
			DisplayImage.color = (sprite ? Color.white.SetAlpha(0.07f) : Color.grey.SetAlpha(0f));
			if ((bool)QuantityText)
			{
				QuantityText.text = string.Empty;
			}
			if ((bool)SecondaryName)
			{
				SecondaryName.text = string.Empty;
			}
			if ((bool)DamageSlider)
			{
				DamageSlider.gameObject.SetActive(value: false);
			}
			if ((bool)StateImage)
			{
				StateImage.enabled = false;
			}
		}
		RefreshState();
		RefreshHotkeyGrid();
	}

	public async UniTaskVoid RefreshQuantity()
	{
		if (ThreadedManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		if (!(DisplayImage == null) && !(QuantityText == null))
		{
			DynamicThing occupant = Slot.Occupant;
			if ((bool)occupant && (bool)QuantityText && (bool)SecondaryName)
			{
				QuantityText.text = occupant.GetQuantityText();
				SecondaryName.text = occupant.GetSecondaryNameText();
			}
		}
	}

	public async UniTaskVoid RefreshDamage()
	{
		if (ThreadedManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		if (DisplayImage == null || DamageSlider == null)
		{
			return;
		}
		DynamicThing occupant = Slot.Occupant;
		if ((bool)occupant)
		{
			if (occupant.IsBroken)
			{
				DamageSlider.gameObject.SetActive(value: false);
				StateImage.enabled = true;
				return;
			}
			StateImage.enabled = false;
			bool flag = occupant.DamageState.TotalRounded > 0;
			DamageSlider.gameObject.SetActive(flag);
			if (flag)
			{
				DamageSliderFill.color = StatusUpdates.GetDamageColor(occupant.DamageState.TotalRatioClamped);
				DamageSlider.value = occupant.DamageState.TotalRatioClampedUndamaged;
			}
		}
		else
		{
			DamageSlider.gameObject.SetActive(value: false);
		}
	}

	public void RefreshState()
	{
		if (!(SlotDisplayButton == null))
		{
			DynamicThing occupant = Slot.Occupant;
			if (occupant == null)
			{
				SlotDisplayButton.StatusFire.SetVisible(isVisble: false);
				SlotDisplayButton.StatusLeak.SetVisible(isVisble: false);
			}
			else
			{
				SlotDisplayButton.StatusFire.SetVisible(occupant.IsBurning);
				SlotDisplayButton.StatusLeak.SetVisible(!occupant.IsBurning && occupant.IsLeaking);
			}
		}
	}

	public void RefreshHotkeyGrid()
	{
		if (!HotkeyGrid)
		{
			return;
		}
		DynamicThing occupant = Slot.Occupant;
		if (!occupant)
		{
			HotkeyGrid.HideAll();
			return;
		}
		HotkeyGrid.Interaction.SetVisible(occupant.HasKeyInteractions);
		HotkeyGrid.Inventory.SetVisible(occupant.HasSlots);
		if (HotkeyGrid.HasParentBackground)
		{
			bool activeSelf = HotkeyGrid.Interaction.ActiveSelf;
			bool activeSelf2 = HotkeyGrid.Inventory.ActiveSelf;
			if (!activeSelf && !activeSelf2)
			{
				HotkeyGrid.SetParentBackGroundVisible(isVisible: false);
			}
			else
			{
				HotkeyGrid.SetParentBackGroundVisible(isVisible: true);
			}
		}
	}
}
