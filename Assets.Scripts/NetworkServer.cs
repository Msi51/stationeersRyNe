using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Networking.GameSessions;
using Networks;
using Objects.Rockets;
using Objects.Rockets.Log;
using SyncedReferencables;
using TerrainSystem;
using UnityEngine;
using UnityEngine.Networking;
using WorldLogSystem;

namespace Assets.Scripts;

public class NetworkServer : NetworkBase
{
	public const int SEND_STATE_TICK_RATE_MS = 50;

	private const int TICK_MAX_MS = 100;

	private const ulong SENDQ_SLOWDOWN_BYTES = 1048576uL;

	private const ulong SENDQ_FULL_SLOWDOWN_BYTES = 4194304uL;

	private const ulong SENDQ_KICK_BYTES = 8388608uL;

	private const int KICK_STUCK_TICKS = 40;

	private static ulong _lastMaxSendQueue;

	private static int _kickStuckTicks;

	private static readonly List<BlacklistedClient> Blacklist = new List<BlacklistedClient>();

	private static readonly Queue<Client> JoiningClients = new Queue<Client>(NetworkManager.MaxConnections);

	private static byte[] _packagedJoinData;

	private static CancellationTokenSource _serverAutoPauseCancelToken;

	private static UniTask _ServerAutoPauseTimerTask;

	private static readonly System.Timers.Timer _masterServerPingTimer = new System.Timers.Timer(30000.0);

	private static RocketBinaryWriter _joinWriter = new RocketBinaryWriter(67108864);

	private static int _fragmentSize = 1024;

	private static bool _processJoinQueueTaskRunning;

	private bool _forceSend;

	public static int CurrentTickIntervalMs { get; private set; } = 50;

	public static uint ReferencableCount => (uint)Referencable.Referencables.Count;

	public static ushort HostPort { get; set; }

	public static bool IsHosting { get; private set; }

	private static bool IsProcessingQueue { get; set; }

	public static uint PackagedJoinDataBytes => (uint)_packagedJoinData.Length;

	public static int FragmentSize
	{
		get
		{
			return _fragmentSize;
		}
		set
		{
			_fragmentSize = Mathf.Clamp(value, 128, 1400);
		}
	}

	public void Awake()
	{
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		Blacklist.Clear();
		LoadBlacklist();
		WorldManager.OnPaused += WorldManagerOnPaused;
		_masterServerPingTimer.Elapsed += MasterServerPingTimerOnElapsed;
	}

	private void OnDestroy()
	{
		StopServer();
	}

	public static void LoadBlacklist()
	{
		Blacklist.Clear();
		if (!File.Exists(BlacklistedClient.PATH))
		{
			return;
		}
		string[] array = File.ReadAllText(BlacklistedClient.PATH).Split(",");
		foreach (string text in array)
		{
			if (text.Contains(":") && text.Split(":").Length > 1)
			{
				ulong id = ulong.Parse(text.Split(":")[1]);
				Blacklist.Add(new BlacklistedClient
				{
					Id = id
				});
			}
			else
			{
				ulong id2 = ulong.Parse(text);
				Blacklist.Add(new BlacklistedClient
				{
					Id = id2
				});
			}
		}
	}

	public static async UniTask Host()
	{
		if (IsHosting || GameManager.GameState == GameState.None || !GameManager.RunSimulation)
		{
			ConsoleWindow.Print($"Failed ToHost. IsHosting: {IsHosting} GameState: {GameManager.GameState} RunSimulation: {GameManager.RunSimulation}");
			return;
		}
		int attempts = 0;
		while (!NetworkManager.StartServer(Convert.ToUInt16(Settings.CurrentData.GamePort)))
		{
			await UniTask.Delay(1000);
			attempts++;
			ConsoleWindow.PrintAction($"Start Server Failed. Attempt {attempts} of {3}.");
			if (attempts >= 3)
			{
				return;
			}
		}
		IsHosting = true;
		NetworkUpdate().Forget();
		CreateNewGameSession();
		_masterServerPingTimer.Start();
	}

	public static void PopulateHostClient()
	{
		NetworkManager.HostClient = new Client
		{
			connectionId = 0L,
			name = NetworkManager.Username,
			ClientId = NetworkManager.LocalClientId,
			IsHost = true,
			state = ClientState.Ready
		};
	}

