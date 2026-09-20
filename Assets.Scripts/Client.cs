using System;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts;

public class Client : IComparable<Client>, IEquatable<Client>, IRocketReaderWriter
{
	public string name;

	public string address;

	public int port;

	public ClientState state;

	public ClientUpdateFlag flags = ClientUpdateFlag.DaysLived;

	public float connectTime;

	public ulong ClientId;

	public bool IsHost;

	public float joinProgress;

	public int bytesSent;

	public int RoundTripTime;

	public long connectionId;

	public ushort DaysLived;

	public ConnectionMethod connectionMethod = ConnectionMethod.None;

	public HandshakeType handshake;

	public static int Count => NetworkBase.Clients.Count;

	public Human RegisteredHuman { get; private set; }

	public float GetMessageProgress()
	{
		return Mathf.Clamp((float)bytesSent / (float)NetworkServer.PackagedJoinDataBytes, 0f, 1f);
	}

	public void SetState(ClientState clientState)
	{
		state = clientState;
	}

	public Client()
	{
		connectTime = Time.unscaledTime;
	}

	public Client(long hostId, long connectionId, ulong clientId, string name, ConnectionMethod connectionMethod)
	{
		this.connectionId = connectionId;
		this.connectionMethod = connectionMethod;
		ClientId = clientId;
		this.name = name;
		connectTime = Time.unscaledTime;
	}

	public int CompareTo(Client other)
	{
		if (this == other)
		{
			return 0;
		}
		if (other == null)
		{
			return 1;
		}
		return connectionId.CompareTo(other.connectionId);
	}

	public override string ToString()
	{
		return name;
	}

	public string ToStringOneLine()
	{
		return string.Format("{0} | {1} {2} \t", connectionId, name, IsHost ? "(Host)" : string.Empty) + string.Format("{0}: {1:0.0}s, ", "connectTime", connectTime) + string.Format("{0}: {1}", "ClientId", ClientId);
	}

	public string ToStringNameAndId()
	{
		return $"{name} ({ClientId})";
	}

	public static Client Find(long connectionId)
	{
		if (NetworkManager.HostClient != null && NetworkManager.HostClient.connectionId == connectionId)
		{
			return NetworkManager.HostClient;
		}
		foreach (Client client in NetworkBase.Clients)
		{
			if (client.connectionId == connectionId)
			{
				return client;
			}
		}
		return null;
	}

	public static Client Find(ulong clientId)
	{
		if (NetworkManager.HostClient != null && NetworkManager.HostClient.ClientId == clientId)
		{
			return NetworkManager.HostClient;
		}
		foreach (Client client in NetworkBase.Clients)
		{
			if (client.ClientId == clientId)
			{
				return client;
			}
		}
		return null;
	}

	public void Ban()
	{
		NetworkServer.Ban(this);
	}

	public void Disconnect()
	{
		NetworkServer.ClientDisconnected(connectionId);
		NetworkManager.CloseP2PConnectionServer(this);
	}

	public void SetProgress(float progress)
	{
		joinProgress = progress;
	}

	public void Read(RocketBinaryReader reader)
	{
		ClientId = reader.ReadUInt64();
		name = reader.ReadString();
		address = reader.ReadString();
		port = reader.ReadInt32();
		connectionId = reader.ReadInt64();
		state = (ClientState)reader.ReadByte();
		connectTime = reader.ReadSingle();
		IsHost = reader.ReadBoolean();
		joinProgress = reader.ReadSingle();
		bytesSent = reader.ReadInt32();
		RoundTripTime = reader.ReadInt32();
		DaysLived = reader.ReadUInt16();
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteUInt64(ClientId);
		writer.WriteString(name ?? string.Empty);
		writer.WriteString(address ?? string.Empty);
		writer.WriteInt32(port);
		writer.WriteInt64(connectionId);
		writer.WriteByte((byte)state);
		writer.WriteSingle(connectTime);
		writer.WriteBoolean(IsHost);
		writer.WriteSingle(joinProgress);
		writer.WriteInt32(bytesSent);
		writer.WriteInt32(RoundTripTime);
		ushort value = Entity.GetClientEntity(ClientId)?.DaysLived ?? 0;
		writer.WriteUInt16(value);
	}

	public static void DeserialiseClient(RocketBinaryReader reader)
	{
		ulong num = reader.ReadUInt64();
		string text = reader.ReadString();
		string text2 = reader.ReadString();
		int num2 = reader.ReadInt32();
		long num3 = reader.ReadInt64();
		ClientState clientState = (ClientState)reader.ReadByte();
		float num4 = reader.ReadSingle();
		bool flag = reader.ReadBoolean();
		float num5 = reader.ReadSingle();
		int num6 = reader.ReadInt32();
		int roundTripTime = reader.ReadInt32();
		ushort daysLived = reader.ReadUInt16();
		if (num == 0L)
		{
			return;
		}
		Client client;
		if (flag)
		{
			if (NetworkManager.HostClient == null)
			{
				NetworkManager.HostClient = new Client();
			}
			client = NetworkManager.HostClient;
		}
		else
		{
			client = Find(num) ?? new Client();
			if (!NetworkBase.Clients.Contains(client))
			{
				NetworkBase.AddClient(client);
				Achievements.AssessWelcomeAboard();
			}
		}
		client.ClientId = num;
		client.name = text;
		client.address = text2;
		client.port = num2;
		client.connectionId = num3;
		client.state = clientState;
		client.connectTime = num4;
		client.IsHost = flag;
		client.joinProgress = num5;
		client.bytesSent = num6;
		client.RoundTripTime = roundTripTime;
		client.DaysLived = daysLived;
	}

	public bool Equals(Client other)
	{
		if (other == null)
		{
			return false;
		}
		if (this == other)
		{
			return true;
		}
		if (connectionId == other.connectionId)
		{
			return ClientId == other.ClientId;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((Client)obj);
	}

	public override int GetHashCode()
	{
		return ClientId.GetHashCode();
	}

	public static void Register(Human thing, ulong ownerClientId)
	{
		if (ownerClientId != 0L)
		{
			Client client = Find(ownerClientId);
			if (client != null)
			{
				client.RegisteredHuman = thing;
			}
		}
	}

	public static void SerializeDeltaState(RocketBinaryWriter writer)
	{
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		foreach (Client client in NetworkBase.Clients)
		{
			if (client.flags != ClientUpdateFlag.None)
			{
				writer.WriteUInt64(client.ClientId);
				writer.WriteUInt32(client.DaysLived);
				client.flags = ClientUpdateFlag.None;
				count++;
			}
		}
		Network.WriteIndex(writer, count, bufferIndex);
		writer.WriteBoolean(GameManager.ResendClientInfo);
		if (GameManager.ResendClientInfo)
		{
			GameManager.ResendClientInfo = false;
			GameManager.WriteClientInfo(writer);
		}
	}

	public static void DeserializeDeltaState(RocketBinaryReader reader)
	{
		Network.ReadIndex<byte>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			ulong clientId = reader.ReadUInt64();
			uint num = reader.ReadUInt32();
			Client client = Find(clientId);
			if (client != null)
			{
				client.DaysLived = (ushort)num;
			}
		}
		if (reader.ReadBoolean())
		{
			GameManager.ReadClientInfo(reader);
		}
	}
}
