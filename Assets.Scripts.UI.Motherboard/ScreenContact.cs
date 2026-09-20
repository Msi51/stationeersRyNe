using System;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using CharacterCustomisation;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Motherboard;

public class ScreenContact : GameBase
{
	[NonSerialized]
	public CommsMotherboard ParentMotherboard;

	public TraderContact AssignedContact;

	public SatelliteDish AssignedDish;

	public Button ContactButton;

	public Button LandButton;

	public Slider ProgressBar;

	public Image Icon;

	public Sprite[] Icons;

	public TextMeshProUGUI RunwayLength;

	public TextMeshProUGUI PadSize;

	public UiComponentRenderer RunwayRequired;

	public UiComponentRenderer O2Required;

	public TextMeshProUGUI ContactNameText;

	[FormerlySerializedAs("SignalStrengthText")]
	public TextMeshProUGUI ContactingStatusText;

	public TextMeshProUGUI TimeRemainingText;

	public UiComponentRenderer SignalStrengthRenderer;

	public UiComponentRenderer ProgressBarRenderer;

	public UiComponentRenderer UiComponentRenderer;

	private ScannedContactUIData _displayedData;

	public override bool IsVisible
	{
		get
		{
			if (!(UiComponentRenderer != null))
			{
				return base.IsVisible;
			}
			return UiComponentRenderer.IsVisible;
		}
	}

	public override void SetVisible(bool isVisble)
	{
		UiComponentRenderer.SetVisible(isVisble);
	}

	public override void SetActive(bool active)
	{
		UiComponentRenderer.SetVisible(active);
	}

	public void SetContactIcons(TraderContact contact)
	{
		PadSize.text = StringManager.Get(Mathf.CeilToInt(contact.RequiredPadSize().x)) + "x" + StringManager.Get(Mathf.CeilToInt(contact.RequiredPadSize().y));
		RunwayRequired.SetVisible(contact.RequiresThreshold);
		RunwayLength.text = StringManager.Get(contact.RequiredRunwayLength);
		O2Required.SetVisible(contact.RequiredPadEnvironment == SpeciesClass.Human);
	}

	public void ButtonContact()
	{
		if (AssignedContact != null)
		{
			CommsTerminal.InputState = InputPanelState.None;
			CommsTerminal.ShowInputPanel(AssignedContact, ParentMotherboard);
		}
		else
		{
			AlertPanel.Instance.ShowAlert(AlertStrings.TraderGoneMissing, AlertState.Alert);
		}
	}

	public void Assign(CommsMotherboard commsMotherboard)
	{
		ParentMotherboard = commsMotherboard;
		SetVisible(isVisble: false);
	}

	public void Clear()
	{
		SetVisible(isVisble: false);
	}

	internal void Assign(ScannedContactUIData newData)
	{
		if (!newData.IsValid())
		{
			Clear();
		}
		AssignedDish = newData.ScanningDish;
		ProgressBarRenderer.SetVisible(newData.ProgressBarRequired);
		SignalStrengthRenderer.SetVisible(!newData.ProgressBarRequired);
		if (_displayedData.TraderContact == null || _displayedData.TraderContact.ReferenceId != newData.TraderContact.ReferenceId)
		{
			AssignedContact = newData.TraderContact;
			ContactNameText.text = AssignedContact.ContactName;
			Icon.sprite = AssignedContact.ContactSlot.ContactSlotData.Icon.IconSprite;
			SetContactIcons(newData.TraderContact);
		}
		TimeRemainingText.text = $"{StringManager.Get(newData.TimeRemainingSeconds / 60)}:{newData.TimeRemainingSeconds % 60:D2}";
		if (ScannedContactUIData.ContactStatusTextUpdateRequired(_displayedData, newData))
		{
			ContactingStatusText.text = ScannedContactUIData.GenerateContactStatusText(newData);
		}
		if (!RocketMath.Approximately(_displayedData.ProgressBarPercentage(), newData.ProgressBarPercentage()))
		{
			ProgressBar.value = newData.ProgressBarPercentage();
		}
		if (LandButton.interactable != newData.LandButtonInteractable)
		{
			LandButton.interactable = newData.LandButtonInteractable;
		}
		if (ContactButton.interactable != newData.ContactButtonInteractable)
		{
			ContactButton.interactable = newData.ContactButtonInteractable;
		}
		SetVisible(isVisble: true);
		_displayedData = newData;
	}
}
