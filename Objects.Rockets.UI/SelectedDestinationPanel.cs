using System;
using Assets.Scripts.Localization2;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using TMPro;
using UI;
using UI.Tooltips;
using UnityEngine;

namespace Objects.Rockets.UI;

public class SelectedDestinationPanel : UserInterfaceBase
{
	[SerializeField]
	private TextMeshProUGUI _destinationNameTextMesh;

	[SerializeField]
	private TextMeshProUGUI _codeTextMesh;

	[Space(15f)]
	[Header("Panel Buttons")]
	[SerializeField]
	private BasicButton _setDestinationButton;

	[SerializeField]
	private BasicButton _locationInfoButton;

	[SerializeField]
	private BasicButton _removeNodeButton;

	[SerializeField]
	private BasicButton _removeNodesOfTypeButton;

	private MapPanel _mapPanel;

	private MapLocation _location;

	public void Show(MapLocation location, MapPanel mapPanel)
	{
		SetVisible(isVisble: true);
		_location = location;
		_mapPanel = mapPanel;
		_destinationNameTextMesh.text = location.Node.DisplayName;
		_codeTextMesh.text = (location.Node.IsCharted ? ((string)location.Node.Code) : string.Empty);
		_setDestinationButton.SetInteractable(location.Node.IsCharted && !IsInvalidMannedDestination());
		_locationInfoButton.SetInteractable(location.Node.IsCharted);
	}

	public void Refresh()
	{
		if (IsVisible && (bool)_location)
		{
			bool interactable = _location.Node.NodeType == NodeType.Generated && _location.Node.RocketsHere.Count == 0 && !AnyRocketsTargetingNode(_location.Node);
			_removeNodeButton.SetInteractable(interactable);
			_removeNodesOfTypeButton.SetInteractable(interactable);
			_setDestinationButton.SetInteractable((bool)_mapPanel.Motherboard?.SelectedRocket()?.Avionics && !IsInvalidMannedDestination());
		}
	}

	private bool IsInvalidMannedDestination()
	{
		Rocket rocket = _mapPanel?.Motherboard?.SelectedRocket()?.Avionics?.Rocket;
		if (rocket != null && rocket.IsManned)
		{
			return !Rocket.IsValidMannedTarget(_location?.Node);
		}
		return false;
	}

	private void Update()
	{
		if (IsVisible && (bool)_location && _setDestinationButton.Hovered && IsInvalidMannedDestination())
		{
			UITooltipManager.SetTooltip(GameStrings.RocketCrewModuleMannedTravel.DisplayString);
		}
	}

	private bool AnyRocketsTargetingNode(SpaceMapNode node)
	{
		foreach (Rocket allRocket in Rocket.AllRockets)
		{
			if (allRocket.TargetNode == node)
			{
				return true;
			}
		}
		return false;
	}

	public void Hide()
	{
		SetVisible(isVisble: false);
	}

	private void OnSetDestination()
	{
		_mapPanel.SetDestination();
	}

	private void OnClickLocationInfo()
	{
		_mapPanel.ClickLocationInfo();
	}

	private void OnClickRemoveNode()
	{
		Singleton<ConfirmationPanel>.Instance.Show("RemoveNodeButton", "RemoveNodeText", "RemoveNodeConfirm", RemoveNode, "RemoveNodeCancel");
	}

	private void OnClickRemoveNodesOfType()
	{
		Singleton<ConfirmationPanel>.Instance.Show("RemoveNodesOfTypeButton", "RemoveNodesOfTypeText", "RemoveNodeConfirm", RemoveNodesOfType, "RemoveNodeCancel");
	}

	private void RemoveNode()
	{
		_mapPanel.RemoveNode();
	}

	private void RemoveNodesOfType()
	{
		_mapPanel.RemoveNodesOfType();
	}

	private new void OnEnable()
	{
		BasicButton setDestinationButton = _setDestinationButton;
		setDestinationButton.OnClick = (Action)Delegate.Combine(setDestinationButton.OnClick, new Action(OnSetDestination));
		BasicButton locationInfoButton = _locationInfoButton;
		locationInfoButton.OnClick = (Action)Delegate.Combine(locationInfoButton.OnClick, new Action(OnClickLocationInfo));
		BasicButton removeNodeButton = _removeNodeButton;
		removeNodeButton.OnClick = (Action)Delegate.Combine(removeNodeButton.OnClick, new Action(OnClickRemoveNode));
		BasicButton removeNodesOfTypeButton = _removeNodesOfTypeButton;
		removeNodesOfTypeButton.OnClick = (Action)Delegate.Combine(removeNodesOfTypeButton.OnClick, new Action(OnClickRemoveNodesOfType));
	}

	private new void OnDisable()
	{
		BasicButton setDestinationButton = _setDestinationButton;
		setDestinationButton.OnClick = (Action)Delegate.Remove(setDestinationButton.OnClick, new Action(OnSetDestination));
		BasicButton locationInfoButton = _locationInfoButton;
		locationInfoButton.OnClick = (Action)Delegate.Remove(locationInfoButton.OnClick, new Action(OnClickLocationInfo));
		BasicButton removeNodeButton = _removeNodeButton;
		removeNodeButton.OnClick = (Action)Delegate.Remove(removeNodeButton.OnClick, new Action(OnClickRemoveNode));
		BasicButton removeNodesOfTypeButton = _removeNodesOfTypeButton;
		removeNodesOfTypeButton.OnClick = (Action)Delegate.Remove(removeNodesOfTypeButton.OnClick, new Action(OnClickRemoveNodesOfType));
	}
}
