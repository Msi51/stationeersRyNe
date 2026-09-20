using System.Collections;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class CommsTerminal : InputWindowBase, IModal
{
	public delegate void InputEvent(string result);

	public static CommsTerminal Instance;

	public TextMeshProUGUI TitleText;

	public TextMeshProUGUI Message;

	public static InputPanelState InputState;

	public static CommsMotherboard CommsMotherboard;

	public static TraderContact CurrentContact;

	public TextMeshProUGUI ButtonConfirmText;

	public Button ButtonConfirm;

	public RawImage Icon;

	private static float LastMessageWattageRatio;

	private static SatelliteDish LastDish;

	public bool UnlockCursor => true;

	public static event InputEvent OnSubmit;

	public override void Initialize()
	{
		base.Initialize();
		Instance = this;
		SetVisible(isVisble: false);
	}

	public void ButtonInputClose()
	{
		InputState = InputPanelState.Submitted;
		Instance.SetVisible(isVisble: false);
		MouseModeController.RemoveModal(this);
		CommsTerminal.OnSubmit = null;
		InputState = InputPanelState.None;
	}

	protected override void CloseOnClientConnected(bool isPaused, string message)
	{
		if (IsVisible && isPaused)
		{
			ButtonInputClose();
		}
	}

	public void ButtonHandleContactTerminalPanel()
	{
		if (CommsMotherboard.SelectedLandingPad != null && CurrentContact != null && CommsMotherboard != null && CommsMotherboard.SelectedLandingPad.CanTraderLand(CurrentContact, out var _) && CurrentContact.Contacted)
		{
			CallTrader();
		}
		else if (LastDish != null && CurrentContact != null && !CurrentContact.InterrogatingDish && (bool)LastDish && LastMessageWattageRatio >= 1f)
		{
			InterrogateTrader(CurrentContact, LastDish);
		}
		Instance.ButtonInputClose();
		InputState = InputPanelState.Submitted;
		Instance.SetVisible(isVisble: false);
		MouseModeController.RemoveModal(this);
		CommsTerminal.OnSubmit = null;
		InputState = InputPanelState.None;
	}

	private void CallTrader()
	{
		TraderContact currentContact = CurrentContact;
		if (currentContact != null && CommsMotherboard.SelectedLandingPad.CurrentTradingContact == null)
		{
			if (GameManager.RunSimulation)
			{
				CommsMotherboard.SelectedLandingPad.ServerCallTrader(isLanding: true, currentContact);
			}
			else if (NetworkManager.IsClient)
			{
				NetworkClient.CallTrader(isLanding: true, currentContact, CommsMotherboard.SelectedLandingPad);
			}
		}
	}

	public static void InterrogateTrader(TraderContact trader, SatelliteDish dish)
	{
		if (GameManager.RunSimulation)
		{
			trader.InterrogatingDish = dish;
			dish.InterrogatingContact = trader;
		}
		else if (NetworkManager.IsClient)
		{
			NetworkClient.InterrogateTrader(trader, dish);
		}
	}

	public static string ParseText(List<string> messages)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string message in messages)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append("\n");
			}
			stringBuilder.Append(message);
		}
		return stringBuilder.ToString();
	}

	public static bool ShowInputPanel(TraderContact contact, CommsMotherboard commsMotherboard)
	{
		if (InputState != InputPanelState.None)
		{
			return false;
		}
		CurrentContact = contact;
		CommsMotherboard = commsMotherboard;
		SatelliteDish selectedDish = commsMotherboard.SelectedDish;
		InputState = InputPanelState.Waiting;
		Instance.SetVisible(isVisble: true);
		MouseModeController.AddModal(Instance);
		Instance.StartCoroutine(Instance.WaitForInput());
		Instance.TitleText.text = contact.ContactName;
		List<string> messageText = contact.GetMessageText(CommsMotherboard);
		bool flag = messageText.Count > 1;
		float wattageOnContact = selectedDish.GetWattageOnContact(contact);
		float num = contact.InterrogationWattageRatio(selectedDish);
		float f = contact.TotalInterrogationTimeAtWattage(wattageOnContact);
		float value = contact.InterrogationTimeRemainingAtWattage(wattageOnContact);
		if (contact.InterrogatingDish != null && contact.InterrogatingDish != selectedDish)
		{
			messageText.Add(GameStrings.CommsTerminalInterrogationInProgressAnother);
			Instance.ButtonConfirmText.SetText(GameStrings.NotApplicableString);
			Instance.ButtonConfirm.interactable = false;
			Instance.Icon.gameObject.SetActive(value: false);
		}
		else if ((bool)contact.InterrogatingDish && !contact.Contacted)
		{
			messageText.Add(GameStrings.CommsTerminalInterrogationInProgress);
			messageText.Add(GameStrings.CommTerminalWattageOnContact.AsString(StringManager.Get(Mathf.Floor(wattageOnContact)), StringManager.Get(selectedDish.Setting)));
			messageText.Add(GameStrings.CommTerminalEstimatedSecondsUntilInterrogated.AsString(value.ToStringRounded()));
			messageText.Add(GameStrings.CommsTerminalDishInUse.AsString(selectedDish.DisplayName));
			Instance.ButtonConfirmText.SetText(GameStrings.NotApplicableString);
			Instance.ButtonConfirm.interactable = false;
			Instance.Icon.gameObject.SetActive(value: false);
		}
		else if (contact.Contacted)
		{
			if (flag)
			{
				Instance.ButtonConfirmText.SetText(GameStrings.CommsTerminalLandTraderButton);
				Instance.ButtonConfirm.interactable = false;
				Instance.Icon.gameObject.SetActive(value: false);
			}
			else
			{
				messageText.Add(GameStrings.CommsTerminalContactingComplete);
				Instance.ButtonConfirmText.SetText(GameStrings.CommsTerminalLandTraderButton);
				Instance.ButtonConfirm.interactable = true;
				Instance.Icon.gameObject.SetActive(value: true);
			}
		}
		else if (!contact.Contacted && !contact.InterrogatingDish)
		{
			messageText.Add(GameStrings.CommsTerminalContactRequires.AsString(StringManager.Get(Mathf.Ceil(CurrentContact.MinimumWattsToContact)), StringManager.Get(Mathf.Ceil(CurrentContact.SecondsRequiredToContact))));
			messageText.Add(GameStrings.CommTerminalWattageOnContact.AsString(StringManager.Get(Mathf.Floor(wattageOnContact)), StringManager.Get(selectedDish.Setting)));
			messageText.Add(GameStrings.CommsTerminalDishInUse.AsString(selectedDish.DisplayName));
			if (num >= 1f)
			{
				if (selectedDish.InterrogatingContact != null)
				{
					messageText.Add(GameStrings.CommsTerminalDishBusy);
					Instance.ButtonConfirmText.SetText(GameStrings.CommsTerminalInterrogateButton);
					Instance.ButtonConfirm.interactable = false;
					Instance.Icon.gameObject.SetActive(value: false);
				}
				else
				{
					messageText.Add(GameStrings.CommTerminalEstimatedSecondsUntilInterrogated.AsString(StringManager.Get(Mathf.Ceil(f))));
					Instance.ButtonConfirmText.SetText(GameStrings.CommsTerminalInterrogateButton);
					Instance.ButtonConfirm.interactable = true;
					Instance.Icon.gameObject.SetActive(value: true);
				}
			}
			else
			{
				messageText.Add(GameStrings.CommsTerminalNotEnoughEnergy);
				Instance.ButtonConfirmText.SetText(GameStrings.NotApplicableString);
				Instance.ButtonConfirm.interactable = false;
				Instance.Icon.gameObject.SetActive(value: false);
			}
		}
		LastDish = selectedDish;
		LastMessageWattageRatio = num;
		Instance.Message.text = ParseText(messageText);
		return true;
	}

	private IEnumerator WaitForInput()
	{
		while (InputState == InputPanelState.Waiting)
		{
			yield return Yielders.EndOfFrame;
		}
		InputState = InputPanelState.None;
	}
}
