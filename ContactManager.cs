using System.Collections;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Util;
using UnityEngine;

public class ContactManager : ManagerBase
{
	public static bool RefreshContacts;

	public static ContactManager Instance;

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		if (Instance == null)
		{
			Instance = this;
		}
	}

	public void StartCheckContactLifeTime()
	{
		if (GameManager.RunSimulation)
		{
			StopCoroutine("CheckContactLifeTime");
			StartCoroutine("CheckContactLifeTime");
		}
	}

	public IEnumerator CheckContactLifeTime()
	{
		WaitForEndOfFrame frameDelay = new WaitForEndOfFrame();
		while (GameManager.GameState != GameState.Running)
		{
			if (GameManager.GameState == GameState.None)
			{
				yield break;
			}
			yield return frameDelay;
		}
		yield return frameDelay;
		while (GameManager.GameState == GameState.Running)
		{
			if (GameManager.RunSimulation)
			{
				for (int num = TraderContact.AllStationContacts.Count - 1; num >= 0; num--)
				{
					TraderContact traderContact = TraderContact.AllStationContacts[num];
					if (traderContact == null)
					{
						TraderContact.AllStationContacts.RemoveAt(num);
						ConsoleWindow.PrintError("Contact Manager: Iterated over null contact");
						RefreshContacts = true;
					}
					else
					{
						if (traderContact.EndLifetime < Time.time && !traderContact.CurrentlyTrading)
						{
							TraderContact.RemoveContact(traderContact);
							RefreshContacts = true;
						}
						if (traderContact.InterrogatingDish == null && traderContact.NormalizedSecondsConnected > 0f)
						{
							traderContact.NormalizedSecondsConnected = 0f;
						}
						if (traderContact.InterrogatingDish != null && (!traderContact.InterrogatingDish.Powered || !traderContact.InterrogatingDish.OnOff))
						{
							traderContact.NormalizedSecondsConnected = 0f;
							traderContact.InterrogatingDish.InterrogatingContact = null;
							traderContact.InterrogatingDish = null;
						}
					}
				}
			}
			foreach (ContactSlot contactSlot in ContactSlot.ContactSlots)
			{
				contactSlot.TryGenerateNewContact();
			}
			if (RefreshContacts)
			{
				TraderContact.OnInitializedCall();
				RefreshContacts = false;
			}
			yield return Yielders.WaitForSeconds(1f);
		}
	}
}
