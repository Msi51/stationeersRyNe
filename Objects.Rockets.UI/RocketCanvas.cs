using Assets.Scripts;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Objects.Rockets.UI.Models;
using TraderUI;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;

namespace Objects.Rockets.UI;

public class RocketCanvas : InputWindowBase, IModal
{
	public static RocketCanvas Instance;

	[Header("Rocket Canvas")]
	[SerializeField]
	private GameObject _rootObject;

	[SerializeField]
	private Transform _mainPanel;

	[Space(15f)]
	[SerializeField]
	private RocketPanel _rocketPanel;

	[SerializeField]
	public MapPanel _mapPanel;

	[SerializeField]
	private LocationPanel _locationPanel;

	[Space(15f)]
	[SerializeField]
	private TabWell _tabWell;

	[SerializeField]
	private Button _closeButton;

	private const float PREFERRED_PANEL_WIDTH = 1950f;

	public RocketMotherboard CurrentMotherboard { get; set; }

	public bool UnlockCursor => true;

	public override void Initialize()
	{
		base.Initialize();
		Instance = this;
		_rootObject.SetActive(value: true);
		SetVisible(isVisble: false);
		_rocketPanel.Initialize();
	}

	public void RefreshMap()
	{
		if (_mapPanel.IsVisible)
		{
			_mapPanel.Clear();
			_mapPanel.Show(this, _locationPanel, CurrentMotherboard);
		}
	}

	private void SetPanelScale()
	{
		float num = Screen.width;
		float num2 = 1950f * Transform.localScale.x;
		if (num < num2)
		{
			int num3 = 50;
			float num4 = (num - (float)num3) / num2;
			_mainPanel.localScale = Vector3.one * num4;
		}
		else
		{
			_mainPanel.localScale = Vector3.one;
		}
	}

	public void Show(RocketMotherboard motherboard)
	{
		CurrentMotherboard = motherboard;
		CursorManager.Instance.BlockCursorRaycast = true;
		SetVisible(isVisble: true);
		SetPanelScale();
		_tabWell.Select(0);
		MouseModeController.AddModal(this);
	}

	public void ClearAll()
	{
		_tabWell.Select(0);
		_mapPanel.ClearAll();
	}

	public void RefreshRocketLocation()
	{
		if (IsVisible)
		{
			_locationPanel.RefreshRocketLocation();
		}
	}

	public void RocketChangedRefresh()
	{
		if (IsVisible)
		{
			Hide();
		}
	}

	public void AddDynamicNodeToSpaceMap(SpaceMapNode parent, SpaceMapNode child)
	{
		if (IsVisible)
		{
			_mapPanel.AddDynamicNodeToSpaceMap(parent, child);
		}
	}

	public void RefreshUI(RocketUIModel model)
	{
		bool isValid = model.RocketPanelModel.SelectedRocketModel.IsValid;
		if (!isValid && _tabWell.SelectedIndex == 0)
		{
			_tabWell.Select(1);
		}
		_tabWell.SetEnabled(0, isValid);
		_rocketPanel.Refresh(model);
		_mapPanel.Refresh(model);
		_locationPanel.Refresh();
	}

	private void Clear()
	{
		_mapPanel.Clear();
		_locationPanel.Clear();
	}

	protected override void CloseOnClientConnected(bool isPaused, string message)
	{
		if (IsVisible && isPaused)
		{
			Hide();
		}
	}

	public void Hide()
	{
		CursorManager.Instance.BlockCursorRaycast = false;
		SetVisible(isVisble: false);
		Clear();
		CurrentMotherboard = null;
		PanelToolTip.Instance.ClearToolTip();
		UITooltipCanvas.Instance.HideTooltip();
		MouseModeController.RemoveModal(this);
	}

	public void ShowLocationPanel()
	{
		_locationPanel.OpenCurrentRocketNode = false;
		_tabWell.Select(2);
	}

	private void TabSelected(int index)
	{
		if (IsVisible)
		{
			_mapPanel.Hide();
			_rocketPanel.Hide();
			_locationPanel.Hide();
			switch (index)
			{
			case 0:
				_rocketPanel.Show(CurrentMotherboard);
				break;
			case 1:
				_mapPanel.Show(this, _locationPanel, CurrentMotherboard);
				break;
			case 2:
				_locationPanel.Show(CurrentMotherboard);
				break;
			}
		}
	}

	private new void OnEnable()
	{
		_tabWell.TabSelected.AddListener(TabSelected);
		_closeButton.onClick.AddListener(Hide);
	}

	private new void OnDisable()
	{
		_tabWell.TabSelected.RemoveListener(TabSelected);
		_closeButton.onClick.RemoveListener(Hide);
	}

	private void RefreshUI()
	{
		if (RocketUIModelBuilder.Build(CurrentMotherboard, out var model))
		{
			Instance.RefreshUI(model);
		}
		else
		{
			Instance.Hide();
		}
	}

	private void Update()
	{
		if (!GameManager.IsBatchMode && (bool)Instance)
		{
			UITooltipCanvas.Instance.DoUpdate();
			if (Instance.IsVisible)
			{
				RefreshUI();
			}
		}
	}
}
