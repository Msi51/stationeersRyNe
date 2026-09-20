using System.Collections.Generic;
using System.Text.RegularExpressions;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI.Genetics;
using Objects.Rockets.UI;
using TMPro;
using TraderUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI.HelperHints;

public class HelperHintsTextController : UserInterfaceBase, IPointerClickHandler, IEventSystemHandler, IPointerDownHandler, IPointerUpHandler, IScreenSpaceTooltip
{
	private static HelperHintsTextController _instance;

	private static List<HelperHintViewModel> _viewModels = new List<HelperHintViewModel>(64);

	private static HelperHintBuilder _helperHintBuilder = new HelperHintBuilder();

	private static string _helperHintText;

	private static bool _isDirty = true;

	private bool _panelIsHovered;

	private TMP_LinkInfo _hoveredLink;

	private int _hoveredLinkIndex = -1;

	private string _hoveredLinkId;

	private bool _hintsTooltipIsDisplayed;

	public TextMeshProUGUI TextMesh;

	[Space(15f)]
	[SerializeField]
	private RectTransform _mainPanelTransform;

	[SerializeField]
	private GameObject _mainPanelGameObject;

	[SerializeField]
	private RectTransform _headerRectTransform;

	[SerializeField]
	private RectTransform _contentRectTransform;

	[Space(15f)]
	[SerializeField]
	private float _maxPanelHeight;

	[Space(15f)]
	[SerializeField]
	private Button _toggleButton;

	[SerializeField]
	private Button _showDismissedButton;

	[SerializeField]
	private Button _dismissCompletedButton;

	[SerializeField]
	private Toggle _autoExpandToggle;

	private bool _isExpanded = true;

	private const string THING_PATTERN = "^Thing";

	private bool IsHoveringLink
	{
		get
		{
			if (_panelIsHovered && _hoveredLinkIndex >= 0)
			{
				return _hoveredLinkIndex < TextMesh.textInfo.linkCount;
			}
			return false;
		}
	}

	private bool IsExpanded
	{
		get
		{
			return _isExpanded;
		}
		set
		{
			if (_isExpanded != value)
			{
				_isDirty = true;
				if (!value && _isExpanded)
				{
					ClearCursorHover();
					_panelIsHovered = false;
				}
			}
			_isExpanded = value;
		}
	}

	private static bool CanDisplayTooltips
	{
		get
		{
			if (!WorldManager.IsGamePaused && !GameManager.IsBatchMode && !Stationpedia.IsOpen && !InventoryManager.Instance.InGameMenuOpen)
			{
				return !AnyTooltipBlockingPanelIsOpen;
			}
			return false;
		}
	}

	private static bool AnyTooltipBlockingPanelIsOpen
	{
		get
		{
			if (!InputWindow.Instance.IsVisible && !InputSourceCode.Instance.IsVisible && !InputPrefabs.Instance.IsVisible && !CommsTerminal.Instance.IsVisible && !InputKeyWindow.Instance.IsVisible && !InputAxisWindow.Instance.IsVisible && !PanelPlantGenetics.Instance.IsVisible && !TraderCanvas.Instance.IsVisible)
			{
				return RocketCanvas.Instance.IsVisible;
			}
			return true;
		}
	}

	public bool TooltipIsVisible => IsHoveringLink;

	public static void InitializePanel()
	{
		_instance._autoExpandToggle.isOn = Settings.CurrentData.AutoExpandHelperHints;
	}

	private void Awake()
	{
		_instance = this;
		_toggleButton.onClick.AddListener(ToggleMainPanel);
		_showDismissedButton.onClick.AddListener(ShowDismissed);
		_dismissCompletedButton.onClick.AddListener(DismissCompleted);
		_autoExpandToggle.onValueChanged.AddListener(AutoExpand);
	}

	private void OnDestroy()
	{
		_toggleButton.onClick.RemoveListener(ToggleMainPanel);
		_showDismissedButton.onClick.RemoveListener(ShowDismissed);
		_dismissCompletedButton.onClick.RemoveListener(DismissCompleted);
		_autoExpandToggle.onValueChanged.RemoveListener(AutoExpand);
	}

	public static void SetWorldObjectives(List<WorldObjectiveState> worldObjectiveStates)
	{
		_viewModels.Clear();
		foreach (WorldObjectiveState worldObjectiveState in worldObjectiveStates)
		{
			_viewModels.Add(new HelperHintViewModel(worldObjectiveState));
		}
	}

	public static void ClearAll()
	{
		_viewModels.Clear();
		_helperHintText = string.Empty;
		_instance?.ClearCursorHover();
	}

	private void DismissCompleted()
	{
		HelperHintsManager.DismissCompleted();
	}

	private void ShowDismissed()
	{
		HelperHintsManager.UnDismissAll();
	}

	private void AutoExpand(bool value)
	{
		Settings.CurrentData.AutoExpandHelperHints = value;
		Settings.CurrentData.Save();
	}

	public static void ToggleMainPanel()
	{
		_instance?.SetPanelState(!_instance._mainPanelGameObject.activeInHierarchy);
	}

	public static void RefreshDisplayState(bool isLoadingWorld = false)
	{
		_instance?.SetPanelState(_instance._isExpanded || (isLoadingWorld && GameManager.IsNewTutorial));
	}

