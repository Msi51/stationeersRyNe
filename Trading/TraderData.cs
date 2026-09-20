using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Serialization;
using Assets.Scripts;
using UnityEngine;

namespace Trading;

public class TraderData
{
	public static List<TraderData> AllTraderData = new List<TraderData>();

	[XmlAttribute("Id")]
	public string Id;

	[XmlAttribute("ShuttleVariant")]
	public ShuttleVariant ShuttleVariant;

	[XmlIgnore]
	public int IdHash;

	[XmlElement("Type")]
	public List<SlotIdReference> SlotTypes = new List<SlotIdReference>();

	[XmlElement("Name")]
	public List<LocalizedStringReference> Names = new List<LocalizedStringReference>();

	[XmlElement("Buy")]
	public List<BuyData> BuyData = new List<BuyData>();

	[XmlElement("Sell")]
	public List<SellData> SellData = new List<SellData>();

	[XmlIgnore]
	private List<TraderItemPool> _itemPools = new List<TraderItemPool>(8);

	public const int ChecksumPrime = 41;

	public string TraderChecksum()
	{
		if (string.IsNullOrEmpty(Id))
		{
			throw new NullReferenceException("Id checksum");
		}
		int idHash = IdHash;
		idHash ^= ShuttleVariant.GetHashCode() * 41;
		idHash ^= BuyData.Count * 41;
		for (int i = 0; i < SlotTypes.Count; i++)
		{
			idHash ^= SlotTypes[i].GetChecksum() * 41;
		}
		for (int j = 0; j < Names.Count; j++)
		{
			LocalizedStringReference localizedStringReference = Names[j];
			idHash ^= localizedStringReference.GetChecksum() * 41;
		}
		for (int k = 0; k < BuyData.Count; k++)
		{
			BuyData buyData = BuyData[k];
			idHash ^= buyData.GetChecksum() * 41;
		}
		idHash ^= SellData.Count * 41;
		for (int l = 0; l < SellData.Count; l++)
		{
			SellData sellData = SellData[l];
			idHash ^= sellData.GetChecksum() * 41;
		}
		return idHash.ToString("X", NumberFormatInfo.InvariantInfo);
	}

	public void Initialize(ModAbout mod)
	{
		_itemPools.Clear();
		IdHash = Animator.StringToHash(Id);
		if (WorldManager.Instance != null)
		{
			foreach (TraderData allTraderDatum in AllTraderData)
			{
				if (allTraderDatum.IdHash == IdHash && allTraderDatum != this)
				{
					ConsoleWindow.PrintError("Error! Failed to load a trader data with the same Id as " + Id + ". Ids must be unique.");
					return;
				}
			}
		}
		foreach (BuyData buyDatum in BuyData)
		{
			buyDatum.Initialize(mod);
		}
		foreach (SellData sellDatum in SellData)
		{
			sellDatum.Initialize(mod);
			AddToItemPoolLookup(sellDatum);
		}
		foreach (SlotIdReference slotType in SlotTypes)
		{
			slotType.Initialise();
		}
		AllTraderData.Add(this);
	}

	public void Validate()
	{
		for (int num = BuyData.Count - 1; num >= 0; num--)
		{
			BuyData buyData = BuyData[num];
			if (buyData == null)
			{
				BuyData.RemoveAt(num);
				ConsoleWindow.PrintAction("Removed a null BuyData from Trader: " + Id + ".");
			}
			else
			{
				if (buyData.IdHash != 0 && !buyData.IsValid())
				{
					BuyData transactionData = DataCollection.Get<BuyData>(buyData.IdHash);
					buyData.Apply(transactionData);
				}
				AddToItemPoolLookup(buyData);
				if (!buyData.IsValid())
				{
					ConsoleWindow.PrintError($"BuyData {buyData.Id}: {buyData.Name} is invalid.");
				}
			}
		}
		for (int num2 = SellData.Count - 1; num2 >= 0; num2--)
		{
			SellData sellData = SellData[num2];
			if (sellData == null)
			{
				SellData.RemoveAt(num2);
				ConsoleWindow.PrintAction("Removed a null SellData from Trader: " + Id + ".");
			}
			else
			{
				if (sellData.IdHash != 0 && !sellData.IsValid())
				{
					SellData transactionData2 = DataCollection.Get<SellData>(sellData.IdHash);
					sellData.Apply(transactionData2);
				}
				AddToItemPoolLookup(sellData);
				if (!sellData.IsValid())
				{
					ConsoleWindow.PrintError($"SellData {sellData.Id}: {sellData.Name} is invalid.");
				}
			}
		}
	}

