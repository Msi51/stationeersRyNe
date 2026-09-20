using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Motherboards;

public struct ScannedContactUIData
{
	public TraderContact TraderContact;

	public SatelliteDish ScanningDish;

	public ContactState ContactState;

	public float NormalizedSecondsConnected;

	public float SecondsRequiredToContact;

	public int CurrentTimeTillResolve;

	public float StartTimeTillResolve;

	public int DishAlignment;

	public bool CurrentlyTrading;

	public int TimeRemainingSeconds;

	public bool ContactButtonInteractable;

	public static ScannedContactUIData Invalid;

	public bool ProgressBarRequired => ContactState == ContactState.Resolving;

	public bool LandButtonInteractable
	{
		get
		{
			if (ContactState == ContactState.Contacted)
			{
				return !TraderContact.CurrentlyTrading;
			}
			return false;
		}
	}

	public bool IsValid()
	{
		if (ContactState != ContactState.None && TraderContact != null)
		{
			return ScanningDish != null;
		}
		return false;
	}

	public ScannedContactUIData(SatelliteDish dish, TraderContact contact)
	{
		ScanningDish = dish;
		TraderContact = contact;
		if (ScanningDish != null && contact != null)
		{
			ContactState = contact.GetContactState(dish);
			NormalizedSecondsConnected = contact.NormalizedSecondsConnected;
			SecondsRequiredToContact = contact.SecondsRequiredToContact;
			TimeRemainingSeconds = (GameManager.RunSimulation ? Mathf.FloorToInt(contact.EndLifetime - GameManager.GameTime) : Mathf.FloorToInt(contact.EndLifetime - NetworkTime.time));
			TimeRemainingSeconds = Mathf.Max(0, TimeRemainingSeconds);
			if (dish.DishScannedContacts.TryGetData(contact, out var data))
			{
				CurrentTimeTillResolve = (dish.RotatableBehaviour.IsMoving ? 99999 : ((int)data.CurrentTimeTillResolve));
				float lastScannedDegreeOffset = data.LastScannedDegreeOffset;
				DishAlignment = (int)lastScannedDegreeOffset;
				StartTimeTillResolve = data.StartTimeTillResolve;
				ContactButtonInteractable = data.CurrentTimeTillResolve <= 0f && !contact.Contacted;
				CurrentlyTrading = TraderContact.CurrentlyTrading;
			}
			else
			{
				CurrentTimeTillResolve = 0;
				DishAlignment = 0;
				StartTimeTillResolve = 0f;
				ContactButtonInteractable = false;
				CurrentlyTrading = false;
			}
		}
		else
		{
			ContactState = ContactState.None;
			NormalizedSecondsConnected = 0f;
			SecondsRequiredToContact = 0f;
			CurrentTimeTillResolve = 0;
			DishAlignment = 0;
			StartTimeTillResolve = 0f;
			ContactButtonInteractable = false;
			CurrentlyTrading = false;
			TimeRemainingSeconds = 0;
		}
	}

	public bool Equals(ScannedContactUIData other)
	{
		if (!IsValid() && !other.IsValid())
		{
			return true;
		}
		if (!IsValid() || !other.IsValid())
		{
			return false;
		}
		if (ScanningDish.ReferenceId == other.ScanningDish.ReferenceId && TraderContact.ReferenceId == other.TraderContact.ReferenceId && ContactState == other.ContactState && RocketMath.Approximately(NormalizedSecondsConnected, other.NormalizedSecondsConnected) && RocketMath.Approximately(SecondsRequiredToContact, other.SecondsRequiredToContact) && CurrentTimeTillResolve == other.CurrentTimeTillResolve && RocketMath.Approximately(StartTimeTillResolve, other.StartTimeTillResolve) && DishAlignment == other.DishAlignment && CurrentlyTrading == other.CurrentlyTrading)
		{
			return TimeRemainingSeconds == other.TimeRemainingSeconds;
		}
		return false;
	}

	public float ProgressBarPercentage()
	{
		return ContactState switch
		{
			ContactState.Resolving => 1f - (float)CurrentTimeTillResolve / StartTimeTillResolve, 
			ContactState.Interrogating => NormalizedSecondsConnected / SecondsRequiredToContact, 
			_ => 1f, 
		};
	}

	public static string GenerateContactStatusText(ScannedContactUIData uiData)
	{
		if (uiData.CurrentlyTrading)
		{
			return GameStrings.CommsMotherboardCurrentlyTrading;
		}
		return uiData.ContactState switch
		{
			ContactState.None => string.Empty, 
			ContactState.Unknown => GameStrings.CommsMotherboardTimeTillResolveUnknown, 
			ContactState.Resolving => GameStrings.CommsMotherboardSecondsTillResolve.AsString(StringManager.Get(uiData.CurrentTimeTillResolve)), 
			ContactState.Resolved => GameStrings.CommsMotherboardDegreesFromContact.AsString(StringManager.Get(uiData.DishAlignment)), 
			ContactState.Interrogating => GameStrings.CommsMotherboardPercentInterogated.AsString(StringManager.Get((int)Mathf.Clamp(uiData.NormalizedSecondsConnected / uiData.SecondsRequiredToContact * 100f, 0f, 100f))), 
			ContactState.Contacted => GameStrings.CommsMotherboardContacted, 
			_ => string.Empty, 
		};
	}

	public static bool ContactStatusTextUpdateRequired(ScannedContactUIData displayedData, ScannedContactUIData newData)
	{
		if (!displayedData.IsValid() || displayedData.ContactState != newData.ContactState || displayedData.CurrentlyTrading != newData.CurrentlyTrading)
		{
			return true;
		}
		switch (newData.ContactState)
		{
		case ContactState.None:
		case ContactState.Unknown:
		case ContactState.Contacted:
			return false;
		case ContactState.Resolving:
			return displayedData.CurrentTimeTillResolve != newData.CurrentTimeTillResolve;
		case ContactState.Resolved:
			return displayedData.DishAlignment != newData.DishAlignment;
		case ContactState.Interrogating:
			return (int)Mathf.Clamp(displayedData.NormalizedSecondsConnected / displayedData.SecondsRequiredToContact * 100f, 0f, 100f) != (int)Mathf.Clamp(newData.NormalizedSecondsConnected / newData.SecondsRequiredToContact * 100f, 0f, 100f);
		default:
			return false;
		}
	}
}
