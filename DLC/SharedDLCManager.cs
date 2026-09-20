using Assets.Scripts;
using Assets.Scripts.Networking;

namespace DLC;

public static class SharedDLCManager
{
	private static ushort _sharedDLC;

	private static ushort NetworkUpdateFlags { get; set; }

	public static ushort SharedDLC
	{
		get
		{
			return _sharedDLC;
		}
		set
		{
			_sharedDLC = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				NetworkUpdateFlags |= 256;
			}
		}
	}

	public static void AddSharedDLC(ushort clientOwnedDLC)
	{
		SharedDLC |= clientOwnedDLC;
	}

	public static void HostFinishedLoad()
	{
		if (!GameManager.IsBatchMode && GameManager.RunSimulation)
		{
			SharedDLC = (ushort)DLCManager.GetOwnedDLC();
		}
	}

	public static void ClientFinishedLoad()
	{
		DLCType ownedDLC = DLCManager.GetOwnedDLC();
		NetworkClient.SendToServer(new AvailableDLCMessage
		{
			DLCType = (ushort)ownedDLC
		});
	}

	public static bool CheckSharedAccess(DLCType dlcType)
	{
		DLCType sharedDLC = (DLCType)SharedDLC;
		return CheckAccess(dlcType, sharedDLC);
	}

	public static void SerializeDeltaState(RocketBinaryWriter writer)
	{
		writer.WriteUInt16(NetworkUpdateFlags);
		if (IsNetworkUpdateRequired(256, NetworkUpdateFlags))
		{
			writer.WriteUInt16(SharedDLC);
		}
		NetworkUpdateFlags = 0;
	}

	public static void DeserializeDeltaState(RocketBinaryReader reader)
	{
		ushort networkUpdateType = reader.ReadUInt16();
		if (IsNetworkUpdateRequired(256, networkUpdateType))
		{
			SharedDLC = reader.ReadUInt16();
		}
	}

	public static void ClearAll()
	{
		SharedDLC = 0;
	}

	private static bool IsNetworkUpdateRequired(ushort toCheck, ushort networkUpdateType)
	{
		return (toCheck & networkUpdateType) != 0;
	}

	private static bool CheckAccess(DLCType dlcType, DLCType ownedDlc)
	{
		if (dlcType == DLCType.None)
		{
			return true;
		}
		return (dlcType & ownedDlc) != 0;
	}
}
