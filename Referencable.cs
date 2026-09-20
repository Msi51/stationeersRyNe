using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Trading;

public static class Referencable
{
	public static long NextReferenceId = 1L;

	public static object NextReferenceIdLock = new object();

	public static readonly List<IReferencable> ReferencablesChanged = new List<IReferencable>();

	public static readonly Dictionary<long, IReferencable> Referencables = new Dictionary<long, IReferencable>();

	public static List<IReferencable> FailedToRegister = new List<IReferencable>();

	public const long INVALID = 0L;

	public static bool CompareBaseObjects(IReferencable lhs, IReferencable rhs)
	{
		bool flag = lhs == null;
		bool flag2 = rhs == null;
		if (flag2 && flag)
		{
			return true;
		}
		if (flag2)
		{
			return false;
		}
		if (flag)
		{
			return false;
		}
		return lhs.ReferenceId == rhs.ReferenceId;
	}

	public static bool RegisterNew(IReferencable iReferencable)
	{
		if (NetworkManager.IsClient)
		{
			string text = "Error: Clients should not be assigning a new Reference";
			if (iReferencable != null)
			{
				text = text + ". Client is trying to assign a new reference for " + iReferencable.DisplayName;
			}
			throw new Exception(text);
		}
		if (iReferencable == null)
		{
			ConsoleWindow.PrintError("Can't register a null IReferencable");
			return false;
		}
		if (iReferencable.ReferenceId != 0L)
		{
			ConsoleWindow.PrintError($"error trying to register '{iReferencable.DisplayName}' as it already is with '{iReferencable.ReferenceId}'");
			return false;
		}
		lock (NextReferenceIdLock)
		{
			if (Referencables.ContainsKey(NextReferenceId))
			{
				ConsoleWindow.PrintError($"error trying to register '{iReferencable.DisplayName}' with referenceId of '{NextReferenceId}' as it is already Assigned to: '{Referencables[NextReferenceId].DisplayName}'");
				return false;
			}
			iReferencable.ReferenceId = NextReferenceId;
			NextReferenceId++;
		}
		lock (Referencables)
		{
			Referencables.Add(iReferencable.ReferenceId, iReferencable);
		}
		iReferencable.OnAssignedReference();
		DeferredMessageQueue.NotifyRegistered(iReferencable.ReferenceId);
		return true;
	}

	public static bool RegisterAs(IReferencable thing, long referenceId, bool force = false)
	{
		lock (Referencables)
		{
			if (thing == null)
			{
				ConsoleWindow.PrintError("Can't register a null IReferencable");
				return false;
			}
			if (referenceId == 0L)
			{
				ConsoleWindow.PrintError("error trying to assign " + thing.DisplayName + " a null reference id");
				FailedToRegister.Add(thing);
				return false;
			}
			if (!force && thing.ReferenceId != 0L)
			{
				ConsoleWindow.PrintError($"error trying to register '{thing.DisplayName}' with id '{referenceId}' as it already has been registered with '{thing.ReferenceId}' ");
				FailedToRegister.Add(thing);
				return false;
			}
			if (Referencables.ContainsKey(referenceId))
			{
				ConsoleWindow.PrintError($"Couldn't register '{thing.DisplayName}' with id '{referenceId}' as there is already an entry for this id. It is used by: {Referencables[referenceId].DisplayName}");
				FailedToRegister.Add(thing);
				return false;
			}
			lock (NextReferenceIdLock)
			{
				thing.ReferenceId = referenceId;
				referenceId++;
				NextReferenceId = ((referenceId > NextReferenceId) ? referenceId : NextReferenceId);
			}
			Referencables.Add(thing.ReferenceId, thing);
			thing.OnAssignedReference();
			DeferredMessageQueue.NotifyRegistered(thing.ReferenceId);
		}
		return true;
	}