	private TraderItemPool GetItemPool(int index)
	{
		foreach (TraderItemPool itemPool in _itemPools)
		{
			if (itemPool.Index == index)
			{
				return itemPool;
			}
		}
		TraderItemPool traderItemPool = new TraderItemPool(index);
		_itemPools.Add(traderItemPool);
		return traderItemPool;
	}

	public void AddToItemPoolLookup(TransactionData transactionData)
	{
		if (transactionData.ItemPoolData != null)
		{
			GetItemPool(transactionData.ItemPoolData.Index).Add(transactionData);
		}
	}

	public TraderDataInstance Instantiate(int seed, TraderContact contactParent, long referenceId = 0L)
	{
		TraderDataInstance traderDataInstance = new TraderDataInstance(this, contactParent, seed, referenceId);
		bool flag = referenceId == 0;
		foreach (BuyData buyDatum in BuyData)
		{
			if (buyDatum.ItemPoolData == null && (buyDatum.ChanceData == null || !(traderDataInstance.Random.NextDouble() > (double)buyDatum.ChanceData.Value)) && (buyDatum.WorldCondition == null || buyDatum.WorldCondition.Evaluate()) && !InValidForSlot(buyDatum.SlotTypes, contactParent))
			{
				BuyDataInstance buyDataInstance = new BuyDataInstance(buyDatum, traderDataInstance.Random);
				traderDataInstance.BuyDataInstances.Add(buyDataInstance);
				if (flag)
				{
					Referencable.RegisterNew(buyDataInstance);
				}
			}
		}
		foreach (SellData sellDatum in SellData)
		{
			if (sellDatum.ItemPoolData == null && (sellDatum.ChanceData == null || !(traderDataInstance.Random.NextDouble() > (double)sellDatum.ChanceData.Value)) && (sellDatum.WorldCondition == null || sellDatum.WorldCondition.Evaluate()) && !InValidForSlot(sellDatum.SlotTypes, contactParent))
			{
				SellDataInstance sellDataInstance = new SellDataInstance(sellDatum, traderDataInstance.Random);
				traderDataInstance.SellDataInstances.Add(sellDataInstance);
				if (flag)
				{
					Referencable.RegisterNew(sellDataInstance);
				}
			}
		}
		foreach (TraderItemPool itemPool in _itemPools)
		{
			if (itemPool == null)
			{
				continue;
			}
			TransactionData transactionData = itemPool.Pick(traderDataInstance.Random);
			if ((transactionData.WorldCondition != null && !transactionData.WorldCondition.Evaluate()) || InValidForSlot(transactionData.SlotTypes, contactParent))
			{
				continue;
			}
			if (!(transactionData is SellData sellData))
			{
				if (transactionData is BuyData buyData)
				{
					BuyDataInstance buyDataInstance2 = new BuyDataInstance(buyData, traderDataInstance.Random);
					traderDataInstance.BuyDataInstances.Add(buyDataInstance2);
					if (flag)
					{
						Referencable.RegisterNew(buyDataInstance2);
					}
				}
			}
			else
			{
				SellDataInstance sellDataInstance2 = new SellDataInstance(sellData, traderDataInstance.Random);
				traderDataInstance.SellDataInstances.Add(sellDataInstance2);
				if (flag)
				{
					Referencable.RegisterNew(sellDataInstance2);
				}
			}
		}
		return traderDataInstance;
	}

	public static bool InValidForSlot(List<SlotIdReference> slotTypes, TraderContact contactParent)
	{
		bool result = false;
		if (slotTypes.Count > 0)
		{
			result = true;
			foreach (SlotIdReference slotType in slotTypes)
			{
				if (slotType.SlotIdHash == contactParent.ContactSlot.IdHash)
				{
					result = false;
					break;
				}
			}
		}
		return result;
	}

	public static TraderData Find(int idHash)
	{
		foreach (TraderData allTraderDatum in AllTraderData)
		{
			if (allTraderDatum.IdHash == idHash)
			{
				return allTraderDatum;
			}
		}
		return null;
	}
}
