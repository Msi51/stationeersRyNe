using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Objects.Rockets.UI.Models;
using TMPro;
using UI.Motherboard;
using UI.Tooltips;
using UnityEngine;

namespace Objects.Rockets.UI;

public class RocketPanel : UserInterfaceBase
{
	[Space(15f)]
	[Header("Rocket Panel")]
	[SerializeField]
	private InfoPanel _infoPanel;

	[SerializeField]
	private InfoSecondaryPanel _infoSecondaryPanel;

	[SerializeField]
	private LogicControlPanel _logicControlPanel;

	[SerializeField]
	private TextMeshProUGUI _rocketNameText;

	[SerializeField]
	private RocketToggleGroup _rocketToggleGroup;

	[Space(15f)]
	[SerializeField]
	private TwoStepButton _abandonButton;

	[SerializeField]
	private ToggleButton _autoShutOffToggleButton;

	[SerializeField]
	private ToggleButton _autoLandToggleButton;

	[SerializeField]
	private OnOffButton _autoShutOffToggle;

	[SerializeField]
	private OnOffButton _autoLandToggle;

	[SerializeField]
	private TMP_Dropdown _landingDropdown;

	[SerializeField]
	private UITooltip _landingDropDownTooltip;

	[SerializeField]
	private UITooltip _autolandTooltip;

	[SerializeField]
	private UITooltip _confidenceTooltip;

	[SerializeField]
	private LandingConfidenceMeter _landingConfidenceMeter;

	[Header("Log Panel")]
	[SerializeField]
	private ExpandCollapseToggle _logToggle;

	[SerializeField]
	private RectTransform _bodyPanel;

	[SerializeField]
	private RectTransform _logPanelRectTransform;

	[SerializeField]
	private LogPanel _logPanel;

	[SerializeField]
	private GameObject _secondaryInfoPanel;

	private RocketMotherboard _motherboard;

	public ConnectedRocketInfo CurrentRocket => _motherboard.SelectedRocket();

	public void Initialize()
	{
		_logicControlPanel.Initialize();
	}

	public void Show(RocketMotherboard motherboard)
	{
		SetVisible(isVisble: true);
		_motherboard = motherboard;
		InitializeRocketToggleGroup(motherboard);
		int selectedIndex = motherboard.SelectedIndex;
		_rocketToggleGroup.Select(selectedIndex, force: true);
		SetDropdownValues();
		_logicControlPanel.Show(motherboard);
		_logPanel.Initialize();
	}

	public void Hide()
	{
		SetVisible(isVisble: false);
	}

	private void SetDropdownValues()
	{
		List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
		for (int i = 1; i < EnumCollections.ReEntryProfile.Names.Length; i++)
		{
			ReEntryProfile reEntryProfile = EnumCollections.ReEntryProfile.Values[i];
			string text = reEntryProfile.GetName() + " " + StringManager.Get((int)(Rocket.ReEntryProfiles[reEntryProfile] / 1000f)) + "km";
			list.Add(new RocketReEntryDropdownOption(reEntryProfile, text));
		}
		_landingDropdown.options = list;
		RefreshSelectedReEntryProfile();
	}

	private void RefreshSelectedReEntryProfile()
	{
		ReEntryProfile reEntryProfile = ReEntryProfile.Low;
		if (CurrentRocket != null && CurrentRocket.Avionics != null)
		{
			reEntryProfile = CurrentRocket.Avionics.GetRocketReEntryProfile();
		}
		_landingDropdown.SetValueWithoutNotify((int)(reEntryProfile - 1));
	}

