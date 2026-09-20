using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;

namespace SyncedReferencables;

public static class SyncedReferencableManager
{
	private static readonly SyncList<SyncedReferencable> _newToSend = new SyncList<SyncedReferencable>(SyncedReferencable.DeserializeNewAndCreate);

	private static readonly SyncList<SyncedReferencableDestroyEvent> _destroyToSend = new SyncList<SyncedReferencableDestroyEvent>(SyncedReferencableDestroyEvent.DeserializeDestroy);

	public static readonly ConcurrentDensePool<SyncedReferencable> AllSynced = new ConcurrentDensePool<SyncedReferencable>("AllSynced", 65535);

	private static readonly Action<SyncedReferencable> OnFinishJoinAction = delegate(SyncedReferencable synced)
	{
		if (synced != null && !synced.BeingDestroyed)
		{
			synced.OnFinishJoin();
		}
	};

	public static void SerializeNew(RocketBinaryWriter writer)
	{
		_newToSend.Serialize(writer);
	}

	public static void DeserializeNew(RocketBinaryReader reader)
	{
		_newToSend.Deserialize(reader);
	}

	public static void SerializeDestroy(RocketBinaryWriter writer)
	{
		_destroyToSend.Serialize(writer);
	}

	public static void DeserializeDestroy(RocketBinaryReader reader)
	{
		_destroyToSend.Deserialize(reader);
	}

	public static void SendNew(SyncedReferencable synced)
	{
		_newToSend.Add(synced);
	}

	public static void SendDestroy(SyncedReferencable synced)
	{
		_destroyToSend.Add(new SyncedReferencableDestroyEvent(synced));
	}

	public static void SerializeDeltaState(RocketBinaryWriter writer)
	{
		Network.WriteIndex<uint>(writer, out var count, out var bufferIndex);
		DensePool<SyncedReferencable>.ActiveEnumerable.Enumerator enumerator = AllSynced.Active().GetEnumerator();
		while (enumerator.MoveNext())
		{
			SyncedReferencable current = enumerator.Current;
			if (current != null && current.ReferenceId != 0L && !current.BeingDestroyed && current.ShouldDoNetworkUpdate())
			{
				ushort networkUpdateFlags = current.NetworkUpdateFlags;
				Network.WritePackedId(writer, current);
				writer.WriteNetworkUpdateType(networkUpdateFlags);
				current.BuildUpdate(writer, networkUpdateFlags);
				current.NetworkUpdateFlags = 0;
				count++;
			}
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public static void DeserializeDeltaState(RocketBinaryReader reader)
	{
		string arg = string.Empty;
		Network.ReadIndex<uint>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			Network.ReadPackedId(reader, out var referenceId);
			ushort networkUpdateType = reader.ReadNetworkUpdateType();
			SyncedReferencable syncedReferencable = Referencable.Find<SyncedReferencable>(referenceId);
			if (syncedReferencable != null)
			{
				arg = syncedReferencable.DisplayName;
				syncedReferencable.ProcessUpdate(reader, networkUpdateType);
				continue;
			}
			throw new NullReferenceException($"SyncedReferencable {referenceId} at index {i} is null. Last valid: {arg}");
		}
	}

	public static void SerializeOnJoin(RocketBinaryWriter writer)
	{
		Network.WriteIndex<uint>(writer, out var count, out var bufferIndex);
		DensePool<SyncedReferencable>.ActiveEnumerable.Enumerator enumerator = AllSynced.Active().GetEnumerator();
		while (enumerator.MoveNext())
		{
			SyncedReferencable current = enumerator.Current;
			if (current == null || current.BeingDestroyed)
			{
				continue;
			}
			if (current.ReferenceId == 0L)
			{
				ConsoleWindow.PrintError(current.DisplayName + " has no reference id, skipping and continuing.");
				continue;
			}
			try
			{
				Network.WritePackedId(writer, current);
				writer.WriteInt32(current.GetTypeId());
				current.SerializeOnJoin(writer);
				count++;
			}
			catch (Exception ex)
			{
				ConsoleWindow.PrintError($"fatal error serializing '{current.DisplayName}' #{current.ReferenceId} at index {count} for join package");
				ConsoleWindow.Print(ex.ToString());
			}
		}
		Network.WriteIndex(writer, count, bufferIndex);
		ConsoleWindow.Print($"serialized {count} synced referencables for join package");
	}

	public static void DeserializeOnJoin(RocketBinaryReader reader)
	{
		Network.ReadIndex<uint>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			Network.ReadPackedId(reader, out var referenceId);
			SyncedReferencable.CreateAs(reader.ReadInt32(), referenceId).DeserializeOnJoin(reader);
		}
		AllSynced.ForEach(OnFinishJoinAction);
	}

	public static void AddToWorldData(XmlSaveLoad.WorldData worldData)
	{
		worldData.SyncedReferencables = new List<SyncedReferencableSaveData>();
		DensePool<SyncedReferencable>.ActiveEnumerable.Enumerator enumerator = AllSynced.Active().GetEnumerator();
		while (enumerator.MoveNext())
		{
			SyncedReferencable current = enumerator.Current;
			worldData.SyncedReferencables.Add(current.SerializeSave());
		}
	}

	public static void Load(SyncedReferencableSaveData data)
	{
		SyncedReferencable.CreateAs(data.TypeId, data.ReferenceId).DeserializeSave(data);
	}

	public static void ClearAll()
	{
		AllSynced.Clear();
	}
}