	private static void PackageJoinData()
	{
		StructureNetwork.ForceNextDeltaFull = true;
		ElectricityManager.ForceNextDeltaFull = true;
		GameManager.SerializeGameTime(_joinWriter);
		GameManager.SerializeTerrainSeed(_joinWriter);
		WorldManager.SerializeOnJoin(_joinWriter);
		Vein.SerializeOnJoin(_joinWriter);
		OrbitalSimulation.SerializeOnJoin(_joinWriter);
		TerraForming.SerializeOnJoin(_joinWriter);
		VoxelTerrain.SerializeOnJoin(_joinWriter);
		StructureNetwork.SerializeOnJoin(_joinWriter);
		CableNetwork.SerializeOnJoin(_joinWriter);
		TraderContact.SerializeOnJoin(_joinWriter);
		SpaceMap.SerializeOnJoin(_joinWriter);
		Rocket.SerializeOnJoin(_joinWriter);
		WorldLog.SerializeOnJoin(_joinWriter);
		WorldObjectiveState.SerializeOnJoin(_joinWriter);
		Thing.SerializeAllOnJoin(_joinWriter);
		SyncedReferencableManager.SerializeOnJoin(_joinWriter);
		RocketLog.SerializeOnJoin(_joinWriter);
		RoomController.SerializeOnJoin(_joinWriter);
		AtmosphericsManager.SerialiseOnJoin(_joinWriter);
		Span<byte> data = _joinWriter.AsSpan();
		_packagedJoinData = NetworkManager.Compress(data);
		ConsoleWindow.Print("Join Package raw data " + data.Length.ToStringPrefix("B") + ", compressed " + ((int)PackagedJoinDataBytes).ToStringPrefix("B"));
		_joinWriter.Reset();
	}

	public static void Serialize(RocketBinaryWriter writer, Thing thing, ref uint count)
	{
		thing.SerializeOnJoin(writer);
		count++;
		foreach (Slot slot in thing.Slots)
		{
			if ((object)slot.Occupant != null)
			{
				Serialize(writer, slot.Occupant, ref count);
			}
		}
	}

	public static void RecalculateFragmentSize()
	{
		int minClientMtu = NetworkManager.GetMinClientMtu();
		if (minClientMtu <= 0)
		{
			FragmentSize = 1024;
			return;
		}
		FragmentSize = minClientMtu - 100;
		ConsoleWindow.Print($"MTU-driven fragment size set to {FragmentSize}B (min client MTU {minClientMtu}B)");
	}

	public static void ClientConnected(long connectionId, ConnectionMethod connectionMethod)
	{
		NetworkMessages.VerifyPlayerRequest message = new NetworkMessages.VerifyPlayerRequest
		{
			ClientConnectionID = connectionId,
			ClientConnectionMethod = connectionMethod,
			PasswordRequired = !string.IsNullOrEmpty(Settings.CurrentData.ServerPassword)
		};
		NetworkManager.SendNetworkMessageDirect(connectionId, connectionMethod, NetworkChannel.GeneralTraffic, message);
	}

	private static void HandleBlacklisting(NetworkMessages.VerifyPlayer msg, Client client)
	{
		SendToClient(new NetworkMessages.Handshake
		{
			HandshakeState = HandshakeType.Rejected,
			Message = NetworkClient.MultiplayerBannedKey
		}, NetworkChannel.GeneralTraffic, client);
		ConsoleWindow.PrintError($"client `{client.address}:{client.port}` rejecting incoming player '{msg.Name}' as they are banned", suppressStacktrace: true);
		CloseRejectedConnection(client).Forget();
	}

	private static void HandleIncorrectPassword(Client client)
	{
		SendToClient(new NetworkMessages.Handshake
		{
			HandshakeState = HandshakeType.Rejected,
			Message = NetworkClient.MultiplayerPasswordKey
		}, NetworkChannel.GeneralTraffic, client);
		ConsoleWindow.PrintError($"client `{client.address}:{client.port}` incorrect password given", suppressStacktrace: true);
		CloseRejectedConnection(client).Forget();
	}

	private static void HandleIncorrectVersion(Client client, NetworkMessages.VerifyPlayer msg)
	{
		SendToClient(new NetworkMessages.Handshake
		{
			HandshakeState = HandshakeType.Rejected,
			Message = NetworkClient.MultiplayerIncorrectVersionKey
		}, NetworkChannel.GeneralTraffic, client);
		ConsoleWindow.PrintError($"client `{client.address}:{client.port}` attempted to connect with incorrect version: {msg.Version}", suppressStacktrace: true);
		CloseRejectedConnection(client).Forget();
	}

	private static async UniTaskVoid CloseRejectedConnection(Client client)
	{
		await UniTask.Delay(500);
		NetworkManager.CloseConnectionServer(client.connectionId, client.connectionMethod);
	}

