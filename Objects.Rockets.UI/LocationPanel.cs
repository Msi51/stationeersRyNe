using System.Collections.Generic;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using TMPro;
using TraderUI;
using UnityEngine;

namespace Objects.Rockets.UI;

public class LocationPanel : UserInterfaceBase
{
	[Space(15f)]
	[Header("Location Panel")]
	[SerializeField]
	private TextMeshProUGUI _locationNameText;

	[Space(15f)]
	[SerializeField]
	private LocationProgressPanel _progressPanel;

	[SerializeField]
	private LocationActionsPanel _actionsPanel;

	[SerializeField]
	private DifficultyPanel _difficultyPanel;

	[SerializeField]
	private LocationInfoPanel _locationInfoPanel;

	[SerializeField]
	private ScanInfoPanel _scanInfoPanel;

	[SerializeField]
	private LocationResourcesPanel _resourcesPanel;

	[SerializeField]
	private LocationSiteDiscoveryPanel _siteDiscoveryPanel;

	[SerializeField]
	private LocationNavPointsPanel _navPointsPanel;

	[SerializeField]
	private SiteSurveyTargetPanel _siteSurveyTargetPanel;

	[Space(15f)]
	[SerializeField]
	private GameObject _tabPanels;

	[SerializeField]
	private GameObject _errorPanel;

	[Space(15f)]
	[SerializeField]
	private TabWell _tabWell;

	[SerializeField]
	private TMP_Dropdown _rocketDropdown;

	private const int RESOURCES_TAB_INDEX = 0;

	private const int SITE_DISCOVERY_TAB_INDEX = 1;

	private const int NAV_POINTS_TAB_INDEX = 2;

	public RocketMotherboard Motherboard { get; set; }

	public SpaceMapNode CurrentNode { get; set; }

	public bool OpenCurrentRocketNode { get; set; } = true;

	public void Show(RocketMotherboard motherboard)
	{
		SetVisible(isVisble: true);
		Motherboard = motherboard;
		_progressPanel.Initialize(this);
		_actionsPanel.Initialize(this);
		_difficultyPanel.Initialize(this);
		_locationInfoPanel.Initialize(this);
		_scanInfoPanel.Initialize(this);
		if (OpenCurrentRocketNode || CurrentNode == null)
		{
			SelectCurrentNode(motherboard.SelectedRocket());
		}
		SetDropdownValues();
		SelectCurrentRocket(motherboard.SelectedRocket());
		_locationNameText.text = CurrentNode.DisplayName;
		EnableDisableTabs();
		_tabWell.EnsureEnabledTabIsSelected();
		OpenCurrentRocketNode = true;
	}

	private void SelectCurrentNode(ConnectedRocketInfo currentRocket)
	{
		SpaceMapNode spaceMapNode = ((currentRocket != null && currentRocket.Avionics != null) ? currentRocket.Avionics.GetCurrentNode() : null);
		if (spaceMapNode != null)
		{
			CurrentNode = spaceMapNode;
			return;
		}
		SpaceMap current = SpaceMap.Current;
		CurrentNode = current.Nodes[0];
	}

	private void SelectCurrentRocket(ConnectedRocketInfo currentRocket)
	{
		int valueWithoutNotify = 0;
		if (currentRocket != null && currentRocket.IsValid)
		{
			for (int i = 0; i < _rocketDropdown.options.Count; i++)
			{
				if (_rocketDropdown.options[i] is RocketDropdownOption rocketDropdownOption && rocketDropdownOption.RocketInfo.Equals(currentRocket))
				{
					valueWithoutNotify = i;
					break;
				}
			}
		}
		RocketChangedRefresh();
		_rocketDropdown.SetValueWithoutNotify(valueWithoutNotify);
	}

	public void Hide()
	{
		SetVisible(isVisble: false);
		Clear();
	}

	public void Refresh()
	{
		if (IsVisible)
		{
			_progressPanel.Refresh();
			_actionsPanel.Refresh();
			_difficultyPanel.Refresh();
			_locationInfoPanel.Refresh();
			_scanInfoPanel.Refresh();
			_resourcesPanel.Refresh();
			_siteDiscoveryPanel.Refresh();
			_navPointsPanel.Refresh();
			_siteSurveyTargetPanel.Refresh();
		}
	}

	public void Clear()
	{
		_progressPanel.Clear();
		_actionsPanel.Clear();
		_difficultyPanel.Clear();
		_locationInfoPanel.Clear();
		_scanInfoPanel.Clear();
		_resourcesPanel.Clear();
		_siteDiscoveryPanel.Clear();
		_navPointsPanel.Clear();
		Motherboard = null;
	}

