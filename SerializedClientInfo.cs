using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Networking;

[XmlRoot]
public class SerializedClientInfo
{
	public ulong ClientId;

	public int StartLocationHash;

	public long SpawnPointReference;

	public SerializedClientInfo()
	{
	}

	public SerializedClientInfo(ulong clientId, int startLocationHash, long spawnPointReference)
	{
		ClientId = clientId;
		StartLocationHash = startLocationHash;
		SpawnPointReference = spawnPointReference;
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteUInt64(ClientId);
		writer.WriteInt32(StartLocationHash);
		Network.WritePackedId(writer, SpawnPointReference);
	}

	public static SerializedClientInfo Read(RocketBinaryReader reader)
	{
		ulong clientId = reader.ReadUInt64();
		int startLocationHash = reader.ReadInt32();
		Network.ReadPackedId(reader, out var referenceId);
		return new SerializedClientInfo(clientId, startLocationHash, referenceId);
	}

	public static void UpdateSpawnPointReference(ulong clientId, long spawnPointRef)
	{
		SerializedClientInfo clientInfo = GameManager.GetClientInfo(clientId);
		if (clientInfo != null)
		{
			clientInfo.SpawnPointReference = spawnPointRef;
			if (NetworkManager.IsServer)
			{
				GameManager.ResendClientInfo = true;
			}
		}
		else
		{
			ConsoleWindow.PrintError($"Client {clientId} does not exist");
		}
	}
}