	public static void VerifyConnection(long hostId, NetworkMessages.VerifyPlayer msg)
	{
		Client client = new Client(hostId, msg.OwnerConnectionId, msg.ClientId, msg.Name, msg.ClientConnectionMethod);
		ConsoleWindow.Print($"Verifying {msg.ClientConnectionMethod.ToString()} connection for player {msg.ClientId}");
		if (Blacklist.Any((BlacklistedClient x) => x.Id == msg.ClientId))
		{
			HandleBlacklisting(msg, client);
			return;
		}
		if (!string.IsNullOrEmpty(Settings.CurrentData.ServerPassword) && Settings.CurrentData.ServerPassword != msg.Password)
		{
			HandleIncorrectPassword(client);
			return;
		}
		if (GameManager.GetGameVersion() != msg.Version)
		{
			HandleIncorrectVersion(client, msg);
			return;
		}
		NetworkBase.AddClient(client);
		Achievements.AssessWelcomeAboard();
		JoiningClients.Enqueue(client);
		client.SetState(ClientState.Queued);
		if (!_processJoinQueueTaskRunning)
		{
			_processJoinQueueTaskRunning = true;
			ProcessJoinQueue().Forget();
		}
	}

	private static async UniTaskVoid ProcessJoinQueue()
	{
		ConsoleWindow.PrintAction("Processing Join Queue");
		NetworkBase.PauseEvent(pause: true);
		int servicedClients = 0;
		while (true)
		{
			if (JoiningClients.Count > 0)
			{
				Client joiningClient = JoiningClients.Peek();
				ConsoleWindow.PrintAction($"{JoiningClients.Count} clients in queue");
				PackageJoinData();
				ConsoleWindow.Print("Sending meta data");
				OnServer.SendMetaData(joiningClient);
				await SendJoinData(joiningClient);
				while (joiningClient.state == ClientState.Connected)
				{
					await UniTask.WaitForEndOfFrame();
				}
				ConsoleWindow.PrintAction("Client " + joiningClient.ToStringNameAndId() + " Dequeued");
				JoiningClients.Dequeue();
				await FragmentHandler.Send(logging: true);
				while (joiningClient.state == ClientState.WaitingForCharacter)
				{
					await UniTask.WaitForEndOfFrame();
				}
				servicedClients++;
			}
			else
			{
				if (NetworkBase.Clients.All((Client x) => x.state == ClientState.Ready) && JoiningClients.Count == 0)
				{
					break;
				}
				await UniTask.WaitForEndOfFrame();
			}
		}
		_processJoinQueueTaskRunning = false;
		NetworkBase.PauseEvent(pause: false);
		ConsoleWindow.PrintAction($"Client join queue processed. {servicedClients} clients serviced");
	}

	private static async UniTask SendJoinData(Client joiningClient)
	{
		RecalculateFragmentSize();
		int num = 0;
		int currentByte = 0;
		int totalBytes = _packagedJoinData.Length;
		ConsoleWindow.Print("Client: " + joiningClient.ToStringNameAndId() + ". Receiving");
		joiningClient.SetState(ClientState.Receiving);
		joiningClient.bytesSent = 0;
		while (currentByte < totalBytes)
		{
			if (joiningClient.state == ClientState.Disconnected)
			{
				ConsoleWindow.Print("Client has disconnected during joining data phase");
				return;
			}
			int num2 = Mathf.Min(totalBytes - currentByte, FragmentSize);
			num++;
			if (!NetworkManager.SendNetworkDataDirect(joiningClient.connectionId, joiningClient.connectionMethod, NetworkChannel.PlayerJoin, _packagedJoinData.AsSpan(currentByte, num2), logWarning: false))
			{
				ConsoleWindow.PrintError($"Error sending join data. Outgoing bytes: {num2}");
			}
			currentByte += num2;
			joiningClient.bytesSent += num2;
			if (num >= 50)
			{
				await UniTask.NextFrame();
				num = 0;
			}
		}
		ConsoleWindow.Print($"Client: {joiningClient.ToStringNameAndId()}. Connected. {currentByte} / {totalBytes}");
		joiningClient.SetState(ClientState.Connected);
	}

	public static void ClientDisconnected(long connectionId)
	{
		Client client = Client.Find(connectionId);
		if (client != null)
		{
			client.SetState(ClientState.Disconnected);
			ConsoleWindow.Print("Client disconnected: " + client.ToStringOneLine());
			NetworkBase.RemoveClient(client);
		}
	}

	public static async UniTask SendToClientReliable<T>(MessageBase<T> message, NetworkChannel channel, Client client) where T : MessageBase<T>, new()
	{
		await NetworkManager.SendNetworkMessageReliable(client, channel, message);
	}

