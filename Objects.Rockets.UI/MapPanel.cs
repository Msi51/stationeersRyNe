using System;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Objects.Rockets.UI.Models;
using TMPro;
using UnityEngine;

namespace Objects.Rockets.UI;

public class MapPanel : UserInterfaceBase
{
	[Header("Map panel")]
	[SerializeField]
	private MapView _mapView;

	[SerializeField]
	private SelectedDestinationPanel _selectedDestinationPanel;

	[SerializeField]
	private GameObject _rocketNameObject;

	[SerializeField]
	private TextMeshProUGUI _rocketNameTextMesh;

	[SerializeField]
	private GameObject _errorText;

	[Space(15f)]
	[SerializeField]
	private BasicButton _reCenterButton;

	[SerializeField]
	private BasicButton _focusOnSelectedButton;

	[SerializeField]
	private BasicButton _clearDestinationButton;

	private MapLocation _selectedLocation;

	private RocketCanvas _rocketCanvas;

	private LocationPanel _locationPanel;

	private bool _initialized;

	public RocketMotherboard Motherboard { get; set; }

	public void Show(RocketCanvas rocketCanvas, LocationPanel locationPanel, RocketMotherboard motherboard)
	{
		Motherboard = motherboard;
		SetVisible(isVisble: true);
		_rocketCanvas = rocketCanvas;
		_locationPanel = locationPanel;
		_mapView.Initialize(this);
		if (!_initialized)
		{
			RecenterAndClearSelection();
		}
		Refresh();
		_initialized = true;
	}

	public void Hide()
	{
		SetVisible(isVisble: false);
		Clear();
	}

	public void AddDynamicNodeToSpaceMap(SpaceMapNode parent, SpaceMapNode child)
	{
		_mapView.AddDynamicNodeToSpaceMap(parent, child);
	}

	public void SetSelectedRocketNameText(string name)
	{
		_rocketNameTextMesh.text = name;
	}

	public void LocationSelected(MapLocation location)
	{
		_selectedDestinationPanel.Show(location, this);
		_selectedLocation = location;
		_locationPanel.CurrentNode = location.Node;
	}

	public void SetDestination()
	{
		if (_selectedLocation?.Node != null && _selectedLocation.Node.IsCharted)
		{
			_mapView.SetTargetDestination(_selectedLocation.Node);
		}
	}

	public void ClearDestination()
	{
		_mapView.SetTargetDestination(null);
	}

	public void ClickLocationInfo()
	{
		if (_selectedLocation?.Node != null)
		{
			_rocketCanvas.ShowLocationPanel();
		}
	}

	public void RemoveNode()
	{
		if (_selectedLocation?.Node != null)
		{
			Motherboard.RemoveSpaceMapNode(_selectedLocation.Node);
			_mapView.ClearSelected();
		}
	}

	public void RemoveNodesOfType()
	{
		if (_selectedLocation?.Node != null)
		{
			Motherboard.RemoveSpaceMapNodesOfType(_selectedLocation.Node);
			_mapView.ClearSelected();
		}
	}

	public void ClearLocationSelection()
	{
		_selectedDestinationPanel.Hide();
		_selectedLocation = null;
		_locationPanel.CurrentNode = null;
	}

	public void Clear()
	{
		_mapView.Clear();
		Motherboard = null;
	}

	public void ClearAll()
	{
		_mapView.ClearAll();
	}

	public void Refresh(RocketUIModel model)
	{
		Refresh();
		_clearDestinationButton.SetVisible(model.MapPanelModel.CurrentRocketHasDestination);
	}

	public void Refresh()
	{
		if (IsVisible)
		{
			_mapView.Refresh();
			CheckForErrors();
			_selectedDestinationPanel.Refresh();
			RocketAvionicsDevice rocketAvionicsDevice = Motherboard?.SelectedRocket()?.Avionics;
			_focusOnSelectedButton.gameObject.SetActive(rocketAvionicsDevice);
			_clearDestinationButton.SetInteractable((bool)rocketAvionicsDevice && rocketAvionicsDevice.GetTarget() != null);
			_rocketNameObject.SetActive(rocketAvionicsDevice);
		}
	}

	private void RecenterAndClearSelection()
	{
		_mapView.RecenterMap();
		_mapView.ClearSelected();
	}

	private void CheckForErrors()
	{
		RocketAvionicsDevice rocketAvionicsDevice = Motherboard?.SelectedRocket()?.Avionics;
		if (!rocketAvionicsDevice || !rocketAvionicsDevice.OnOff)
		{
			_errorText.SetActive(value: true);
		}
		else
		{
			_errorText.SetActive(value: false);
		}
	}

	private void OnClearDestination()
	{
		ClearDestination();
	}

	private void OnReCenter()
	{
		_mapView.RecenterMap();
	}

	private void OnFocusSelectedRocket()
	{
		_mapView.FocusOnSelectedRocket();
	}

	private new void OnEnable()
	{
		BasicButton reCenterButton = _reCenterButton;
		reCenterButton.OnClick = (Action)Delegate.Combine(reCenterButton.OnClick, new Action(OnReCenter));
		BasicButton clearDestinationButton = _clearDestinationButton;
		clearDestinationButton.OnClick = (Action)Delegate.Combine(clearDestinationButton.OnClick, new Action(OnClearDestination));
		BasicButton focusOnSelectedButton = _focusOnSelectedButton;
		focusOnSelectedButton.OnClick = (Action)Delegate.Combine(focusOnSelectedButton.OnClick, new Action(OnFocusSelectedRocket));
	}

	private new void OnDisable()
	{
		BasicButton reCenterButton = _reCenterButton;
		reCenterButton.OnClick = (Action)Delegate.Remove(reCenterButton.OnClick, new Action(OnReCenter));
		BasicButton clearDestinationButton = _clearDestinationButton;
		clearDestinationButton.OnClick = (Action)Delegate.Remove(clearDestinationButton.OnClick, new Action(OnClearDestination));
		BasicButton focusOnSelectedButton = _focusOnSelectedButton;
		focusOnSelectedButton.OnClick = (Action)Delegate.Remove(focusOnSelectedButton.OnClick, new Action(OnFocusSelectedRocket));
	}
}
