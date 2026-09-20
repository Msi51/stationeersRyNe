using System;
using System.Collections.Generic;
using Assets.Scripts.Localization2;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;
using WorldLogSystem;

namespace Assets.Scripts;

public class ContactSlot
{
	public static List<ContactSlot> ContactSlots = new List<ContactSlot>();

	private static float _minWattsVisibleDefault = 2f;

	private static float _minWattsContactDefault = 180f;

	private static float _secondsRequiredToContactDefault = 60f;

	private static float _wattsToResolveDefault = 100f;

	private static float _lifeTimeDefault = 1200f;

	public readonly int IdHash;

	public long SavedContactId;

	public readonly ContactSlotData ContactSlotData;

	public string Id => ContactSlotData?.Id ?? string.Empty;

	public TraderContact CurrentContact { get; set; }

	public float LastExpirationTime { get; private set; }

	public float CurrentDownTime { get; private set; }

	public static void CommandResetSlots()
	{
		foreach (ContactSlot contactSlot in ContactSlots)
		{
			if (contactSlot != null)
			{
				if (contactSlot.CurrentContact != null)
				{
					contactSlot.CurrentContact.EndLifetime = 0f;
				}
				contactSlot.CurrentDownTime = 0f;
			}
		}
	}

	public ContactSlot(ContactSlotData contactSlotData)
	{
		ContactSlotData = contactSlotData;
		if (!string.IsNullOrEmpty(Id))
		{
			IdHash = Animator.StringToHash(Id);
		}
	}

	public void Clear()
	{
		LastExpirationTime = GameManager.GameTime;
		if (CurrentContact != null)
		{
			CurrentContact.ContactSlot = null;
		}
		CurrentContact = null;
		SavedContactId = 0L;
	}

	public bool TryGenerateNewContact(TraderData traderData = null, bool force = false)
	{
		if (!force && (CurrentContact != null || GameManager.GameTime - LastExpirationTime <= CurrentDownTime))
		{
			return false;
		}
		if (!force && ContactSlotData.WorldCondition != null && !ContactSlotData.WorldCondition.IsValid())
		{
			return false;
		}
		if (CurrentContact != null)
		{
			TraderContact.RemoveContact(CurrentContact);
		}
		if (traderData == null)
		{
			if (ContactSlotData.TraderSelectData != null)
			{
				traderData = ContactSlotData.TraderSelectData.Select();
			}
			else
			{
				List<TraderData> list = new List<TraderData>();
				foreach (TraderData allTraderDatum in TraderData.AllTraderData)
				{
					if (allTraderDatum.SlotTypes.Count == 0)
					{
						list.Add(allTraderDatum);
						continue;
					}
					foreach (SlotIdReference slotType in allTraderDatum.SlotTypes)
					{
						if (slotType.SlotIdHash == ContactSlotData.IdHash)
						{
							list.Add(allTraderDatum);
							break;
						}
					}
				}
				traderData = list.Pick();
			}
		}
		int seed = TraderContact.Randomize.Next();
		CurrentContact = new TraderContact(this, 0L)
		{
			Angle = UnityEngine.Random.onUnitSphere,
			WattsToResolve = (ContactSlotData.WattsToResolve?.GenerateValue(TraderContact.Randomize) ?? _wattsToResolveDefault),
			MinimumWattsToResolve = (ContactSlotData.MinimumWattsVisible?.GenerateValue(TraderContact.Randomize) ?? _minWattsVisibleDefault),
			MinimumWattsToContact = (ContactSlotData.MinimumWattsToContact?.GenerateValue(TraderContact.Randomize) ?? _minWattsContactDefault),
			SecondsRequiredToContact = (ContactSlotData.SecondsToContact?.GenerateValue(TraderContact.Randomize) ?? _secondsRequiredToContactDefault),
			Lifetime = (ContactSlotData.LifeTime?.GenerateValue(TraderContact.Randomize) ?? _lifeTimeDefault),
			InitialLifeTime = GameManager.GameTime,
			BulkMultiplier = (ContactSlotData.BulkData?.Value ?? 1f)
		};
		CurrentContact.EndLifetime = CurrentContact.InitialLifeTime + CurrentContact.Lifetime;
		CurrentContact.Angle.y = Math.Abs(CurrentContact.Angle.y);
		CurrentContact.DataInstance = traderData.Instantiate(seed, CurrentContact, 0L);
		CurrentContact.DataInstance.ApplyBulkMultiplier();
		CurrentContact.ContactName = CurrentContact.DataInstance.DisplayName;
		ContactSlotData.ConditionSelect?.Apply(CurrentContact);
		CurrentContact.ApplyShuttleVariant();
		CurrentDownTime = ContactSlotData.DownTime?.GenerateValue(TraderContact.Randomize) ?? 0f;
		WorldLog.Append(new TraderEnteredRangeEvent(GameStrings.TraderEnteredRangeEvent.AsString(CurrentContact.ContactName)));
		return true;
	}

	public static ContactSlot Find(int contactDataContactSlotId)
	{
		foreach (ContactSlot contactSlot in ContactSlots)
		{
			if (contactSlot != null && contactSlot.IdHash == contactDataContactSlotId)
			{
				return contactSlot;
			}
		}
		return null;
	}

	public static void SaveContactSlots()
	{
	}

	public static void LoadContactSlots(List<ContactSlotSaveData> contactSlotSaveDatas)
	{
		foreach (ContactSlotSaveData contactSlotSaveData in contactSlotSaveDatas)
		{
			if (contactSlotSaveData == null)
			{
				continue;
			}
			ContactSlot contactSlot = Find(contactSlotSaveData.IdHash);
			if (contactSlot != null)
			{
				contactSlot.SavedContactId = contactSlotSaveData.CurrentContactReferenceId;
				if (contactSlotSaveData.DownTimeRemaining > 0f && contactSlot.SavedContactId == 0L)
				{
					contactSlot.CurrentDownTime = contactSlotSaveData.DownTimeRemaining;
				}
				else
				{
					contactSlot.CurrentDownTime = contactSlotSaveData.CurrentDownTime;
				}
			}
		}
	}

	public static void ClearAll()
	{
		foreach (ContactSlot contactSlot in ContactSlots)
		{
			contactSlot.Clear();
		}
	}

	public static ContactSlot Get(int slotId)
	{
		foreach (ContactSlot contactSlot in ContactSlots)
		{
			if (contactSlot.ContactSlotData.IdHash == slotId)
			{
				return contactSlot;
			}
		}
		return null;
	}
}