	public void Refresh(RocketUIModel model)
	{
		if (!IsVisible)
		{
			return;
		}
		RocketModel selectedRocketModel = model.RocketPanelModel.SelectedRocketModel;
		_rocketNameText.text = selectedRocketModel.DisplayName;
		_autoShutOffToggleButton.Set(selectedRocketModel.AutoShutOff);
		_autoLandToggleButton.Set(selectedRocketModel.AutoLand);
		_autoShutOffToggle.IsOn = selectedRocketModel.AutoShutOff;
		_autoLandToggle.IsOn = selectedRocketModel.AutoLand;
		_landingConfidenceMeter.Refresh(selectedRocketModel.AutoLand, selectedRocketModel.AutoLandConfidenceRatio, selectedRocketModel.AutoLandConfidenceString);
		StringBuilder stringBuilder = new StringBuilder();
		if (selectedRocketModel.AutoLand)
		{
			stringBuilder.Append(GameStrings.AutoLandEnabledTooltip);
			stringBuilder.Append("\n");
			GameStrings.LandingBeginAltitudeToolTip.AppendFormat(stringBuilder, selectedRocketModel.ReEntryAltitude.ToStringPrefix("m", "yellow"));
			if (selectedRocketModel.AutoLandConfidenceRatio <= 0f)
			{
				stringBuilder.Append("\n").Append("<b>").Append(GameStrings.AutoLandNoConfidence)
					.Append("</b>");
			}
			else
			{
				stringBuilder.Append("\n").Append(GameStrings.AutoLandThrustRequired.AsString(selectedRocketModel.AutoLandConfidenceToolTip, selectedRocketModel.RequiredThrustToAutoLand.ToStringPrefix("N", "yellow"), selectedRocketModel.ExpectedMaxThrustDuringAutoland.ToStringPrefix("N", "yellow")));
			}
			string tooltipText = stringBuilder.ToString();
			_autolandTooltip.TooltipText = tooltipText;
			_confidenceTooltip.TooltipText = tooltipText;
			_landingDropDownTooltip.TooltipText = tooltipText;
		}
		else
		{
			_autolandTooltip.TooltipText = GameStrings.AutoLandDisabledTooltip;
			_confidenceTooltip.TooltipText = GameStrings.AutoLandDisabledTooltip;
			stringBuilder.Append(GameStrings.LandingBeginAltitudeToolTip.AsString(selectedRocketModel.ReEntryAltitude.ToStringPrefix("m", "yellow")));
			_landingDropDownTooltip.TooltipText = stringBuilder.ToString();
		}
		stringBuilder.Clear();
		_infoPanel.Refresh(selectedRocketModel);
		_infoSecondaryPanel.Refresh(selectedRocketModel);
		_logicControlPanel.Refresh(model);
		_rocketToggleGroup.Select(_motherboard.SelectedIndex, force: false, mute: true);
		RefreshToggleButtons();
		RefreshSelectedReEntryProfile();
	}

	private void RefreshToggleButtons()
	{
		_autoShutOffToggleButton.Set(CurrentRocket?.Avionics?.GetIsAutoShutOff() == true);
		_autoLandToggleButton.Set(CurrentRocket?.Avionics?.GetIsAutoLand() == true);
		_autoShutOffToggle.IsOn = CurrentRocket?.Avionics?.GetIsAutoShutOff() == true;
		_autoLandToggle.IsOn = CurrentRocket?.Avionics?.GetIsAutoLand() == true;
	}

	private void InitializeRocketToggleGroup(RocketMotherboard motherboard)
	{
		_rocketToggleGroup.DisableAll();
		for (int i = 0; i < motherboard.ConnectedRockets.Length; i++)
		{
			_rocketToggleGroup.Enable(i, motherboard.ConnectedRockets[i].Tooltip());
		}
	}

	private void RocketSelected(int index)
	{
		_motherboard.RocketSelected(index);
	}

	private void OnAutoShutOffToggled()
	{
		_autoShutOffToggle.Toggle();
		_motherboard.OnAutoShutOffToggled(_autoShutOffToggle.IsOn);
	}

	private void OnAutoLandToggled()
	{
		_autoLandToggle.Toggle();
		_motherboard.OnAutoLandToggled(_autoLandToggle.IsOn);
	}

	private void OnAutoShutOffToggled(bool state)
	{
		_motherboard.OnAutoShutOffToggled(state);
	}

	private void OnAutoLandToggled(bool state)
	{
		_motherboard.OnAutoLandToggled(state);
	}

	private void AbandonRocket()
	{
		_motherboard.AbandonRocket();
	}

	private void OnLandingProfileSelected(int index)
	{
		_motherboard.OnLandingProfileSelected(index);
	}