	public static void SendToClient<T>(MessageBase<T> message, NetworkChannel channel, long clientConnectionId) where T : MessageBase<T>, new()
	{
		Client client = Client.Find(clientConnectionId);
		if (client != null)
		{
			NetworkManager.SendNetworkMessageToClient(client, channel, message);
		}
	}

	public static void SendToClient<T>(MessageBase<T> message, NetworkChannel channel, Client client) where T : MessageBase<T>, new()
	{
		NetworkManager.SendNetworkMessageToClient(client, channel, message);
	}

	public static void SendToClients<T>(MessageBase<T> message, NetworkChannel channel = NetworkChannel.GeneralTraffic, long excludeConnectionId = -1L) where T : MessageBase<T>, new()
	{
		NetworkManager.SendNetworkMessageAll(channel, message, excludeConnectionId);
	}

	public static UniTask SendToClientsDirect(byte[] data, NetworkChannel channel = NetworkChannel.GeneralTraffic, bool excludeConnecting = true, long excludeConnectionId = -1L)
	{
		return SendToClientsDirect(data, data.Length, channel, excludeConnecting, excludeConnectionId);
	}

	public static async UniTask SendToClientsDirect(byte[] data, int count, NetworkChannel channel = NetworkChannel.GeneralTraffic, bool excludeConnecting = true, long excludeConnectionId = -1L)
	{
		foreach (Client client in NetworkBase.Clients)
		{
			if (client.connectionId != excludeConnectionId && (!excludeConnecting || client.state == ClientState.Ready || client.state == ClientState.WaitingForCharacter))
			{
				while (!NetworkManager.SendNetworkDataDirect(client.connectionId, client.connectionMethod, channel, new Span<byte>(data, 0, count)))
				{
					await UniTask.NextFrame();
				}
			}
		}
	}

	public static void SendToClientsDirect(Span<byte> data, NetworkChannel channel = NetworkChannel.GeneralTraffic, bool excludeConnecting = true, long excludeConnectionId = -1L)
	{
		foreach (Client client in NetworkBase.Clients)
		{
			if (client.connectionId != excludeConnectionId && (!excludeConnecting || client.state == ClientState.Ready || client.state == ClientState.WaitingForCharacter))
			{
				while (!NetworkManager.SendNetworkDataDirect(client.connectionId, client.connectionMethod, channel, data))
				{
					Thread.Yield();
				}
			}
		}
	}

	private static async UniTaskVoid NetworkUpdate()
	{
		if (!NetworkManager.IsServer)
		{
			return;
		}
		while (true)
		{
			Client worstClient;
			ulong maxClientSendQueueBytes = NetworkManager.GetMaxClientSendQueueBytes(out worstClient);
			if (worstClient != null && maxClientSendQueueBytes >= 8388608 && maxClientSendQueueBytes >= _lastMaxSendQueue)
			{
				_kickStuckTicks++;
			}
			else
			{
				_kickStuckTicks = 0;
			}
			_lastMaxSendQueue = maxClientSendQueueBytes;
			if (worstClient != null && _kickStuckTicks >= 40)
			{
				ConsoleWindow.PrintError($"Kicking {worstClient.ToStringNameAndId()}: send backlog {maxClientSendQueueBytes >> 20}MB and not draining at the minimum tick rate, their connection can't keep up.", suppressStacktrace: true);
				NetworkManager.CloseConnectionServer(worstClient.connectionId, worstClient.connectionMethod);
				worstClient.Disconnect();
				_kickStuckTicks = 0;
			}
			CurrentTickIntervalMs = TickIntervalForBacklog(maxClientSendQueueBytes);
			await UniTask.Delay(CurrentTickIntervalMs);
			await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
			await UniTask.WaitUntil(() => GameManager.GameState != GameState.Paused && JoiningClients.Count <= 0);
			if (NetworkBase.Clients.Count > 0)
			{
				await FragmentHandler.Send();
			}
			else
			{
				NetworkBase._connectionStartTime = Time.unscaledTime;
			}
		}
	}

	private static int TickIntervalForBacklog(ulong maxSendQueue)
	{
		if (maxSendQueue <= 1048576)
		{
			return 50;
		}
		if (maxSendQueue >= 4194304)
		{
			return 100;
		}
		double num = (double)(maxSendQueue - 1048576) / 3145728.0;
		return (int)(50.0 + num * 50.0);
	}

	private static void WorldManagerOnPaused(bool isPaused)
	{
		if (!isPaused)
		{
			_serverAutoPauseCancelToken?.Cancel();
		}
	}

