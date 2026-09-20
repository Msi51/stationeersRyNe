using System;
using Assets.Scripts.UI;
using UnityEngine;

namespace Objects.Rockets.UI;

public class LocationActionsPanel : UserInterfaceBase
{
	[Space(15f)]
	[Header("Location Actions")]
	[SerializeField]
	private RocketActionButton _mineButton;

	[SerializeField]
	private RocketActionButton _discoverButton;

	[SerializeField]
	private RocketActionButton _chartButton;

	[SerializeField]
	private RocketActionButton _surveyButton;

	[SerializeField]
	private RocketActionButton _deployButton;

	[SerializeField]
	private RocketActionButton _surfaceScanButton;

	[SerializeField]
	private RocketActionButton _transferButton;

	[SerializeField]
	private RocketActionButton _idleButton;

	private LocationPanel _locationPanel;

	public void Initialize(LocationPanel locationPanel)
	{
		_locationPanel = locationPanel;
	}

	public void Refresh()
	{
		RocketAvionicsDevice rocketAvionicsDevice = _locationPanel.Motherboard?.SelectedRocket()?.Avionics;
		if (!rocketAvionicsDevice || rocketAvionicsDevice.GetCurrentNode() == null || rocketAvionicsDevice.Rocket.Progress != 0f || rocketAvionicsDevice.Rocket.RocketState != RocketState.InSpace)
		{
			_mineButton.Button.SetInteractable(interactable: false);
			_discoverButton.Button.SetInteractable(interactable: false);
			_chartButton.Button.SetInteractable(interactable: false);
			_surveyButton.Button.SetInteractable(interactable: false);
			_deployButton.Button.SetInteractable(interactable: false);
			_surfaceScanButton.Button.SetInteractable(interactable: false);
			_idleButton.Button.SetInteractable(interactable: false);
			_transferButton.Button.SetInteractable(interactable: false);
			_idleButton.Button.SetInteractable(rocketAvionicsDevice);
		}
		else
		{
			SpaceMapNode currentNode = rocketAvionicsDevice.GetCurrentNode();
			_mineButton.Button.SetInteractable(currentNode.IsActionAvailable(RocketMode.Mine));
			_discoverButton.Button.SetInteractable(currentNode.IsActionAvailable(RocketMode.Discover));
			_chartButton.Button.SetInteractable(currentNode.IsActionAvailable(RocketMode.Chart));
			_surveyButton.Button.SetInteractable(currentNode.IsActionAvailable(RocketMode.Survey));
			_deployButton.Button.SetInteractable(currentNode.IsActionAvailable(RocketMode.Deploy));
			_surfaceScanButton.Button.SetInteractable(currentNode.IsActionAvailable(RocketMode.SurfaceScan));
			_transferButton.Button.SetInteractable(currentNode.IsActionAvailable(RocketMode.Transfer));
			_idleButton.Button.SetInteractable(interactable: true);
		}
		RocketMode valueOrDefault = (rocketAvionicsDevice?.Rocket?.RocketMode).GetValueOrDefault();
		_mineButton.ShowSelectionOutline(valueOrDefault == RocketMode.Mine);
		_discoverButton.ShowSelectionOutline(valueOrDefault == RocketMode.Discover);
		_chartButton.ShowSelectionOutline(valueOrDefault == RocketMode.Chart);
		_surveyButton.ShowSelectionOutline(valueOrDefault == RocketMode.Survey);
		_deployButton.ShowSelectionOutline(valueOrDefault == RocketMode.Deploy);
		_surfaceScanButton.ShowSelectionOutline(valueOrDefault == RocketMode.SurfaceScan);
		_transferButton.ShowSelectionOutline(valueOrDefault == RocketMode.Transfer);
		_idleButton.ShowSelectionOutline(valueOrDefault == RocketMode.None);
	}

	public void Clear()
	{
	}

	private void MineButtonClicked()
	{
		_locationPanel?.Motherboard?.SelectedRocket()?.Avionics?.SetRocketMode(RocketMode.Mine);
	}

