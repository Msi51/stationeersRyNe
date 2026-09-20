using System;
using Assets.Scripts;
using Assets.Scripts.Networking;

namespace UnityEngine.Networking;

public abstract class MessageBase<T> : IMessageSerialisable where T : MessageBase<T>, new()
{
	private static T _Singleton;

	public static T Singleton
	{
		get
		{
			if (_Singleton == null)
			{
				_Singleton = new T();
			}
			return _Singleton;
		}
	}

	public abstract void Deserialize(RocketBinaryReader reader);

	public abstract void Serialize(RocketBinaryWriter writer);

	public bool Send()
	{
		if (NetworkManager.IsServer)
		{
			return SendToClients();
		}
		if (NetworkManager.IsClient)
		{
			return SendToServer();
		}
		return false;
	}

	public bool SendToServer()
	{
		if (NetworkManager.IsServer)
		{
			throw new Exception("Server cannot send messages. Only client to server");
		}
		NetworkClient.SendToServer(this);
		return true;
	}

	public bool SendToClients()
	{
		if (!NetworkManager.IsServer)
		{
			throw new Exception("Messages are not meant to go to clients. Only client to server");
		}
		NetworkServer.SendToClients(this, NetworkChannel.GeneralTraffic, -1L);
		return true;
	}
}