	public static void Handshake(NetworkMessages.Handshake handshake)
	{
		long connectionId = handshake.ConnectionId;
		HandshakeType handshakeState = handshake.HandshakeState;
		Client client = Client.Find(connectionId);
		if (client == null)
		{
			ConsoleWindow.PrintError($"Can't find byteArrayClient {connectionId} to process {handshakeState}", suppressStacktrace: true);
			return;
		}
		client.handshake = handshakeState;
		switch (handshakeState)
		{
		case HandshakeType.ClientReady:
			client.SetState(ClientState.Ready);
			ConsoleWindow.Print("Client " + client.ToStringNameAndId() + " is ready");
			break;
		case HandshakeType.Disconnecting:
			client.Disconnect();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case HandshakeType.None:
			break;
		}
		NetworkBase.UpdateLoadingScreenContext(handshake);
		NetworkChannel channel = ((handshakeState == HandshakeType.ClientReady || handshakeState == HandshakeType.Disconnecting) ? NetworkChannel.GeneralTraffic : NetworkChannel.Unreliable);
		SendToClients(handshake, channel, -1L);
	}

	public static void StopServer()
	{
		IsHosting = false;
		NetworkManager.EndConnection();
		NetworkClient.ResetStatistics();
		NetworkBase.ClearClientsList();
		FragmentHandler.Reset();
		NetworkManager.StopHost();
		_masterServerPingTimer.Stop();
	}

	public static bool HasClients()
	{
		return NetworkBase.Clients.Count > 0;
	}

	public static void ApplyLocalHostSetting(bool currentDataStartLocalHost)
	{
		if (currentDataStartLocalHost)
		{
			Host().Forget();
		}
	}

	public static void Cancel()
	{
		World.Cancel();
		ImGuiLoadingScreen.SetActive(active: false);
		GameManager.LeaveGame();
	}

	public static void Ban(Client client)
	{
		if (!NetworkManager.IsServer)
		{
			ConsoleWindow.PrintError("Only Server can ban players", suppressStacktrace: true);
			return;
		}
		client.Disconnect();
		AddToBlacklist(client.ClientId);
	}

	public static void AddToBlacklist(ulong clientId)
	{
		if (!NetworkManager.IsServer)
		{
			ConsoleWindow.PrintError("Only Server can ban players", suppressStacktrace: true);
			return;
		}
		Blacklist.Add(new BlacklistedClient
		{
			Id = clientId
		});
		File.WriteAllText(BlacklistedClient.PATH, string.Join(",", Blacklist.Select((BlacklistedClient x) => x.Id)));
	}

	private static void PingMasterServer()
	{
		if (Settings.CurrentData.ServerVisible)
		{
			if (NetworkManager.CurrentGameSession == null)
			{
				CreateNewGameSession();
				return;
			}
			NetworkManager.UpdateSessionData();
			NetworkManager.CurrentTransport.Ping(NetworkManager.CurrentGameSession).Forget();
		}
	}

	private static void CreateNewGameSession()
	{
		if (Settings.CurrentData.ServerVisible)
		{
			NetworkManager.StartSession(new GameSessionConfig
			{
				gameName = Settings.CurrentData.ServerName,
				password = !string.IsNullOrEmpty(Settings.CurrentData.ServerPassword),
				maxPlayers = Settings.CurrentData.ServerMaxPlayers,
				port = ushort.Parse(Settings.CurrentData.GamePort),
				mapName = WorldManager.CurrentWorldName,
				ipAddress = NetworkManager.CurrentTransport.PublicIp,
				SteamId = (Settings.CurrentData.UseSteamP2P ? GameManager.GetSteamId() : 0)
			});
		}
	}

	private static async void MasterServerPingTimerOnElapsed(object sender, ElapsedEventArgs e)
	{
		await UniTask.SwitchToMainThread();
		PingMasterServer();
	}

	public static void ClientIsAwaitingCharacter(ulong clientId)
	{
		Client client = null;
		using (Queue<Client>.Enumerator enumerator = JoiningClients.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				Client current = enumerator.Current;
				if (clientId == current.ClientId)
				{
					client = current;
				}
			}
		}
		client?.SetState(ClientState.WaitingForCharacter);
	}

	public static void OnNextDay()
	{
		foreach (Client client in NetworkBase.Clients)
		{
			if (client.state == ClientState.Ready)
			{
				ushort daysLived = Entity.GetClientEntity(client.ClientId)?.DaysLived ?? 0;
				client.DaysLived = daysLived;
				client.flags |= ClientUpdateFlag.DaysLived;
			}
		}
	}
}