	private void DiscoverButtonClicked()
	{
		_locationPanel?.Motherboard?.SelectedRocket()?.Avionics?.SetRocketMode(RocketMode.Discover);
	}

	private void ChartButtonClicked()
	{
		_locationPanel?.Motherboard?.SelectedRocket()?.Avionics?.SetRocketMode(RocketMode.Chart);
	}

	private void SurveyButtonClicked()
	{
		_locationPanel?.Motherboard?.SelectedRocket()?.Avionics?.SetRocketMode(RocketMode.Survey);
	}

	private void DeployButtonClicked()
	{
		_locationPanel?.Motherboard?.SelectedRocket()?.Avionics?.SetRocketMode(RocketMode.Deploy);
	}

	private void SurfaceScanButtonClicked()
	{
		_locationPanel?.Motherboard?.SelectedRocket()?.Avionics?.SetRocketMode(RocketMode.SurfaceScan);
	}

	private void TransferButtonClicked()
	{
		_locationPanel?.Motherboard?.SelectedRocket()?.Avionics?.SetRocketMode(RocketMode.Transfer);
	}

	private void IdleButtonClicked()
	{
		_locationPanel?.Motherboard?.SelectedRocket()?.Avionics?.SetRocketMode(RocketMode.None);
	}

	private new void OnEnable()
	{
		BasicButton button = _mineButton.Button;
		button.OnClick = (Action)Delegate.Combine(button.OnClick, new Action(MineButtonClicked));
		BasicButton button2 = _discoverButton.Button;
		button2.OnClick = (Action)Delegate.Combine(button2.OnClick, new Action(DiscoverButtonClicked));
		BasicButton button3 = _chartButton.Button;
		button3.OnClick = (Action)Delegate.Combine(button3.OnClick, new Action(ChartButtonClicked));
		BasicButton button4 = _surveyButton.Button;
		button4.OnClick = (Action)Delegate.Combine(button4.OnClick, new Action(SurveyButtonClicked));
		BasicButton button5 = _deployButton.Button;
		button5.OnClick = (Action)Delegate.Combine(button5.OnClick, new Action(DeployButtonClicked));
		BasicButton button6 = _surfaceScanButton.Button;
		button6.OnClick = (Action)Delegate.Combine(button6.OnClick, new Action(SurfaceScanButtonClicked));
		BasicButton button7 = _transferButton.Button;
		button7.OnClick = (Action)Delegate.Combine(button7.OnClick, new Action(TransferButtonClicked));
		BasicButton button8 = _idleButton.Button;
		button8.OnClick = (Action)Delegate.Combine(button8.OnClick, new Action(IdleButtonClicked));
	}

	private new void OnDisable()
	{
		BasicButton button = _mineButton.Button;
		button.OnClick = (Action)Delegate.Remove(button.OnClick, new Action(MineButtonClicked));
		BasicButton button2 = _discoverButton.Button;
		button2.OnClick = (Action)Delegate.Remove(button2.OnClick, new Action(DiscoverButtonClicked));
		BasicButton button3 = _chartButton.Button;
		button3.OnClick = (Action)Delegate.Remove(button3.OnClick, new Action(ChartButtonClicked));
		BasicButton button4 = _surveyButton.Button;
		button4.OnClick = (Action)Delegate.Remove(button4.OnClick, new Action(SurveyButtonClicked));
		BasicButton button5 = _deployButton.Button;
		button5.OnClick = (Action)Delegate.Remove(button5.OnClick, new Action(DeployButtonClicked));
		BasicButton button6 = _surfaceScanButton.Button;
		button6.OnClick = (Action)Delegate.Remove(button6.OnClick, new Action(SurfaceScanButtonClicked));
		BasicButton button7 = _transferButton.Button;
		button7.OnClick = (Action)Delegate.Remove(button7.OnClick, new Action(TransferButtonClicked));
		BasicButton button8 = _idleButton.Button;
		button8.OnClick = (Action)Delegate.Remove(button8.OnClick, new Action(IdleButtonClicked));
	}
}
