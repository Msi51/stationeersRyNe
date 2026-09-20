using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using TraderUI;
using UnityEngine;

namespace Trading;

public class TraderDataInstance : IReferencable, IEvaluable
{
	public readonly TraderData TraderData;

	public readonly List<BuyDataInstance> BuyDataInstances = new List<BuyDataInstance>();

	public readonly List<SellDataInstance> SellDataInstances = new List<SellDataInstance>();

	public readonly TraderContact ContactParent;

	public readonly System.Random Random;

	public readonly int Seed;

	public string DisplayName { get; }

	public ushort NetworkUpdateFlags { get; set; }

	public long ReferenceId { get; set; }

	public bool BeingDestroyed { get; set; }

	public TraderDataInstance(TraderData traderData, TraderContact contactParent, int seed, long referenceId)
	{
		Seed = seed;
		TraderData = traderData;
		ContactParent = contactParent;
		Random = new System.Random(Seed);
		DisplayName = ((traderData.Names.Count > 0) ? ((string)traderData.Names.Pick(Random)) : "Unnamed Trader");
		if (referenceId == 0L)
		{
			Referencable.RegisterNew(this);
		}
		else
		{
			Referencable.RegisterAs(this, referenceId);
		}
	}

	public static void Write(RocketBinaryWriter writer, TraderDataInstance traderDataInstance)
	{
		bool flag = traderDataInstance?.TraderData == null;
		writer.WriteBoolean(flag);
		if (flag)
		{
			return;
		}
		writer.WriteInt32(traderDataInstance.TraderData.IdHash);
		writer.WriteString(traderDataInstance.TraderData.TraderChecksum());
		writer.WriteInt32(traderDataInstance.Seed);
		writer.WriteInt64(traderDataInstance.ReferenceId);
		foreach (BuyDataInstance buyDataInstance in traderDataInstance.BuyDataInstances)
		{
			writer.WriteInt64(buyDataInstance.ReferenceId);
			buyDataInstance.Write(writer);
		}
		foreach (SellDataInstance sellDataInstance in traderDataInstance.SellDataInstances)
		{
			writer.WriteInt64(sellDataInstance.ReferenceId);
			sellDataInstance.Write(writer);
		}
	}

	public static TraderDataInstance Read(RocketBinaryReader reader, TraderContact contactParent)
	{
		if (reader.ReadBoolean())
		{
			return null;
		}
		int num = reader.ReadInt32();
		TraderData traderData = TraderData.Find(num);
		if (traderData == null)
		{
			throw new NullReferenceException($"Couldn't find TraderData of Hash: {num} Check you have the same mods installed as the server.");
		}
		if (!reader.ReadString().Equals(traderData.TraderChecksum()))
		{
			throw new NullReferenceException("TraderData: " + traderData.Id + " has a different checksum that the server's data. ensure your trader mod data matches the server.");
		}
		int seed = reader.ReadInt32();
		long referenceId = reader.ReadInt64();
		TraderDataInstance traderDataInstance = traderData.Instantiate(seed, contactParent, referenceId);
		foreach (BuyDataInstance buyDataInstance in traderDataInstance.BuyDataInstances)
		{
			long referenceId2 = reader.ReadInt64();
			Referencable.RegisterAs(buyDataInstance, referenceId2);
			buyDataInstance.Read(reader);
		}
		foreach (SellDataInstance sellDataInstance in traderDataInstance.SellDataInstances)
		{
			long referenceId3 = reader.ReadInt64();
			Referencable.RegisterAs(sellDataInstance, referenceId3);
			sellDataInstance.Read(reader);
		}
		return traderDataInstance;
	}

	public void SerializeDeltaState(RocketBinaryWriter writer)
	{
		foreach (BuyDataInstance buyDataInstance in BuyDataInstances)
		{
			buyDataInstance.Write(writer);
		}
		foreach (SellDataInstance sellDataInstance in SellDataInstances)
		{
			sellDataInstance.Write(writer);
		}
	}