	private void OnExpandLog()
	{
		_logPanelRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0f, 345f);
		_bodyPanel.offsetMin = new Vector2(0f, 345f);
		_logPanel.Refresh();
		_secondaryInfoPanel.SetActive(value: false);
	}

	private void OnCollapseLog()
	{
		_logPanelRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0f, 55f);
		_bodyPanel.offsetMin = new Vector2(0f, 55f);
		_logPanel.Refresh();
		_secondaryInfoPanel.SetActive(value: true);
	}

	private new void OnEnable()
	{
		RocketToggleGroup rocketToggleGroup = _rocketToggleGroup;
		rocketToggleGroup.OnIndexSelected = (Action<int>)Delegate.Combine(rocketToggleGroup.OnIndexSelected, new Action<int>(RocketSelected));
		ToggleButton autoShutOffToggleButton = _autoShutOffToggleButton;
		autoShutOffToggleButton.OnClick = (Action)Delegate.Combine(autoShutOffToggleButton.OnClick, new Action(OnAutoShutOffToggled));
		ToggleButton autoLandToggleButton = _autoLandToggleButton;
		autoLandToggleButton.OnClick = (Action)Delegate.Combine(autoLandToggleButton.OnClick, new Action(OnAutoLandToggled));
		OnOffButton autoShutOffToggle = _autoShutOffToggle;
		autoShutOffToggle.OnClick = (Action<bool>)Delegate.Combine(autoShutOffToggle.OnClick, new Action<bool>(OnAutoShutOffToggled));
		OnOffButton autoLandToggle = _autoLandToggle;
		autoLandToggle.OnClick = (Action<bool>)Delegate.Combine(autoLandToggle.OnClick, new Action<bool>(OnAutoLandToggled));
		TwoStepButton abandonButton = _abandonButton;
		abandonButton.OnConfirm = (Action)Delegate.Combine(abandonButton.OnConfirm, new Action(AbandonRocket));
		ExpandCollapseToggle logToggle = _logToggle;
		logToggle.OnCollapse = (Action)Delegate.Combine(logToggle.OnCollapse, new Action(OnCollapseLog));
		ExpandCollapseToggle logToggle2 = _logToggle;
		logToggle2.OnExpand = (Action)Delegate.Combine(logToggle2.OnExpand, new Action(OnExpandLog));
		_landingDropdown.onValueChanged.AddListener(OnLandingProfileSelected);
	}

	private new void OnDisable()
	{
		RocketToggleGroup rocketToggleGroup = _rocketToggleGroup;
		rocketToggleGroup.OnIndexSelected = (Action<int>)Delegate.Remove(rocketToggleGroup.OnIndexSelected, new Action<int>(RocketSelected));
		ToggleButton autoShutOffToggleButton = _autoShutOffToggleButton;
		autoShutOffToggleButton.OnClick = (Action)Delegate.Remove(autoShutOffToggleButton.OnClick, new Action(OnAutoShutOffToggled));
		ToggleButton autoLandToggleButton = _autoLandToggleButton;
		autoLandToggleButton.OnClick = (Action)Delegate.Remove(autoLandToggleButton.OnClick, new Action(OnAutoLandToggled));
		OnOffButton autoShutOffToggle = _autoShutOffToggle;
		autoShutOffToggle.OnClick = (Action<bool>)Delegate.Remove(autoShutOffToggle.OnClick, new Action<bool>(OnAutoShutOffToggled));
		OnOffButton autoLandToggle = _autoLandToggle;
		autoLandToggle.OnClick = (Action<bool>)Delegate.Remove(autoLandToggle.OnClick, new Action<bool>(OnAutoLandToggled));
		TwoStepButton abandonButton = _abandonButton;
		abandonButton.OnConfirm = (Action)Delegate.Remove(abandonButton.OnConfirm, new Action(AbandonRocket));
		ExpandCollapseToggle logToggle = _logToggle;
		logToggle.OnCollapse = (Action)Delegate.Remove(logToggle.OnCollapse, new Action(OnCollapseLog));
		ExpandCollapseToggle logToggle2 = _logToggle;
		logToggle2.OnExpand = (Action)Delegate.Remove(logToggle2.OnExpand, new Action(OnExpandLog));
		_landingDropdown.onValueChanged.RemoveListener(OnLandingProfileSelected);
		_landingConfidenceMeter.Clear();
	}
}