	private void SetPanelState(bool expanded)
	{
		bool helperHintsEnabled = GameManager.HelperHintsEnabled;
		_instance.IsExpanded = expanded;
		_instance._toggleButton.gameObject.SetActive(helperHintsEnabled);
		_instance._mainPanelGameObject.SetActive(helperHintsEnabled && _instance.IsExpanded);
		_instance._autoExpandToggle.isOn = Settings.CurrentData.AutoExpandHelperHints;
		if (helperHintsEnabled)
		{
			_instance.RefreshPanelSize();
		}
	}

	private void RefreshPanelSize()
	{
		float y = _headerRectTransform.sizeDelta.y;
		float size = Mathf.Min(TextMesh.preferredHeight + y, _maxPanelHeight);
		_mainPanelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		_contentRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, TextMesh.preferredHeight);
	}

	private void Update()
	{
		if (GameManager.GameState == GameState.Running && IsExpanded)
		{
			if (TextNeedsRebuild())
			{
				UpdateTextContent();
				RefreshPanelSize();
			}
			UpdateCursorHover();
			UpdateTooltip();
		}
	}

	private bool TextNeedsRebuild()
	{
		if (_isDirty)
		{
			return true;
		}
		if (string.IsNullOrWhiteSpace(_helperHintText))
		{
			return _isDirty = true;
		}
		if (_viewModels != null)
		{
			foreach (HelperHintViewModel viewModel in _viewModels)
			{
				if (viewModel.IsDirty())
				{
					return _isDirty = true;
				}
			}
		}
		return false;
	}

	private void UpdateTextContent()
	{
		_helperHintBuilder.Clear();
		if (_viewModels != null)
		{
			foreach (HelperHintViewModel viewModel in _viewModels)
			{
				_helperHintBuilder.Append(viewModel);
				viewModel.SetDirty(value: false);
			}
		}
		TextMesh.text = (_helperHintText = _helperHintBuilder.Build());
		_isDirty = false;
	}

	private void UpdateCursorHover()
	{
		Vector3 mousePosition = Input.mousePosition;
		_panelIsHovered = TMP_TextUtilities.IsIntersectingRectTransform(_mainPanelTransform, mousePosition, null);
		if (_panelIsHovered)
		{
			_hoveredLinkIndex = TMP_TextUtilities.FindIntersectingLink(TextMesh, mousePosition, null);
			if (_hoveredLinkIndex >= 0)
			{
				_hoveredLink = TextMesh.textInfo.linkInfo[_hoveredLinkIndex];
				_hoveredLinkId = _hoveredLink.GetLinkID();
				return;
			}
		}
		ClearCursorHover();
	}

	private void ClearCursorHover()
	{
		if (_hoveredLinkIndex >= 0)
		{
			ClearTooltip();
		}
		_hoveredLinkIndex = -1;
		_hoveredLink = default(TMP_LinkInfo);
		_hoveredLinkId = null;
	}

	private void UpdateTooltip()
	{
		if (!CanDisplayTooltips || !IsHoveringLink)
		{
			ClearTooltip();
			return;
		}
		if (_hoveredLinkId.StartsWith("dismiss:"))
		{
			foreach (HelperHintViewModel viewModel in _viewModels)
			{
				if (viewModel.DismissId == _hoveredLinkId)
				{
					DisplayTooltip(viewModel.Dismissed ? GameStrings.HelperHintRestore : GameStrings.HelperHintDismiss);
					break;
				}
			}
			return;
		}
		if (_hoveredLinkId.StartsWith("expand:"))
		{
			foreach (HelperHintViewModel viewModel2 in _viewModels)
			{
				if (viewModel2.ExpandId == _hoveredLinkId)
				{
					DisplayTooltip(viewModel2.Expanded ? GameStrings.HelperHintCollapse : GameStrings.HelperHintExpand);
					break;
				}
			}
			return;
		}
		DisplayTooltip(GameStrings.HelperHintOpenStationpedia);
	}

	private void DisplayTooltip(string text)
	{
		PanelToolTip.Instance.SetUpTooltip(text, string.Empty, this);
		_hintsTooltipIsDisplayed = true;
	}

	private void ClearTooltip()
	{
		if (_hintsTooltipIsDisplayed)
		{
			PanelToolTip.Instance.ClearToolTip();
		}
		_hintsTooltipIsDisplayed = false;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (!IsHoveringLink || HandleLinkClicks(_hoveredLinkId))
		{
			return;
		}
		if (_hoveredLinkId == "Clipboard")
		{
			if (!Stationpedia.Instance.BaseAnimator.GetBool("Copied"))
			{
				Stationpedia.Instance.BaseAnimator.SetBool("Copied", value: true);
			}
			GameManager.Clipboard = _hoveredLink.GetLinkText();
		}
		else
		{
			Stationpedia.OpenAt(FormatLink(_hoveredLinkId));
		}
	}

	private string FormatLink(string input)
	{
		if (!Regex.IsMatch(input, "^Thing"))
		{
			return "Thing" + input;
		}
		return input;
	}

	private bool HandleLinkClicks(string linkId)
	{
		foreach (HelperHintViewModel viewModel in _viewModels)
		{
			if (viewModel.DismissId == linkId)
			{
				viewModel.Dismiss();
				return true;
			}
			if (viewModel.ExpandId == linkId)
			{
				viewModel.ToggleExpanded();
				return true;
			}
		}
		return false;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
	}

	public void OnPointerUp(PointerEventData eventData)
	{
	}

	public void DoUpdate()
	{
	}
}