	public void DeserializeDeltaState(RocketBinaryReader reader)
	{
		foreach (BuyDataInstance buyDataInstance in BuyDataInstances)
		{
			buyDataInstance.Read(reader);
		}
		foreach (SellDataInstance sellDataInstance in SellDataInstances)
		{
			sellDataInstance.Read(reader);
		}
		TraderCanvas.RefreshNextFrame(ContactParent).Forget();
	}

	public void PrintDebugInfo(bool verbose = false)
	{
	}

	public void OnAssignedReference()
	{
	}

	public TraderInstanceSaveData SerializeInstanceData()
	{
		TraderInstanceSaveData traderInstanceSaveData = new TraderInstanceSaveData
		{
			ReferenceId = ReferenceId,
			TraderDataId = TraderData.IdHash,
			Seed = Seed,
			Checksum = TraderData.TraderChecksum()
		};
		foreach (BuyDataInstance buyDataInstance in BuyDataInstances)
		{
			traderInstanceSaveData.BuySaveData.Add(new BuySaveData
			{
				ReferenceId = buyDataInstance.ReferenceId,
				Required = buyDataInstance.Required
			});
		}
		foreach (SellDataInstance sellDataInstance in SellDataInstances)
		{
			traderInstanceSaveData.SellSaveData.Add(new SellSaveData
			{
				ReferenceId = sellDataInstance.ReferenceId,
				Stock = sellDataInstance.Stock
			});
		}
		return traderInstanceSaveData;
	}

	public static TraderDataInstance DeserializeInstanceSaveData(TraderInstanceSaveData saveData, TraderContact parentContact)
	{
		if (parentContact == null || saveData == null)
		{
			return null;
		}
		TraderData traderData = TraderData.Find(saveData.TraderDataId);
		if (traderData == null || saveData.ReferenceId == 0L)
		{
			return null;
		}
		TraderDataInstance traderDataInstance = traderData.Instantiate(saveData.Seed, parentContact, saveData.ReferenceId);
		if (string.IsNullOrEmpty(saveData.Checksum) || !traderData.TraderChecksum().Equals(saveData.Checksum))
		{
			return null;
		}
		if (saveData.BuySaveData.Count != traderDataInstance.BuyDataInstances.Count || saveData.SellSaveData.Count != traderDataInstance.SellDataInstances.Count)
		{
			ConsoleWindow.PrintError("TransactionSave Data for trader " + traderData.Id + " out of sync with SaveData. This should have been caught by checksum!");
			return null;
		}
		for (int i = 0; i < saveData.BuySaveData.Count; i++)
		{
			BuyDataInstance buyDataInstance = traderDataInstance.BuyDataInstances[i];
			BuySaveData buySaveData = saveData.BuySaveData[i];
			Referencable.RegisterAs(buyDataInstance, buySaveData.ReferenceId);
			buyDataInstance.Required = buySaveData.Required;
		}
		for (int j = 0; j < saveData.SellSaveData.Count; j++)
		{
			SellDataInstance sellDataInstance = traderDataInstance.SellDataInstances[j];
			SellSaveData sellSaveData = saveData.SellSaveData[j];
			Referencable.RegisterAs(sellDataInstance, sellSaveData.ReferenceId);
			sellDataInstance.Stock = sellSaveData.Stock;
		}
		return traderDataInstance;
	}

	public void ApplyBulkMultiplier()
	{
		foreach (BuyDataInstance buyDataInstance in BuyDataInstances)
		{
			if (buyDataInstance.BuyData.Required.Bulk)
			{
				buyDataInstance.Required = Mathf.Max(1, (int)((float)buyDataInstance.Required * ContactParent.BulkMultiplier));
			}
		}
		foreach (SellDataInstance sellDataInstance in SellDataInstances)
		{
			if (sellDataInstance.SellData.Stock.Bulk)
			{
				sellDataInstance.Stock = Mathf.Max(1, (int)((float)sellDataInstance.Stock * ContactParent.BulkMultiplier));
			}
		}
	}
}