	public static void AssignNewIdToDuplicates()
	{
		foreach (IReferencable item in FailedToRegister)
		{
			RegisterNew(item);
			ConsoleWindow.PrintAction(item.DisplayName + "' has been assigned a new Id: " + StringManager.Get(item.ReferenceId));
		}
		FailedToRegister.Clear();
	}

	public static void Deregister(IReferencable iReferencable)
	{
		iReferencable.BeingDestroyed = true;
		lock (Referencables)
		{
			if (Referencables.ContainsKey(iReferencable.ReferenceId))
			{
				Referencables.Remove(iReferencable.ReferenceId);
			}
		}
	}

	public static T Find<T>(long referenceId) where T : class, IReferencable
	{
		if (referenceId <= 0)
		{
			return null;
		}
		Referencables.TryGetValue(referenceId, out var value);
		if (!(value is T result))
		{
			return null;
		}
		return result;
	}

	public static bool Exists<T>(long referenceId, out T thing) where T : class, IReferencable
	{
		if (referenceId <= 0)
		{
			thing = null;
			return false;
		}
		Referencables.TryGetValue(referenceId, out var value);
		if (!(value is T val))
		{
			thing = null;
			return false;
		}
		thing = val;
		return true;
	}

	public static IReferencable Find(long referenceId)
	{
		if (referenceId <= 0)
		{
			return null;
		}
		Referencables.TryGetValue(referenceId, out var value);
		return value;
	}

	public static void ClearReferences()
	{
		lock (Referencables)
		{
			Referencables.Clear();
		}
		lock (NextReferenceIdLock)
		{
			NextReferenceId = 1L;
		}
	}

	public static bool UpdateRequired(byte source, byte toCheck)
	{
		return (source & toCheck) != 0;
	}

	public static void FindAndSetNextReferenceId(XmlSaveLoad.WorldData worldData)
	{
		long highest = 0L;
		foreach (StationContactData stationContact in worldData.StationContacts)
		{
			highest = Math.Max(highest, stationContact.ReferenceId);
			highest = Math.Max(highest, stationContact.InstanceSaveData?.ReferenceId ?? 0);
			if (stationContact.InstanceSaveData == null)
			{
				continue;
			}
			foreach (BuySaveData buySaveDatum in stationContact.InstanceSaveData.BuySaveData)
			{
				highest = Math.Max(highest, buySaveDatum.ReferenceId);
			}
			foreach (SellSaveData sellSaveDatum in stationContact.InstanceSaveData.SellSaveData)
			{
				highest = Math.Max(highest, sellSaveDatum.ReferenceId);
			}
		}
		UpdateHighestId(worldData.PipeNetworks, ref highest);
		UpdateHighestId(worldData.CableNetworks, ref highest);
		UpdateHighestId(worldData.ChuteNetworks, ref highest);
		UpdateHighestId(worldData.LandingPadNetworks, ref highest);
		UpdateHighestId(worldData.RocketNetworks, ref highest);
		UpdateHighestId(worldData.RocketShuttleNetworks, ref highest);
		UpdateHighestId(worldData.GetThings(), ref highest);
		UpdateHighestId(worldData.Atmospheres, ref highest);
		UpdateHighestId(worldData.RocketSaveDatas, ref highest);
		UpdateHighestId(worldData.Objectives, ref highest);
		UpdateHighestId(worldData.RoboticArmNetworks, ref highest);
		UpdateHighestId(worldData.SyncedReferencables, ref highest);
		lock (NextReferenceIdLock)
		{
			NextReferenceId = highest + 1;
		}
	}

	private static void UpdateHighestId(long[] inputArray, ref long highest)
	{
		if (inputArray != null)
		{
			foreach (long val in inputArray)
			{
				highest = Math.Max(highest, val);
			}
		}
	}

	private static void UpdateHighestId<T>(List<T> inputList, ref long highest) where T : IReferencableSaveData
	{
		if (inputList == null)
		{
			return;
		}
		foreach (T input in inputList)
		{
			highest = Math.Max(highest, input.SavedReferenceId);
		}
	}
}