	private void SetDropdownValues()
	{
		List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
		list.Add(new RocketDropdownOption(ConnectedRocketInfo.Invalid, "None"));
		if (Motherboard.ConnectedRockets != null)
		{
			ConnectedRocketInfo[] connectedRockets = Motherboard.ConnectedRockets;
			foreach (ConnectedRocketInfo connectedRocketInfo in connectedRockets)
			{
				if ((bool)connectedRocketInfo.Avionics && connectedRocketInfo.Avionics.GetCurrentNode() == CurrentNode)
				{
					string rocketDropdownText = GetRocketDropdownText(connectedRocketInfo);
					list.Add(new RocketDropdownOption(connectedRocketInfo, rocketDropdownText));
				}
			}
		}
		foreach (Rocket allRocket in Rocket.AllRockets)
		{
			if (allRocket == null)
			{
				continue;
			}
			bool flag = false;
			if (Motherboard.ConnectedRockets != null)
			{
				ConnectedRocketInfo[] connectedRockets = Motherboard.ConnectedRockets;
				for (int i = 0; i < connectedRockets.Length; i++)
				{
					if (connectedRockets[i].Avionics?.Rocket?.ReferenceId == allRocket?.ReferenceId)
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				_ = allRocket.CurrentNode;
				_ = CurrentNode;
			}
		}
		_rocketDropdown.options = list;
	}

	public void RocketChangedRefresh()
	{
		RocketDropdownOption selected = GetSelected();
		SetDropdownValues();
		int selectedIndex = GetSelectedIndex(selected);
		_rocketDropdown.SetValueWithoutNotify(selectedIndex);
	}

	private RocketDropdownOption GetSelected()
	{
		if (_rocketDropdown.options != null && _rocketDropdown.value < _rocketDropdown.options.Count)
		{
			return _rocketDropdown.options[_rocketDropdown.value] as RocketDropdownOption;
		}
		return null;
	}

	private int GetSelectedIndex(RocketDropdownOption selected)
	{
		if (selected != null)
		{
			for (int i = 0; i < _rocketDropdown.options.Count; i++)
			{
				if (_rocketDropdown.options[i] is RocketDropdownOption rocketDropdownOption && rocketDropdownOption.RocketInfo.Equals(selected.RocketInfo))
				{
					return i;
				}
			}
		}
		return 0;
	}

	public void RefreshRocketLocation()
	{
		if (IsVisible)
		{
			RocketDropdownOption selected = GetSelected();
			SetDropdownValues();
			int selectedIndex = GetSelectedIndex(selected);
			_rocketDropdown.SetValueWithoutNotify(selectedIndex);
			RocketSelected(selectedIndex);
		}
	}

	private string GetRocketDropdownText(ConnectedRocketInfo rocketInfo)
	{
		if (!rocketInfo.Avionics)
		{
			return "None";
		}
		string text = ((rocketInfo.Avionics.GetCurrentNode() != CurrentNode) ? " (Not at this location)" : string.Empty);
		return rocketInfo.Avionics.Rocket.DisplayName + text;
	}

	private void EnableDisableTabs()
	{
		bool flag = CurrentNode.Deposit != null;
		bool flag2 = CurrentNode.DiscoverData != null;
		bool flag3 = CurrentNode.ChartData != null;
		_tabWell.SetEnabled(0, flag);
		_tabWell.SetEnabled(1, flag2);
		_tabWell.SetEnabled(2, flag3);
		bool flag4 = !(flag || flag2 || flag3);
		_tabPanels.SetActive(!flag4);
		_errorPanel.SetActive(flag4);
	}

	private void TabSelected(int index)
	{
		_resourcesPanel.Hide();
		_siteDiscoveryPanel.Hide();
		_navPointsPanel.Hide();
		_siteSurveyTargetPanel.Hide();
		switch (index)
		{
		case 0:
			_resourcesPanel.Show(this);
			_siteSurveyTargetPanel.Show(this);
			break;
		case 1:
			_siteDiscoveryPanel.Show(this);
			break;
		case 2:
			_navPointsPanel.Show(this);
			break;
		}
	}

	private void RocketSelected(int index)
	{
		RocketDropdownOption selected = GetSelected();
		Motherboard.RocketSelected(selected.RocketInfo);
		RocketChangedRefresh();
	}

	private void Awake()
	{
		_tabWell.TabSelected.AddListener(TabSelected);
		_rocketDropdown.onValueChanged.AddListener(RocketSelected);
	}

	private void OnDestroy()
	{
		_tabWell.TabSelected.RemoveListener(TabSelected);
		_rocketDropdown.onValueChanged.RemoveListener(RocketSelected);
	}
}
