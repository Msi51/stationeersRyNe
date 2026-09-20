using Assets.Scripts;
using Assets.Scripts.Networking;
using SyncedReferencables;
using UnityEngine;

namespace Objects.Electrical;

public class PylonConnection : SyncedReferencable
{
	public static readonly int TypeId = Animator.StringToHash("PylonConnection");

	private long _fromRefId;

	private byte _fromIndex;

	private long _toRefId;

	private byte _toIndex;

	private PylonNode _fromNode;

	private PylonNode _toNode;

	public override string DisplayName => "PylonConnection";

	public override int GetTypeId()
	{
		return TypeId;
	}

	public PylonNode GetOther(PylonNode node)
	{
		if (node != _fromNode)
		{
			return _fromNode;
		}
		return _toNode;
	}

	public PylonConnection()
	{
	}

	private PylonConnection(PylonNode from, PylonNode to)
	{
		_fromRefId = from.Owner.GetRefId();
		_fromIndex = from.Index;
		_toRefId = to.Owner.GetRefId();
		_toIndex = to.Index;
		_fromNode = from;
		_toNode = to;
	}

	public static PylonConnection CreateNew(PylonNode from, PylonNode to)
	{
		PylonConnection pylonConnection = new PylonConnection(from, to);
		SyncedReferencable.Announce(pylonConnection);
		PylonHelper.ConnectNodes(pylonConnection, from, to);
		return pylonConnection;
	}

	private void ResolveAndConnect()
	{
		_fromNode = PylonHelper.FindNode(_fromRefId, _fromIndex);
		_toNode = PylonHelper.FindNode(_toRefId, _toIndex);
		if (_fromNode == null || _toNode == null)
		{
			ConsoleWindow.PrintError($"{DisplayName} #{base.ReferenceId}: could not resolve nodes {_fromRefId}:{_fromIndex} -> {_toRefId}:{_toIndex}, destroying");
			Destroy();
		}
		else
		{
			PylonHelper.ConnectNodes(this, _fromNode, _toNode);
		}
	}

	public void DisconnectLocal()
	{
		PylonHelper.DisconnectNodes(this, _fromNode, _toNode);
		_fromNode = null;
		_toNode = null;
	}

	protected override void OnDestroy()
	{
		DisconnectLocal();
	}

	protected override void SerializeNew(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, _fromRefId);
		writer.WriteByte(_fromIndex);
		Network.WritePackedId(writer, _toRefId);
		writer.WriteByte(_toIndex);
	}

	protected override void DeserializeNew(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out _fromRefId);
		_fromIndex = reader.ReadByte();
		Network.ReadPackedId(reader, out _toRefId);
		_toIndex = reader.ReadByte();
		ResolveAndConnect();
	}

	public override SyncedReferencableSaveData SerializeSave()
	{
		SyncedReferencableSaveData syncedReferencableSaveData = new PylonConnectionSaveData();
		InitialiseSaveData(syncedReferencableSaveData);
		return syncedReferencableSaveData;
	}

	protected override void InitialiseSaveData(SyncedReferencableSaveData saveData)
	{
		base.InitialiseSaveData(saveData);
		if (saveData is PylonConnectionSaveData pylonConnectionSaveData)
		{
			pylonConnectionSaveData.FromRefId = _fromRefId;
			pylonConnectionSaveData.FromIndex = _fromIndex;
			pylonConnectionSaveData.ToRefId = _toRefId;
			pylonConnectionSaveData.ToIndex = _toIndex;
		}
	}

	public override void DeserializeSave(SyncedReferencableSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is PylonConnectionSaveData pylonConnectionSaveData)
		{
			_fromRefId = pylonConnectionSaveData.FromRefId;
			_fromIndex = pylonConnectionSaveData.FromIndex;
			_toRefId = pylonConnectionSaveData.ToRefId;
			_toIndex = pylonConnectionSaveData.ToIndex;
			ResolveAndConnect();
		}
	}
}
