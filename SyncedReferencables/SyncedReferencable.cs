using System;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using Trading;

namespace SyncedReferencables;

public abstract class SyncedReferencable : IReferencable, IEvaluable, ISyncListable, IDensePoolable
{
	private readonly DensePoolReference<SyncedReferencable> _allSyncedPool = new DensePoolReference<SyncedReferencable>(SyncedReferencableManager.AllSynced);

	public abstract string DisplayName { get; }

	public ushort NetworkUpdateFlags { get; set; }

	public long ReferenceId { get; set; }

	public bool BeingDestroyed { get; set; }

	public static SyncedReferencable CreateAs(int typeId, long referenceId)
	{
		if (GameManager.IsThread)
		{
			throw new InvalidOperationException($"SyncedReferencable.CreateAs called off the main thread for typeId {typeId}");
		}
		SyncedReferencable syncedReferencable = SyncedReferencableTypes.Create(typeId);
		Referencable.RegisterAs(syncedReferencable, referenceId);
		return syncedReferencable;
	}

	public static void Announce(SyncedReferencable synced)
	{
		if (synced != null)
		{
			if (GameManager.IsThread)
			{
				throw new InvalidOperationException("SyncedReferencable.Announce called off the main thread for " + synced.DisplayName);
			}
			if (!GameManager.RunSimulation)
			{
				throw new InvalidOperationException("Announce can only be called from server.");
			}
			if (Referencable.RegisterNew(synced) && NetworkBase.Clients.Count > 0)
			{
				SyncedReferencableManager.SendNew(synced);
			}
		}
	}

	public static void Destroy(SyncedReferencable synced)
	{
		if (synced != null)
		{
			if (GameManager.IsThread)
			{
				throw new InvalidOperationException("SyncedReferencable.Destroy called off the main thread for " + synced.DisplayName);
			}
			if (!GameManager.RunSimulation)
			{
				throw new InvalidOperationException("Destroy can only be called from server.");
			}
			synced.Destroy();
			if (NetworkBase.Clients.Count > 0)
			{
				SyncedReferencableManager.SendDestroy(synced);
			}
		}
	}

	public abstract int GetTypeId();

	public bool ShouldDoNetworkUpdate()
	{
		return NetworkUpdateFlags != 0;
	}

	public void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, this);
		writer.WriteInt32(GetTypeId());
		SerializeNew(writer);
	}

	public void SerializeOnJoin(RocketBinaryWriter writer)
	{
		SerializeNew(writer);
	}

	public void DeserializeOnJoin(RocketBinaryReader reader)
	{
		DeserializeNew(reader);
	}

	protected virtual void SerializeNew(RocketBinaryWriter writer)
	{
	}

	protected virtual void DeserializeNew(RocketBinaryReader reader)
	{
	}

	public virtual void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
	}

	public virtual void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
	}

	public virtual void OnFinishJoin()
	{
	}

	protected virtual void OnDestroy()
	{
	}

	public abstract SyncedReferencableSaveData SerializeSave();

	protected virtual void InitialiseSaveData(SyncedReferencableSaveData saveData)
	{
		saveData.ReferenceId = ReferenceId;
		saveData.TypeId = GetTypeId();
	}

	public virtual void DeserializeSave(SyncedReferencableSaveData saveData)
	{
		ReferenceId = saveData.ReferenceId;
	}

	public void Destroy()
	{
		SyncedReferencableManager.AllSynced.Remove(this);
		Referencable.Deregister(this);
		OnDestroy();
	}

	public static void DeserializeNewAndCreate(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		CreateAs(reader.ReadInt32(), referenceId).DeserializeNew(reader);
	}

	public bool OnAddToPool(object densePool, int slot)
	{
		if (_allSyncedPool.CanAddToPool(densePool))
		{
			return _allSyncedPool.AddToPool(densePool, slot);
		}
		return false;
	}

	public void OnRemoveFromPool(object densePool)
	{
		_allSyncedPool.OnRemovedFrom(densePool);
	}

	public void PrintDebugInfo(bool verbose = false)
	{
	}

	public void OnAssignedReference()
	{
		SyncedReferencableManager.AllSynced.Add(this);
	}
}
