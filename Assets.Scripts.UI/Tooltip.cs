using System.Text;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class Tooltip : UserInterfaceBase
{
	public bool WantDraw;

	public bool Dirty;

	public RectTransform TooltipTransform;

	public Image TooltipBackground;

	public TextMeshProUGUI TooltipTitle;

	public TextMeshProUGUI TooltipAction;

	public TextMeshProUGUI TooltipState;

	public TextMeshProUGUI TooltipExtended;

	public TextMeshProUGUI ToolTipBuildStateInfo;

	public TextMeshProUGUI ToolTipRepairStateInfo;

	public TextMeshProUGUI ToolTipDeconstructBuildStateInfo;

	public TextMeshProUGUI ToolTipPlacementType;

	public TextMeshProUGUI TooltipNumberOfBuildStates;

	public Slider TooltipSlider;

	public Image TooltipSliderFill;

	public Color Color = Color.white;

	public TooltipMode Mode;

	[Header("Game Objects for Elements")]
	public UiComponentRenderer FullToolTipHotKeys;

	public UiComponentRenderer FullToolTipPanel;

	public UiComponentRenderer ActionRotateObject;

	public UiComponentRenderer ActionScrollCycle;

	public UiComponentRenderer ActionConstructionRotate;

	public UiComponentRenderer ActionBuildObject;

	public UiComponentRenderer ToolTipSliderRenderer;

	public UiComponentRenderer InfoConstructionGameObject;

	public UiComponentRenderer InfoDeconstructGameObject;

	public UiComponentRenderer InfoRepairGameObject;

	public UiComponentRenderer InfoPlacementGameObject;

	public UiComponentRenderer TitleRenderer;

	public UiComponentRenderer StateRenderer;

	public UiComponentRenderer ExtendedRenderer;

	private string _action = string.Empty;

	private string _title = string.Empty;

	private string _state = string.Empty;

	private string _extended = string.Empty;

	private string _buildStateInfo = string.Empty;

	private Vector2 _screenPosition;

	private string _deconstructBuildState = string.Empty;

	private string _repairStateInfo = string.Empty;

	private string _placementType = string.Empty;

	private float _slider = -1f;

	[SerializeField]
	private Vector2 _offset;

	public static StringBuilder ToolTipStringBuilder = new StringBuilder();

	private bool _hasState;

	private bool _hasTitle;

	private bool _hasAction;

	private bool _hasExtended;

	private bool _hasConstruction;

	private bool _hasDeconstruction;

	private bool _hasPlacement;

	private bool _hasRepair;

	[Header("Input Mouse ToolTip")]
	public RectTransform MainRectTransform;

	private int _offsetY = 30;

	private int _offsetX;

	private bool TooltipFollowMouse;

	public string Action
	{
		get
		{
			return _action;
		}
		set
		{
			if (Mode == TooltipMode.Hidden)
			{
				Mode = TooltipMode.ActionFirst;
			}
			if (_action != value)
			{
				Dirty = true;
			}
			_action = value;
			TooltipAction.text = value;
		}
	}

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (Mode == TooltipMode.Hidden)
			{
				Mode = TooltipMode.ActionFirst;
			}
			if (_title != value)
			{
				Dirty = true;
			}
			_title = value;
			TooltipTitle.text = _title;
		}
	}

	public string State
	{
		get
		{
			return _state;
		}
		set
		{
			if (Mode == TooltipMode.Hidden)
			{
				Mode = TooltipMode.ActionFirst;
			}
			if (_state != value)
			{
				Dirty = true;
			}
			_state = value;
			TooltipState.text = _state;
		}
	}

	public string Extended
	{
		get
		{
			return _extended;
		}
		set
		{
			if (Mode == TooltipMode.Hidden)
			{
				Mode = TooltipMode.ActionFirst;
			}
			if (_extended != value)
			{
				Dirty = true;
			}
			_extended = value;
			TooltipExtended.text = _extended;
		}
	}

	public string BuildStateInfo
	{
		get
		{
			return _buildStateInfo;
		}
		set
		{
			if (Mode == TooltipMode.Hidden)
			{
				Mode = TooltipMode.ActionFirst;
			}
			if (_buildStateInfo != value)
			{
				Dirty = true;
			}
			_buildStateInfo = value;
			ToolTipBuildStateInfo.text = _buildStateInfo;
		}
	}

	public string DeconstructBuildState
	{
		get
		{
			return _deconstructBuildState;
		}
		set
		{
			if (Mode == TooltipMode.Hidden)
			{
				Mode = TooltipMode.ActionFirst;
			}
			if (_deconstructBuildState != value)
			{
				Dirty = true;
			}
			_deconstructBuildState = value;
			ToolTipDeconstructBuildStateInfo.text = _deconstructBuildState;
		}
	}

	public string RepairBuildState
	{
		get
		{
			return _repairStateInfo;
		}
		set
		{
			if (Mode == TooltipMode.Hidden)
			{
				Mode = TooltipMode.ActionFirst;
			}
			if (_repairStateInfo != value)
			{
				Dirty = true;
			}
			_repairStateInfo = value;
			if ((object)ToolTipRepairStateInfo != null)
			{
				ToolTipRepairStateInfo.text = _repairStateInfo;
			}
		}
	}

	public string PlacementType
	{
		get
		{
			return _placementType;
		}
		set
		{
			if (Mode == TooltipMode.Hidden)
			{
				Mode = TooltipMode.ActionFirst;
			}
			if (_placementType != value)
			{
				Dirty = true;
			}
			_placementType = value;
			ToolTipPlacementType.text = _placementType;
		}
	}

	public float Slider
	{
		get
		{
			return _slider;
		}
		set
		{
			_slider = value;
		}
	}

	public Vector3 WorldPosition
	{
		set
		{
			_screenPosition = CameraController.CurrentCamera.WorldToViewportPoint(value);
		}
	}

	public void ClearTooltip()
	{
		Dirty = false;
		WantDraw = false;
		TooltipMode mode = Mode;
		Mode = TooltipMode.Hidden;
		Color = Color.white;
		_screenPosition = Vector2.one * 0.5f;
		_extended = string.Empty;
		_state = string.Empty;
		_title = string.Empty;
		_action = string.Empty;
		_buildStateInfo = string.Empty;
		_repairStateInfo = string.Empty;
		_deconstructBuildState = string.Empty;
		_placementType = string.Empty;
		if (mode != TooltipMode.Hidden)
		{
			DrawTooltip();
		}
	}

	public void Start()
	{
	}

	public void SetUpToolTip(string action, PassiveTooltip cursorPassiveTooltip)
	{
		Action = cursorPassiveTooltip.Action;
		Title = cursorPassiveTooltip.Title;
		State = cursorPassiveTooltip.State;
		Color = cursorPassiveTooltip.color;
		Slider = cursorPassiveTooltip.Slider;
		Extended = cursorPassiveTooltip.Extended;
		BuildStateInfo = cursorPassiveTooltip.ConstructString;
		DeconstructBuildState = cursorPassiveTooltip.DeconstructString;
		RepairBuildState = cursorPassiveTooltip.RepairString;
		PlacementType = cursorPassiveTooltip.PlacementString;
		TooltipNumberOfBuildStates.text = cursorPassiveTooltip.BuildStateIndexMessage;
		_hasTitle = !string.IsNullOrEmpty(_title) && _title.Length > 0;
		_hasState = !string.IsNullOrEmpty(_state) && _state.Length > 0;
		_hasAction = !string.IsNullOrEmpty(_action) && _action.Length > 0;
		_hasExtended = !string.IsNullOrEmpty(_extended) && _extended.Length > 0;
		_hasConstruction = !string.IsNullOrEmpty(_buildStateInfo);
		_hasDeconstruction = !string.IsNullOrEmpty(_deconstructBuildState);
		_hasRepair = !string.IsNullOrEmpty(_repairStateInfo);
		_hasPlacement = !string.IsNullOrEmpty(PlacementType);
		Action = "<color=#" + ColorToHex(Color) + ">" + Action + "</color> ";
	}

	public void HandleToolTipDisplay(PassiveTooltip cursorPassiveTooltip)
	{
		WantDraw = true;
		if ((bool)InventoryManager.ParentHuman && (bool)InventoryManager.Instance.ActiveHand.Slot.Occupant && InventoryManager.Instance.ActiveHand.Slot.Occupant is Tablet)
		{
			return;
		}
		SetUpToolTip(cursorPassiveTooltip.Action, cursorPassiveTooltip);
		bool flag = !InventoryManager.Instance.UIProgressionBar.IsVisible && (cursorPassiveTooltip.ShowRotate || cursorPassiveTooltip.ShowScroll || (cursorPassiveTooltip.ShowAction && _hasAction));
		bool flag2 = !InventoryManager.Instance.UIProgressionBar.IsVisible && (_hasAction || _hasConstruction || _hasDeconstruction || _hasPlacement || _hasState || _hasTitle || _hasRepair || _hasExtended);
		if (!flag2 && !flag)
		{
			UiComponentRenderer.SetVisible(isVisble: false);
			return;
		}
		if (!UiComponentRenderer.IsVisible)
		{
			UiComponentRenderer.SetVisible(isVisble: true);
		}
		FullToolTipHotKeys.SetVisible(flag);
		FullToolTipPanel.SetVisible(flag2);
		if (flag2)
		{
			InfoConstructionGameObject.SetVisible(_hasConstruction);
			InfoRepairGameObject?.SetVisible(_hasRepair);
			InfoDeconstructGameObject.SetVisible(_hasDeconstruction);
			InfoPlacementGameObject.SetVisible(_hasPlacement);
			StateRenderer.SetVisible(_hasState);
			ExtendedRenderer.SetVisible(_hasExtended);
			TitleRenderer.SetVisible(_hasTitle);
			if (Slider >= 0f)
			{
				ToolTipSliderRenderer.SetVisible(isVisble: true);
				TooltipSlider.value = Slider;
				TooltipSliderFill.color = StatusUpdates.GetDamageColor(1f - Slider);
			}
			else
			{
				ToolTipSliderRenderer.SetVisible(isVisble: false);
			}
		}
		if (flag)
		{
			ActionRotateObject.SetVisible(cursorPassiveTooltip.ShowRotate);
			ActionScrollCycle.SetVisible(cursorPassiveTooltip.ShowScroll);
			ActionConstructionRotate.SetVisible(cursorPassiveTooltip.ShowConstructionRotate);
			ActionBuildObject.SetVisible(cursorPassiveTooltip.ShowAction && _hasAction);
		}
		TooltipFollowMouse = cursorPassiveTooltip.FollowMouseMovement;
		if (!TooltipFollowMouse)
		{
			RectTransform.position = new Vector2(Screen.width, Screen.height) / 2f + _offset;
		}
	}

	private static string ColorToHex(Color32 color)
	{
		return $"{color.r:X2}{color.g:X2}{color.b:X2}";
	}

	public void DrawTooltip()
	{
		if ((!Dirty || !WantDraw) && Mode == TooltipMode.Hidden)
		{
			UiComponentRenderer.SetVisible(isVisble: false);
			return;
		}
		if (InventoryManager.Instance.UIProgressionBar.IsVisible || (!_hasTitle && !_hasState && !_hasAction && !_hasExtended))
		{
			Mode = TooltipMode.Hidden;
		}
		switch (Mode)
		{
		case TooltipMode.Hidden:
			ClearTooltip();
			UiComponentRenderer.SetVisible(isVisble: false);
			break;
		case TooltipMode.ActionFirst:
		case TooltipMode.ActionLast:
			TooltipTransform.anchorMin = _screenPosition;
			TooltipTransform.anchorMax = _screenPosition;
			break;
		}
		Dirty = false;
	}

	public static void SetValuesForInteractable(ref PassiveTooltip tooltip, Thing CursorThing, Interactable interactable)
	{
		Interaction interaction = new Interaction(InventoryManager.Parent, InventoryManager.ActiveHandSlot, CursorThing, KeyManager.GetButton(KeyMap.QuantityModifier));
		Thing.DelayedActionInstance delayedActionInstance = null;
		delayedActionInstance = ((!CursorThing.PreventInteraction(out var failResult, interactable, interaction)) ? CursorThing.InteractWith(interactable, interaction, doAction: false) : failResult);
		if (delayedActionInstance != null)
		{
			tooltip = new PassiveTooltip(delayedActionInstance, string.Empty, CursorThing);
			if ((delayedActionInstance != null && delayedActionInstance.IsDisabled) || !CursorThing.AllowInteraction)
			{
				tooltip.color = Color.red;
			}
			else if (InventoryManager.WillStackFromInteractable(interactable))
			{
				tooltip.color = Color.yellow;
			}
			else
			{
				tooltip.color = ((delayedActionInstance.Duration > 0f) ? Color.yellow : Color.green);
			}
		}
		if (delayedActionInstance != null && delayedActionInstance.SwitchTitleForTooltip)
		{
			tooltip.Title = interactable.ContextualName;
		}
	}

	public static Color SetColorForItemAction(ref PassiveTooltip tooltip, Thing cursorItem)
	{
		if (!(cursorItem is Item))
		{
			return tooltip.color;
		}
		if (InventoryManager.ActiveHandSlot.Contains<IMergeable>(out var occupant) && cursorItem is IMergeable mergeable && occupant.GetPrefabHash() == mergeable.GetPrefabHash() && !occupant.IsStackFull && occupant.CanStack(mergeable))
		{
			tooltip.color = Color.yellow;
			tooltip.Action = ActionStrings.Collect;
		}
		else if (InventoryManager.ActiveHandSlot.IsEmpty() && cursorItem.CanPickup)
		{
			tooltip.color = Color.green;
			tooltip.Action = ActionStrings.Pickup;
		}
		else
		{
			tooltip.color = Color.red;
		}
		return tooltip.color;
	}

	private void LateUpdate()
	{
		WantDraw = false;
		Dirty = false;
		Mode = TooltipMode.Hidden;
		if (base.gameObject.activeSelf && TooltipFollowMouse)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(screenPoint: new Vector3(Input.mousePosition.x - (float)_offsetX, Input.mousePosition.y - (float)_offsetY, Input.mousePosition.z), rect: (MainRectTransform == null) ? RectTransform : MainRectTransform, cam: null, localPoint: out var localPoint);
			base.transform.localPosition = localPoint;
		}
	}

	public static void AppendLine(string str)
	{
		if (!string.IsNullOrEmpty(str))
		{
			ToolTipStringBuilder.AppendLine(str);
		}
	}

	public static void SetProcessingText(DynamicThing importingThing, float completed)
	{
		if (importingThing is IQuantity quantity)
		{
			string arg = ExtensionMethods.ToString(quantity.GetQuantity, "yellow") + " x " + importingThing.ToTooltip();
			ToolTipStringBuilder.AppendLine(GameStrings.ProcessingThing.AsString(arg) + " " + completed.ToStringPercent("green"));
		}
		else
		{
			ToolTipStringBuilder.AppendLine(GameStrings.ProcessingThing.AsString(importingThing.ToTooltip()) + " " + completed.ToStringPercent("green"));
		}
	}

	public static void SetProcessingText(DynamicThing importingThing)
	{
		if (importingThing is IQuantity quantity)
		{
			string arg = ExtensionMethods.ToString(quantity.GetQuantity, "yellow") + " x " + importingThing.ToTooltip();
			ToolTipStringBuilder.AppendLine(GameStrings.ProcessingThing.AsString(arg) ?? "");
		}
		else
		{
			ToolTipStringBuilder.AppendLine(GameStrings.ProcessingThing.AsString(importingThing.ToTooltip()) ?? "");
		}
	}

	public void UpdateOpacity()
	{
		TooltipBackground.color = TooltipBackground.color.SetAlpha(Settings.CurrentData.TooltipOpacity);
	}
}
