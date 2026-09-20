using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking.Transports;
using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Brutal.RakNetApi;
using Cysharp.Threading.Tasks;
using Networking.GameSessions;
using Networking.Lobbies;
using Networking.Servers;
using Open.Nat;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Networking;

public sealed class NetworkManager : ManagerBase
{
	public struct NetClientSample
	{
		public long Id;

		public string Name;

		public int AvgPing;

		public int LastPing;

		public int LowPing;

		public float Loss;

		public ulong SendQueueBytes;

		public uint SendQueueMsgs;

		public ulong BytesSentPerSec;

		public ulong BytesRecvPerSec;

		public int Mtu;

		public Brutal.RakNetApi.ConnectionState State;
	}

	public readonly struct Avatar(Texture2D smallTexture, Texture2D mediumTexture, Texture2D largeTexture)
	{
		internal readonly Sprite SmallSprite = CreateSprite(smallTexture);

		internal readonly Sprite MediumSprite = CreateSprite(mediumTexture);

		internal readonly Sprite LargeSprite = CreateSprite(largeTexture);

		private static Sprite CreateSprite(Texture2D texture)
		{
			if (!texture)
			{
				return null;
			}
			return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
		}

		public bool IsValid()
		{
			if (!SmallSprite && !MediumSprite)
			{
				return LargeSprite;
			}
			return true;
		}
	}

	private RakPeerInstance rakNet;

	private static readonly int PLAYER_LIST_UPDATE_TIME = 2;

	public const int FRAGMENT_SIZE = 1000;

	private static NetworkManager Instance;

	private static long _hostId = -1L;

	public static NetworkState NetworkState = NetworkState.Offline;

	public static NetworkRole NetworkRole = NetworkRole.None;

	[Header("Connection Statistics")]
	[ReadOnly]
	public int OutgoingFullBytesCount;

	private float _avg;

	private float _lastTraffic;

	private static int BaseRocketNetPacketSize = 60;

	public static SteamId _hostSteamId = 0uL;

	public static bool bHasSteamP2PConnections = false;

	public static float TimeSincePacketReceived = 0f;

	public static SteamLobby steamLobby;

	private static readonly string[] StatsHeaders = new string[8] { "CONNECTION", "PING avg/last/low", "MTU", "LOSS 1s", "SENDQ", "RESEND", "CC LIMIT", "STATE" };

	private const int MAX_SEND_ATTEMPTS = 10;

	private static readonly byte[] Buffer = new byte[1400];

	private static RocketBinaryWriter _messageWriter = new RocketBinaryWriter(Buffer.Length);

	public const int PING_TIME_MS = 30000;

	private static readonly Dictionary<ulong, Avatar> AvatarCache = new Dictionary<ulong, Avatar>();

	private static long _outgoingFullBytesCount;

	private static long _incomingFullBytesCount;

	private static readonly System.Random rand = new System.Random();

	public static bool IsActive => NetworkRole != NetworkRole.None;

	public static bool IsClient => NetworkRole == NetworkRole.Client;

	public static bool IsServer => NetworkRole == NetworkRole.Server;

	public static int MaxConnections => 64;

	public static bool IsActiveAsClient
	{
		get
		{
			if (IsActive)
			{
				return IsClient;
			}
			return false;
		}
	}

	public static Client HostClient { get; set; }

	public static int TotalPlayersInGame => NetworkBase.Clients.Count + ((!GameManager.IsBatchMode) ? 1 : 0);

	public static MetaServerTransport CurrentTransport { get; private set; }

	public static GameSession CurrentGameSession { get; private set; }

	public static List<GameSession> GameSessionList { get; private set; } = new List<GameSession>();

	public static List<GameSession> GameSessionFavouriteList { get; } = new List<GameSession>();

	public static PlayerCookie Cookie { get; private set; }

	public static NetConfig Config { get; private set; }

	public static ulong LocalClientId => Cookie?.ClientId ?? 0;

	public static string Username => Cookie?.Username ?? string.Empty;

	private void EnsureRakNet()
	{
		if (rakNet.IsNull())
		{
			rakNet = RakNetLibrary.CreateInstance();
		}
	}

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		Instance = this;
		Init();
		GameManager.OnGameStateChange += GameManagerOnOnGameStateChange;
	}

	public static void EndConnection()
	{
		ConsoleWindow.Print("Ending network connection");
		if (NetworkState != NetworkState.Offline)
		{
			ShutDownRaknet();
			ConsoleWindow.Print("Resetting network state");
			if (_hostSteamId.IsValid)
			{
				CloseP2PConnectionClient();
				_hostSteamId = 0uL;
				bHasSteamP2PConnections = false;
			}
			_hostId = -1L;
			NetworkState = NetworkState.Offline;
			NetworkRole = NetworkRole.None;
		}
	}

	public static void ClearAll()
	{
		bHasSteamP2PConnections = false;
	}

	private static void CloseP2PConnectionClient()
	{
		if (NetworkRole == NetworkRole.Client && NetworkClient.ConnectionMethod == ConnectionMethod.FacepunchSteamP2P)
		{
			ConsoleWindow.Print($"Closing Steam P2P Connection with Host: {_hostSteamId.IsValid}");
			if (_hostSteamId.IsValid)
			{
				steamLobby.LeaveLobby();
				NetworkClient.Disconnect().Forget();
				SteamNetworking.CloseP2PSessionWithUser(_hostSteamId);
				_hostSteamId = 0uL;
			}
		}
	}

	public static void CloseP2PConnectionServer(Client clientToDisconnect)
	{
		if (NetworkRole == NetworkRole.Server && _hostSteamId.IsValid && clientToDisconnect.connectionMethod == ConnectionMethod.FacepunchSteamP2P)
		{
			SteamNetworking.CloseP2PSessionWithUser(clientToDisconnect.ClientId);
		}
	}

	public static void CloseConnectionServer(long connectionId, ConnectionMethod connectionMethod)
	{
		switch (connectionMethod)
		{
		case ConnectionMethod.RocketNet:
			if (!(Instance == null) && !Instance.rakNet.IsNull())
			{
				RakNetGUID guid = new RakNetGUID
				{
					G = (ulong)connectionId
				};
				RakPeerInterface.CloseConnection(target: new AddressOrGUID(guid), instance: Instance.rakNet, sendDisconnectionNotification: true, orderingChannel: 0, disconnectionNotificationPriority: PacketPriority.HighPriority);
			}
			break;
		case ConnectionMethod.FacepunchSteamP2P:
			if (NetworkRole == NetworkRole.Server && _hostSteamId.IsValid)
			{
				SteamNetworking.CloseP2PSessionWithUser((ulong)connectionId);
			}
			break;
		}
	}

	public unsafe static void LogConnectionStatsToConsole()
	{
		if (Instance == null || Instance.rakNet.IsNull())
		{
			ConsoleWindow.PrintAction("RocketNet stats: no active RakNet peer", aged: true);
			return;
		}
		RakPeerInstance instance = Instance.rakNet;
		List<(string, uint)[]> list = new List<(string, uint)[]>();
		RakNetStatistics rakNetStatistics = default(RakNetStatistics);
		for (uint num = 0u; num < MaxConnections; num++)
		{
			RakNetGUID gUIDFromIndex = instance.GetGUIDFromIndex(num);
			if (gUIDFromIndex.G == 0L || gUIDFromIndex.G == ulong.MaxValue)
			{
				continue;
			}
			AddressOrGUID systemIdentifier = new AddressOrGUID(gUIDFromIndex);
			SystemAddress systemAddressFromIndex = instance.GetSystemAddressFromIndex(num);
			if (instance.GetStatistics(systemAddressFromIndex, &rakNetStatistics) != null)
			{
				int averagePing = instance.GetAveragePing(systemIdentifier);
				int lastPing = instance.GetLastPing(systemIdentifier);
				int lowestPing = instance.GetLowestPing(systemIdentifier);
				int mTUSize = instance.GetMTUSize(systemAddressFromIndex);
				Brutal.RakNetApi.ConnectionState connectionState = instance.GetConnectionState(systemIdentifier);
				Client client = Client.Find((long)gUIDFromIndex.G);
				string item = ((client != null) ? client.name : "(unknown)");
				uint num2 = 0u;
				double num3 = 0.0;
				for (int i = 0; i < 4; i++)
				{
					num2 += rakNetStatistics.MessageInSendBuffer[i];
					num3 += rakNetStatistics.BytesInSendBuffer[i];
				}
				ulong num4 = (ulong)num3;
				bool num5 = *(byte*)(&rakNetStatistics.IsLimitedByCongestionControl) != 0;
				int num6 = ((!(rakNetStatistics.PacketlossLastSecond <= 0f)) ? ((rakNetStatistics.PacketlossLastSecond < 0.02f) ? 1 : 2) : 0);
				int num7 = ((num2 > 32 || num4 >= 262144) ? ((num2 <= 512 && num4 < 2097152) ? 1 : 2) : 0);
				int num8 = ((rakNetStatistics.MessagesInResendBuffer > 8) ? ((rakNetStatistics.MessagesInResendBuffer <= 64) ? 1 : 2) : 0);
				int num9 = (num5 ? 2 : 0);
				int num10 = ((mTUSize < 1400) ? ((mTUSize >= 1200) ? 1 : 2) : 0);
				int num11 = ((connectionState != Brutal.RakNetApi.ConnectionState.IsConnected) ? 1 : 0);
				int tier = Math.Max(Math.Max(Math.Max(num6, num7), Math.Max(num8, num9)), Math.Max(num10, num11));
				string item2 = ((!num5) ? "-" : ((rakNetStatistics.BPSLimitByCongestionControl != 0) ? FormatBps(rakNetStatistics.BPSLimitByCongestionControl) : "throttled"));
				list.Add(new(string, uint)[8]
				{
					(item, TierColor(tier)),
					($"{averagePing}/{lastPing}/{lowestPing}ms", ImGuiColor.Integer.SoftSky),
					($"{mTUSize}", TierColor(num10)),
					($"{rakNetStatistics.PacketlossLastSecond * 100f:0.0}%", TierColor(num6)),
					($"{num2} / {FormatBytes(num4)}", TierColor(num7)),
					($"{rakNetStatistics.MessagesInResendBuffer} / {FormatBytes(rakNetStatistics.BytesInResendBuffer)}", TierColor(num8)),
					(item2, TierColor(num9)),
					(connectionState.ToString(), TierColor(num11))
				});
			}
		}
		if (list.Count == 0)
		{
			ConsoleWindow.PrintAction("RocketNet stats: no RocketNet connections (Steam P2P connections are not shown)", aged: true);
			return;
		}
		ConsoleWindow.PrintAction($"RocketNet link stats (tick {NetworkServer.CurrentTickIntervalMs}ms / {1000 / NetworkServer.CurrentTickIntervalMs}Hz) — colours: green healthy / amber watch / red problem. SENDQ = server→client backlog (the desync signal); RESEND = in-flight unacked; CC LIMIT shows when congestion-throttled.");
		ConsoleWindow.PrintTable(StatsHeaders, list);
	}

	public static void LogAwakeBodiesToConsole()
	{
		if (!IsServer)
		{
			ConsoleWindow.PrintAction("network awake must be run on the server", aged: true);
			return;
		}
		Dictionary<string, (int, int)> dictionary = new Dictionary<string, (int, int)>();
		int num = 0;
		int num2 = 0;
		DensePool<Thing>.ActiveEnumerable.Enumerator enumerator = OcclusionManager.AllThings.Active().GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (enumerator.Current is DynamicThing dynamicThing && (bool)dynamicThing && dynamicThing.ReferenceId != 0L && !dynamicThing.IsBeingDestroyed && dynamicThing.TryPeekPhysicsRecord(out var record))
			{
				bool flag = record.Velocity.sqrMagnitude < 0.0001f && record.AngularVelocity.sqrMagnitude < 0.0001f;
				dictionary.TryGetValue(dynamicThing.PrefabName, out var value);
				dictionary[dynamicThing.PrefabName] = (value.Item1 + 1, value.Item2 + (flag ? 1 : 0));
				num++;
				if (flag)
				{
					num2++;
				}
			}
		}
		if (num == 0)
		{
			ConsoleWindow.PrintAction("network awake: no awake physics bodies in the batch set", aged: true);
			return;
		}
		List<KeyValuePair<string, (int, int)>> list = new List<KeyValuePair<string, (int, int)>>(dictionary);
		list.Sort((KeyValuePair<string, (int awake, int idle)> a, KeyValuePair<string, (int awake, int idle)> b) => b.Value.awake.CompareTo(a.Value.awake));
		List<(string, uint)[]> list2 = new List<(string, uint)[]>(Math.Min(list.Count, 40) + 1);
		foreach (KeyValuePair<string, (int, int)> item in list)
		{
			if (list2.Count >= 40)
			{
				break;
			}
			float num3 = ((item.Value.Item1 > 0) ? ((float)item.Value.Item2 * 100f / (float)item.Value.Item1) : 0f);
			int tier = ((num3 >= 75f) ? 2 : ((num3 >= 25f) ? 1 : 0));
			list2.Add(new(string, uint)[3]
			{
				(item.Key, ImGuiColor.Integer.Grey),
				($"{item.Value.Item1}", ImGuiColor.Integer.SoftSky),
				($"{item.Value.Item2}", TierColor(tier))
			});
		}
		list2.Add(new(string, uint)[3]
		{
			("TOTAL", ImGuiColor.Integer.Grey),
			($"{num}", ImGuiColor.Integer.SoftSky),
			($"{num2}", TierColor((num2 * 100 / Math.Max(1, num) >= 50) ? 2 : 0))
		});
		ConsoleWindow.PrintAction($"Physics batch set: {num} awake bodies, {num2} idle. Unchanged records are suppressed to a heartbeat every {10u} ticks, so idle bodies cost ~1/{10u} of their raw ~80B/tick; last tick wrote {FragmentHandler.LastPhysicsRecordCount} records, suppressed {FragmentHandler.LastPhysicsSuppressedCount}.");
		ConsoleWindow.PrintTable(new string[3] { "PREFAB", "AWAKE", "IDLE" }, list2);
	}

	public static void LogSectionsToConsole()
	{
		if (!IsServer)
		{
			ConsoleWindow.PrintAction("network sections must be run on the server", aged: true);
			return;
		}
		IReadOnlyList<(string, int)> lastSectionBytes = FragmentHandler.LastSectionBytes;
		if (lastSectionBytes.Count == 0)
		{
			ConsoleWindow.PrintAction("network sections: no state tick has been sent yet", aged: true);
			return;
		}
		int num = 0;
		foreach (var item4 in lastSectionBytes)
		{
			int item = item4.Item2;
			num += item;
		}
		List<(string, uint)[]> list = new List<(string, uint)[]>(lastSectionBytes.Count + 4);
		foreach (var item5 in lastSectionBytes)
		{
			string item2 = item5.Item1;
			int item3 = item5.Item2;
			float num2 = ((num > 0) ? ((float)item3 * 100f / (float)num) : 0f);
			int tier = ((num2 >= 25f) ? 2 : ((num2 >= 10f) ? 1 : 0));
			list.Add(new(string, uint)[3]
			{
				(item2, ImGuiColor.Integer.Grey),
				(FormatBytes((ulong)item3), TierColor(tier)),
				($"{num2:0.0}%", TierColor(tier))
			});
		}
		list.Add(new(string, uint)[3]
		{
			("STATE raw", ImGuiColor.Integer.Grey),
			(FormatBytes((ulong)num), ImGuiColor.Integer.SoftSky),
			("", ImGuiColor.Integer.Grey)
		});
		list.Add(new(string, uint)[3]
		{
			("STATE compressed", ImGuiColor.Integer.Grey),
			(FormatBytes((ulong)FragmentHandler.LastTickCompressedBytes), ImGuiColor.Integer.SoftSky),
			("", ImGuiColor.Integer.Grey)
		});
		list.Add(new(string, uint)[3]
		{
			("PHYSICS records", ImGuiColor.Integer.Grey),
			($"{FragmentHandler.LastPhysicsRecordCount}", ImGuiColor.Integer.SoftSky),
			("", ImGuiColor.Integer.Grey)
		});
		list.Add(new(string, uint)[3]
		{
			("PHYSICS compressed", ImGuiColor.Integer.Grey),
			(FormatBytes((ulong)FragmentHandler.LastPhysicsCompressedBytes), ImGuiColor.Integer.SoftSky),
			("", ImGuiColor.Integer.Grey)
		});
		ConsoleWindow.PrintAction($"State-tick payload by section (last sent tick {FragmentHandler.NetworkTick}); share is of the raw uncompressed state blob. Physics rides its own sequenced stream.");
		ConsoleWindow.PrintTable(new string[3] { "SECTION", "BYTES", "SHARE" }, list);
	}

	public static void LogStatusToConsole()
	{
		IPGlobalProperties iPGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
		ConsoleWindow.PrintAction("network status for '" + iPGlobalProperties.HostName + "." + iPGlobalProperties.DomainName + "'");
		List<(string, uint)[]> list = new List<(string, uint)[]>();
		list.Add(new(string, uint)[2]
		{
			("role", ImGuiColor.Integer.Grey),
			(NetworkRole.ToString(), ImGuiColor.Integer.SoftSage)
		});
		List<(string, uint)[]> list2 = list;
		switch (NetworkRole)
		{
		case NetworkRole.Server:
			list2.Add(new(string, uint)[2]
			{
				("host port", ImGuiColor.Integer.Grey),
				($"{NetworkServer.HostPort}", ImGuiColor.Integer.SoftSky)
			});
			list2.Add(new(string, uint)[2]
			{
				("hosting time", ImGuiColor.Integer.Grey),
				($"{NetworkBase.ConnectedTime}s", ImGuiColor.Integer.SoftSky)
			});
			break;
		case NetworkRole.Client:
			list2.Add(new(string, uint)[2]
			{
				("host address", ImGuiColor.Integer.Grey),
				(NetworkClient.Address + ":" + NetworkClient.Port, ImGuiColor.Integer.SoftSky)
			});
			list2.Add(new(string, uint)[2]
			{
				("joined time", ImGuiColor.Integer.Grey),
				($"{NetworkBase.ConnectedTime}s", ImGuiColor.Integer.SoftSky)
			});
			list2.Add(new(string, uint)[2]
			{
				("round trip", ImGuiColor.Integer.Grey),
				($"{NetworkClient.RoundTripTime}ms", ImGuiColor.Integer.SoftSky)
			});
			break;
		}
		list2.Add(new(string, uint)[2]
		{
			("fragment size", ImGuiColor.Integer.Grey),
			($"{1000}B", ImGuiColor.Integer.SoftSky)
		});
		bool flag = NetworkServer.CurrentTickIntervalMs > 50;
		string item = $"{NetworkServer.CurrentTickIntervalMs}ms ({1000 / NetworkServer.CurrentTickIntervalMs}Hz, base {50}ms)";
		list2.Add(new(string, uint)[2]
		{
			("tick", ImGuiColor.Integer.Grey),
			(item, flag ? ImGuiColor.Integer.SoftMustard : ImGuiColor.Integer.SoftSage)
		});
		ConsoleWindow.PrintTable(null, list2);
	}

	public static void LogClientRosterToConsole()
	{
		List<Client> clients = NetworkBase.Clients;
		ConsoleWindow.PrintAction($"Clients: {clients.Count}");
		List<(string, uint)[]> list = new List<(string, uint)[]>(clients.Count + 1);
		foreach (Client item in clients)
		{
			list.Add(ClientRosterRow(item));
		}
		if (HostClient != null)
		{
			list.Add(ClientRosterRow(HostClient));
		}
		ConsoleWindow.PrintTable(new string[4] { "NAME", "STATE", "CONNECTED", "CLIENTID" }, list);
	}

	private static (string text, uint color)[] ClientRosterRow(Client client)
	{
		uint item = ((client.state == ClientState.Ready) ? ImGuiColor.Integer.SoftSage : ((client.state == ClientState.Disconnected) ? ImGuiColor.Integer.Red : ImGuiColor.Integer.SoftMustard));
		return new(string, uint)[4]
		{
			(client.IsHost ? (client.name + " (host)") : client.name, item),
			(client.state.ToString(), item),
			($"{client.connectTime:0.0}s", ImGuiColor.Integer.SoftSky),
			($"{client.ClientId}", ImGuiColor.Integer.Grey)
		};
	}

	public static void LogAdaptersToConsole()
	{
		ConsoleWindow.PrintAction("network adapters");
		List<(string, uint)[]> list = new List<(string, uint)[]>();
		NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
		foreach (NetworkInterface networkInterface in allNetworkInterfaces)
		{
			if (!networkInterface.IsReceiveOnly)
			{
				try
				{
					bool flag = networkInterface.OperationalStatus == OperationalStatus.Up;
					bool flag2 = networkInterface.Supports(NetworkInterfaceComponent.IPv4);
					bool flag3 = networkInterface.Supports(NetworkInterfaceComponent.IPv6);
					int num = networkInterface.GetIPProperties().GetIPv4Properties()?.Mtu ?? (-1);
					string text = networkInterface.GetPhysicalAddress().ToString();
					list.Add(new(string, uint)[7]
					{
						(networkInterface.Name, ImGuiColor.Integer.White),
						(networkInterface.OperationalStatus.ToString(), flag ? ImGuiColor.Integer.SoftSage : ImGuiColor.Integer.Grey),
						(networkInterface.NetworkInterfaceType.ToString(), ImGuiColor.Integer.White),
						(flag2 ? "yes" : "no", flag2 ? ImGuiColor.Integer.SoftSage : ImGuiColor.Integer.Grey),
						(flag3 ? "yes" : "no", flag3 ? ImGuiColor.Integer.SoftSage : ImGuiColor.Integer.Grey),
						(string.IsNullOrEmpty(text) ? "-" : text, ImGuiColor.Integer.SoftSky),
						((num > 0) ? $"{num}" : "-", ImGuiColor.Integer.SoftSky)
					});
				}
				catch (Exception ex)
				{
					ConsoleWindow.PrintError("adapter read failed: " + ex.Message);
				}
			}
		}
		ConsoleWindow.PrintTable(new string[7] { "NAME", "STATUS", "TYPE", "IPV4", "IPV6", "MAC", "MTU" }, list);
	}

	private static uint TierColor(int tier)
	{
		return tier switch
		{
			0 => ImGuiColor.Integer.SoftSage, 
			1 => ImGuiColor.Integer.SoftMustard, 
			_ => ImGuiColor.Integer.Red, 
		};
	}

	private static string FormatBytes(ulong bytes)
	{
		if (bytes < 1024)
		{
			return $"{bytes}B";
		}
		if (bytes < 1048576)
		{
			return $"{(double)bytes / 1024.0:0.0}KB";
		}
		return $"{(double)bytes / 1048576.0:0.0}MB";
	}

	private static string FormatBps(ulong bytesPerSec)
	{
		if (bytesPerSec == 0L)
		{
			return "-";
		}
		if (bytesPerSec < 1024)
		{
			return $"{bytesPerSec}B/s";
		}
		if (bytesPerSec < 1048576)
		{
			return $"{(double)bytesPerSec / 1024.0:0.0}KB/s";
		}
		return $"{(double)bytesPerSec / 1048576.0:0.0}MB/s";
	}

	private void OnDestroy()
	{
		GameManager.OnGameStateChange -= GameManagerOnOnGameStateChange;
		EndConnection();
	}

	private static void GameManagerOnOnGameStateChange()
	{
		if (GameManager.GameState == GameState.Joining)
		{
			KeyMap._Cancel.KeyUp += Cancel;
		}
		else
		{
			KeyMap._Cancel.KeyUp -= Cancel;
		}
	}

	private static void Cancel()
	{
		if (GameManager.GameState != GameState.Joining)
		{
			ConsoleWindow.PrintError("User cancelled", suppressStacktrace: true);
			return;
		}
		switch (NetworkRole)
		{
		case NetworkRole.Server:
			NetworkServer.Cancel();
			break;
		case NetworkRole.Client:
			NetworkClient.Cancel();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case NetworkRole.None:
			break;
		}
	}

	private void Init()
	{
		FragmentHandler.Initialize();
		steamLobby = new SteamLobby();
		SteamNetworking.OnP2PSessionRequest = ProcessP2PSessionRequest;
	}

	private void ProcessP2PSessionRequest(SteamId steamId)
	{
		if (Settings.CurrentData.UseSteamP2P && steamId.IsValid && (NetworkRole == NetworkRole.None || NetworkRole == NetworkRole.Server))
		{
			long value = (long)steamId.Value;
			if (Client.Find(value) != null)
			{
				ConsoleWindow.PrintError("P2P Connection request received from Client already in the list of Clients- Removing them from the list so they can rejoin");
				NetworkServer.ClientDisconnected(value);
			}
			ConsoleWindow.Print("Steam P2P request accepted from Steam ID " + steamId.ToString());
			bHasSteamP2PConnections = true;
			NetworkRole = NetworkRole.Server;
			NetworkState = NetworkState.Online;
			SendDataToSteamP2PConnection(steamId, null, 139);
			SteamNetworking.AcceptP2PSessionWithUser(steamId);
			PlayerConnected((long)steamId.Value, ConnectionMethod.FacepunchSteamP2P);
		}
		else
		{
			ConsoleWindow.PrintError("Steam request received but rejected - Steam P2P requests are disabled in settings or this user is hosting");
		}
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		if (NetworkState != NetworkState.Offline)
		{
			Instance.EnsureRakNet();
			while (ReceiveEvents())
			{
			}
			DeferredMessageQueue.TickExpire();
			CheckConnection();
		}
	}

	protected void CheckConnection()
	{
		if (NetworkClient.ConnectionMethod == ConnectionMethod.FacepunchSteamP2P && NetworkRole == NetworkRole.Client && NetworkState == NetworkState.Online)
		{
			float timeSincePacketReceived = TimeSincePacketReceived;
			TimeSincePacketReceived += Time.deltaTime;
			int num = (int)timeSincePacketReceived;
			int num2 = (int)TimeSincePacketReceived;
			if (num2 > 15)
			{
				ConsoleWindow.PrintError("No message received from host for 15 seconds - disconnecting");
				PlayerDisconnected(NetworkClient.ConnectionId);
			}
			else if (num != num2 && num2 >= 5)
			{
				ConsoleWindow.Print($"No message received from host for {num2} seconds - Sending Heartbeat");
				SendDataToSteamP2PConnection(_hostSteamId, null, 140);
			}
		}
	}

	public static void ShutDownRaknet()
	{
		if (Instance != null && !Instance.rakNet.IsNull())
		{
			Instance.rakNet.Shutdown(0u, 0, PacketPriority.ImmediatePriority);
			Instance.rakNet.Dispose();
			Instance.rakNet = default(RakPeerInstance);
		}
	}

	public static byte[] Compress(Span<byte> data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using GZipStream gZipStream = new GZipStream(memoryStream, System.IO.Compression.CompressionLevel.Optimal);
		int length = data.Length;
		byte[] array = ArrayPool<byte>.Shared.Rent(length);
		try
		{
			data.CopyTo(array.AsSpan(0, length));
			gZipStream.Write(array, 0, data.Length);
			gZipStream.Close();
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array);
		}
		return memoryStream.ToArray();
	}

	public static byte[] Decompress(byte[] data)
	{
		using MemoryStream stream = new MemoryStream(data);
		using GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress);
		using MemoryStream memoryStream = new MemoryStream();
		gZipStream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}

	public static string GetIPv4Address()
	{
		uint numberOfAddresses = Instance.rakNet.GetNumberOfAddresses();
		if (numberOfAddresses != 0)
		{
			ConsoleWindow.Print("Found Ipv4 addresses");
			int num = -1;
			for (int num2 = (int)(numberOfAddresses - 1); num2 >= 0; num2--)
			{
				string localIP = Instance.rakNet.GetLocalIP((uint)num2);
				ConsoleWindow.Print(localIP);
				if (IPAddress.TryParse(localIP, out var address))
				{
					byte[] addressBytes = address.GetAddressBytes();
					if (address.AddressFamily == AddressFamily.InterNetwork && addressBytes[0] != 127)
					{
						num = num2;
					}
				}
			}
			if (num == -1)
			{
				throw new Exception("No network adapters with an IPv4 address in the system!");
			}
			return Instance.rakNet.GetLocalIP((uint)num);
		}
		throw new Exception("No network adapters with an IPv4 address in the system!");
	}

	public static bool StartServer(ushort port)
	{
		if (!CanBecome(NetworkRole.Server))
		{
			ConsoleWindow.Print($"Cannot become server. Current Network Role: {NetworkRole}");
			return false;
		}
		Instance.EnsureRakNet();
		_hostSteamId = GameManager.GetSteamId();
		string text = Settings.CurrentData.LocalIpAddress.Trim();
		string text2;
		if (!string.IsNullOrEmpty(text))
		{
			text2 = text;
			ConsoleWindow.PrintAction($"Host IP Address manually provided: {text}:{port}");
		}
		else
		{
			text2 = GetIPv4Address();
			ConsoleWindow.PrintAction($"Attempting to host at {text2}:{port}");
		}
		SocketDescriptor socketDescriptor = new SocketDescriptor(text2, port);
		Span<SocketDescriptor> socketDescriptors = stackalloc SocketDescriptor[1] { socketDescriptor };
		StartupResult startupResult = Instance.rakNet.Startup((uint)MaxConnections, socketDescriptors, 2);
		if (startupResult != StartupResult.RaknetStarted)
		{
			ConsoleWindow.PrintAction("Hosting failed. Attempting fallback behaviour");
			SocketDescriptor socketDescriptor2 = new SocketDescriptor("", port);
			Span<SocketDescriptor> socketDescriptors2 = stackalloc SocketDescriptor[1] { socketDescriptor2 };
			startupResult = Instance.rakNet.Startup((uint)MaxConnections, socketDescriptors2, 2);
		}
		if (startupResult != StartupResult.RaknetStarted)
		{
			ConsoleWindow.PrintError($"{startupResult}. RakNet failed to start Server with Address: {text2}:{port}");
			return false;
		}
		Instance.rakNet.SetMaximumIncomingConnections((ushort)MaxConnections);
		NetworkState = NetworkState.Online;
		Time.timeScale = 1f;
		NetworkRole = NetworkRole.Server;
		NetworkServer.HostPort = port;
		ConsoleWindow.PrintAction($"RakNet successfully hosted with Address: {text2}:{port}");
		steamLobby.HostLobby(_hostSteamId, Settings.CurrentData.ServerMaxPlayers);
		return true;
	}

	public static async void RegisterUsingUPnP(string hostAddress, ushort port)
	{
		ConsoleWindow.Print($"attempting upnp mapping for '{port}'");
		NatDiscoverer natDiscoverer = new NatDiscoverer();
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(5000);
		IEnumerable<NatDevice> enumerable = await natDiscoverer.DiscoverDevicesAsync(PortMapper.Upnp, cancellationTokenSource);
		if (!enumerable.Any())
		{
			ConsoleWindow.Print($"Failed to register '{port}' using upnp. No UPNP enabled devices were discovered", ConsoleColor.Red);
			return;
		}
		int skippedDevices = 0;
		bool flag = false;
		foreach (NatDevice device in enumerable)
		{
			if (device.LocalAddress.ToString() != hostAddress)
			{
				skippedDevices++;
				continue;
			}
			Mapping mapping = new Mapping(Protocol.Udp, port, port, $"Stationeers {port}");
			ErrorCode error = ErrorCode.None;
			try
			{
				await device.CreatePortMapAsync(mapping);
			}
			catch (Exception ex)
			{
				if (NetworkRole == NetworkRole.None)
				{
					return;
				}
				error = (ErrorCode)((MappingException)ex).ErrorCode;
			}
			if (error == ErrorCode.ConflictInMappingEntry)
			{
				ConsoleWindow.Print($"removing conflicting upnp mapping '{mapping.Description}' on '{mapping.PrivateIP}'");
				try
				{
					await device.DeletePortMapAsync(mapping);
					await device.CreatePortMapAsync(mapping);
					error = ErrorCode.None;
				}
				catch (Exception ex2)
				{
					if (NetworkRole == NetworkRole.None)
					{
						return;
					}
					error = (ErrorCode)((MappingException)ex2).ErrorCode;
				}
			}
			if (error != ErrorCode.None)
			{
				ConsoleWindow.PrintError("error '" + error.ToString().ToLower() + "' attempting upnp mapping on " + hostAddress);
				flag = true;
				continue;
			}
			IPAddress iPAddress = await device.GetExternalIPAsync();
			if (NetworkRole != NetworkRole.None)
			{
				string text = iPAddress.ToString();
				ConsoleWindow.Print($"successful registered '{mapping.PrivateIP}:{port}' on '{text}' using upnp");
				SteamTransport.SetSteamRichPresenceStatus();
				SteamTransport.SetSteamRichPresenceConnect(text, port.ToString());
				UpdateSessionData(text, port.ToString());
			}
			return;
		}
		if (!flag)
		{
			if (skippedDevices > 0)
			{
				ConsoleWindow.Print($"...skipped '{skippedDevices}' devices");
			}
			ConsoleWindow.PrintError($"error failed to register '{port}' using upnp", suppressStacktrace: true);
		}
	}

	public static string ResolveIpAddress(string address)
	{
		IPAddress[] hostAddresses = Dns.GetHostAddresses(address);
		if (hostAddresses.Length == 0)
		{
			return string.Empty;
		}
		return hostAddresses[0].ToString();
	}

	public unsafe static bool StartClient(string address, ushort port, ushort localPort)
	{
		if (!CanBecome(NetworkRole.Client))
		{
			return false;
		}
		Instance.EnsureRakNet();
		FragmentHandler.Reset();
		SocketDescriptor socketDescriptor = new SocketDescriptor(AddressFamily.InterNetwork);
		Span<SocketDescriptor> socketDescriptors = stackalloc SocketDescriptor[1] { socketDescriptor };
		StartupResult startupResult = Instance.rakNet.Startup(1u, socketDescriptors, 2);
		if (startupResult != StartupResult.RaknetStarted)
		{
			ConsoleWindow.PrintAction($"RakNet failed to start client: {startupResult}");
			NetworkClient.OnJoinFailed();
			return false;
		}
		byte[] bytes = Encoding.UTF8.GetBytes(address);
		byte[] array = new byte[bytes.Length + 1];
		Array.Copy(bytes, array, bytes.Length);
		array[^1] = 0;
		ConnectionAttemptResult connectionAttemptResult = Instance.rakNet.Connect(array, port, ReadOnlySpan<byte>.Empty, null);
		if (connectionAttemptResult != ConnectionAttemptResult.ConnectionAttemptStarted)
		{
			ConsoleWindow.PrintAction($"RakNet failed to connect to {address}:{port} ({connectionAttemptResult})");
			NetworkClient.OnJoinFailed();
			return false;
		}
		NetworkState = NetworkState.WaitingForConnection;
		NetworkClient.ResetStatistics();
		NetworkClient.Address = address;
		NetworkClient.Port = port.ToString();
		NetworkRole = NetworkRole.Client;
		Debug.Log($"Connected to {address} on port {port}");
		return true;
	}

	public static bool StartClient(SteamId hostSteamId)
	{
		if (NetworkState != NetworkState.Offline || !CanBecome(NetworkRole.Client))
		{
			return false;
		}
		ConsoleWindow.Print("Can become client");
		NetworkClient.ClientPreJoin();
		NetworkState = NetworkState.WaitingForConnection;
		bool num = SendDataToSteamP2PConnection(hostSteamId, null, 138);
		if (num)
		{
			NetworkClient.OnJoinStart();
			ConsoleWindow.Print("Attempting P2P connection with host Steam ID: " + hostSteamId.ToString());
			_hostId = (long)hostSteamId.Value;
			_hostSteamId = hostSteamId;
			NetworkRole = NetworkRole.Client;
			bHasSteamP2PConnections = true;
			NetworkClient.ResetStatistics();
			return num;
		}
		ConsoleWindow.Print("P2P request failed");
		NetworkState = NetworkState.Offline;
		NetworkClient.OnJoinFailed();
		return num;
	}

	private static bool CanBecome(NetworkRole role)
	{
		if (GameManager.IsNewTutorial)
		{
			ConsoleWindow.Print("Cannot host sever in Tutorial");
			return false;
		}
		switch (role)
		{
		case NetworkRole.None:
			return true;
		case NetworkRole.Server:
		case NetworkRole.Client:
			return NetworkRole == NetworkRole.None;
		default:
			throw new ArgumentOutOfRangeException("role", role, null);
		}
	}

	private unsafe static bool ReceiveEvents()
	{
		if (NetworkState == NetworkState.Offline)
		{
			return false;
		}
		bool flag = false;
		int num = 0;
		int num2 = 0;
		long num3 = 0L;
		if (bHasSteamP2PConnections && SteamNetworking.IsP2PPacketAvailable())
		{
			P2Packet? p2Packet = SteamNetworking.ReadP2PPacket();
			if (p2Packet.HasValue)
			{
				flag = true;
				int num4 = p2Packet.Value.Data.Length;
				_incomingFullBytesCount += num4;
				num3 = (long)p2Packet.Value.SteamId.Value;
				int num5 = 0;
				using MemoryStream memoryStream = new MemoryStream(p2Packet.Value.Data);
				using BinaryReader binaryReader = new BinaryReader(memoryStream);
				num = binaryReader.ReadInt32();
				num5 += 4;
				if (memoryStream.Position != memoryStream.Length)
				{
					num2 = binaryReader.ReadInt32();
					num5 += 4;
					if (num2 + num5 != num4)
					{
						ConsoleWindow.PrintError("Packet size invalid! Check whether the total packet size = data size + size of other elements in the packet (like packet ID)");
						return false;
					}
					byte[] array = binaryReader.ReadBytes(num2);
					if (num2 > Buffer.Length)
					{
						ConsoleWindow.PrintError("Packet exceeds Buffer size");
						return false;
					}
					array.CopyTo(Buffer, 0);
				}
			}
		}
		Packet* ptr = null;
		if (!flag)
		{
			if (Instance == null)
			{
				return false;
			}
			Instance.EnsureRakNet();
			ptr = Instance.rakNet.Receive();
			if (ptr == null)
			{
				return false;
			}
			flag = true;
			num = *ptr->Data;
			num2 = (int)(ptr->Length - 1);
			if (num2 < 0)
			{
				num2 = 0;
			}
			if (num2 > Buffer.Length)
			{
				ConsoleWindow.PrintError("Packet exceeds Buffer size");
				Instance.rakNet.DeallocatePacket(ptr);
				return false;
			}
			if (num2 > 0)
			{
				new ReadOnlySpan<byte>(ptr->Data + 1, num2).CopyTo(Buffer);
			}
			num3 = (long)ptr->Guid.G;
			_incomingFullBytesCount += (int)ptr->Length + BaseRocketNetPacketSize - 1;
		}
		if (ptr != null)
		{
			Instance.rakNet.DeallocatePacket(ptr);
		}
		TimeSincePacketReceived = 0f;
		if (num >= 134)
		{
			switch ((NetworkChannel)num)
			{
			case NetworkChannel.SteamP2PHeartbeat:
				if (NetworkRole != NetworkRole.Client)
				{
					SendDataToSteamP2PConnection((ulong)num3, null, 140);
				}
				break;
			case NetworkChannel.SteamP2PConnectionAccepted:
				if (NetworkRole == NetworkRole.Client)
				{
					ConsoleWindow.Print($"Steam P2P Connection accepted with {num3} (Host ID: {_hostSteamId})");
					_hostId = num3;
					NetworkState = NetworkState.Online;
					PlayerConnected(num3, ConnectionMethod.FacepunchSteamP2P);
				}
				break;
			case NetworkChannel.StateTick:
				if (NetworkRole == NetworkRole.Client)
				{
					FragmentHandler.ReceiveState(Buffer, num2);
				}
				break;
			case NetworkChannel.PhysicsTick:
				if (NetworkRole == NetworkRole.Client)
				{
					FragmentHandler.ReceivePhysics(Buffer, num2);
				}
				break;
			case NetworkChannel.PlayerJoin:
				if (NetworkRole == NetworkRole.Client)
				{
					NetworkClient.ReceiveJoinFragment(Buffer, num2).Forget();
				}
				break;
			case NetworkChannel.GeneralTraffic:
			case NetworkChannel.Unreliable:
				HandleGeneralTraffic(_hostId, num2);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			case NetworkChannel.SteamP2PConnectionRequest:
				break;
			}
		}
		else
		{
			switch ((DefaultMessageIDTypes)num)
			{
			case DefaultMessageIDTypes.DisconnectionNotification:
				ConsoleWindow.Print("Client has disconnected.");
				PlayerDisconnected(num3);
				break;
			case DefaultMessageIDTypes.ConnectionLost:
				ConsoleWindow.Print("A client lost the connection.");
				PlayerDisconnected(num3);
				break;
			case DefaultMessageIDTypes.NewIncomingConnection:
				ConsoleWindow.Print("A connection is incoming.");
				PlayerConnected(num3, ConnectionMethod.RocketNet);
				break;
			case DefaultMessageIDTypes.ConnectionRequestAccepted:
				ConsoleWindow.Print("Our connection request has been accepted.");
				NetworkState = NetworkState.Online;
				_hostId = num3;
				PlayerConnected(num3, ConnectionMethod.RocketNet);
				break;
			case DefaultMessageIDTypes.NoFreeIncomingConnections:
				ConsoleWindow.Print("The server is full.");
				NetworkState = NetworkState.Offline;
				break;
			}
		}
		return true;
	}

	private static void HandleGeneralTraffic(long hostId, int incReceivedSize)
	{
		using MemoryStream memoryStream = new MemoryStream(incReceivedSize);
		memoryStream.Write(Buffer, 0, incReceivedSize);
		memoryStream.Seek(0L, SeekOrigin.Begin);
		Array.Clear(Buffer, 0, Buffer.Length);
		RocketBinaryReader rocketBinaryReader = new RocketBinaryReader(memoryStream);
		NetworkBase.DeserializeReceivedData(hostId, rocketBinaryReader);
		rocketBinaryReader.Dispose();
		rocketBinaryReader.Close();
	}

	private static void PlayerConnected(long connectionId, ConnectionMethod connectionMethod)
	{
		switch (NetworkRole)
		{
		case NetworkRole.Server:
			NetworkServer.ClientConnected(connectionId, connectionMethod);
			break;
		case NetworkRole.Client:
			NetworkClient.Connected(connectionId, connectionMethod);
			break;
		}
	}

	private static void PlayerDisconnected(long connectionId)
	{
		switch (NetworkRole)
		{
		case NetworkRole.Server:
			NetworkServer.ClientDisconnected(connectionId);
			break;
		case NetworkRole.Client:
			GameManager.LeaveGame();
			break;
		}
	}

	public static async UniTask SendNetworkMessageReliable<T>(Client client, NetworkChannel channel, MessageBase<T> message) where T : MessageBase<T>, new()
	{
		bool isSent = false;
		int attempts = 0;
		while (!isSent)
		{
			isSent = SendNetworkMessageToClient(client, channel, message);
			if (!isSent)
			{
				attempts++;
				if (attempts > 10)
				{
					ConsoleWindow.PrintError($"client {client.name} failed to receive message after {attempts} attempts, disconnecting them");
					client.Disconnect();
					break;
				}
				await UniTask.Delay(50);
				continue;
			}
			break;
		}
	}

	public static int GetMinClientMtu()
	{
		if (Instance == null || Instance.rakNet.IsNull())
		{
			return 0;
		}
		RakPeerInstance instance = Instance.rakNet;
		int num = int.MaxValue;
		for (uint num2 = 0u; num2 < MaxConnections; num2++)
		{
			RakNetGUID gUIDFromIndex = instance.GetGUIDFromIndex(num2);
			if (gUIDFromIndex.G != 0L && gUIDFromIndex.G != ulong.MaxValue)
			{
				SystemAddress systemAddressFromIndex = instance.GetSystemAddressFromIndex(num2);
				int mTUSize = instance.GetMTUSize(systemAddressFromIndex);
				if (mTUSize > 0)
				{
					num = Math.Min(num, mTUSize);
				}
			}
		}
		if (num != int.MaxValue)
		{
			return num;
		}
		return 0;
	}

	public unsafe static int SampleConnections(NetClientSample[] dst)
	{
		if (dst == null || Instance == null || Instance.rakNet.IsNull())
		{
			return 0;
		}
		RakPeerInstance instance = Instance.rakNet;
		int num = 0;
		RakNetStatistics rakNetStatistics = default(RakNetStatistics);
		for (uint num2 = 0u; num2 < MaxConnections; num2++)
		{
			if (num >= dst.Length)
			{
				break;
			}
			RakNetGUID gUIDFromIndex = instance.GetGUIDFromIndex(num2);
			if (gUIDFromIndex.G == 0L || gUIDFromIndex.G == ulong.MaxValue)
			{
				continue;
			}
			AddressOrGUID systemIdentifier = new AddressOrGUID(gUIDFromIndex);
			SystemAddress systemAddressFromIndex = instance.GetSystemAddressFromIndex(num2);
			if (instance.GetStatistics(systemAddressFromIndex, &rakNetStatistics) != null)
			{
				uint num3 = 0u;
				double num4 = 0.0;
				for (int i = 0; i < 4; i++)
				{
					num3 += rakNetStatistics.MessageInSendBuffer[i];
					num4 += rakNetStatistics.BytesInSendBuffer[i];
				}
				Client client = Client.Find((long)gUIDFromIndex.G);
				dst[num++] = new NetClientSample
				{
					Id = (long)gUIDFromIndex.G,
					Name = ((client != null) ? client.name : "(unknown)"),
					AvgPing = instance.GetAveragePing(systemIdentifier),
					LastPing = instance.GetLastPing(systemIdentifier),
					LowPing = instance.GetLowestPing(systemIdentifier),
					Loss = rakNetStatistics.PacketlossLastSecond,
					SendQueueBytes = (ulong)num4,
					SendQueueMsgs = num3,
					BytesSentPerSec = rakNetStatistics.ValueOverLastSecond[5],
					BytesRecvPerSec = rakNetStatistics.ValueOverLastSecond[6],
					Mtu = instance.GetMTUSize(systemAddressFromIndex),
					State = instance.GetConnectionState(systemIdentifier)
				};
			}
		}
		return num;
	}

	public unsafe static ulong GetMaxClientSendQueueBytes(out Client worstClient)
	{
		worstClient = null;
		if (Instance == null || Instance.rakNet.IsNull())
		{
			return 0uL;
		}
		RakPeerInstance instance = Instance.rakNet;
		ulong num = 0uL;
		RakNetStatistics rakNetStatistics = default(RakNetStatistics);
		for (uint num2 = 0u; num2 < MaxConnections; num2++)
		{
			RakNetGUID gUIDFromIndex = instance.GetGUIDFromIndex(num2);
			if (gUIDFromIndex.G == 0L || gUIDFromIndex.G == ulong.MaxValue)
			{
				continue;
			}
			Client client = Client.Find((long)gUIDFromIndex.G);
			if (client == null || (client.state != ClientState.Ready && client.state != ClientState.WaitingForCharacter))
			{
				continue;
			}
			SystemAddress systemAddressFromIndex = instance.GetSystemAddressFromIndex(num2);
			if (instance.GetStatistics(systemAddressFromIndex, &rakNetStatistics) != null)
			{
				double num3 = 0.0;
				for (int i = 0; i < 4; i++)
				{
					num3 += rakNetStatistics.BytesInSendBuffer[i];
				}
				if ((ulong)num3 > num)
				{
					num = (ulong)num3;
					worstClient = client;
				}
			}
		}
		return num;
	}

	public static void SendNetworkMessageAll<T>(NetworkChannel channel, MessageBase<T> message, long excludeConnectionId) where T : MessageBase<T>, new()
	{
		_messageWriter.WriteMessageType(message.GetType());
		message.Serialize(_messageWriter);
		if (_messageWriter.Position > Buffer.Length)
		{
			ConsoleWindow.PrintError("Network Message size exceeds read buffer size");
		}
		int length = _messageWriter.Length;
		byte[] array = ArrayPool<byte>.Shared.Rent(length);
		try
		{
			_messageWriter.AsSpan().CopyTo(array.AsSpan(0, length));
			foreach (Client client in NetworkBase.Clients)
			{
				bool num = client.state != ClientState.Disconnected;
				bool flag = client.connectionId != excludeConnectionId;
				if (num && flag && !SendNetworkDataToClient(client, channel, array))
				{
					ConsoleWindow.PrintError($"Failed to send to client '{client}'");
				}
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array);
			_messageWriter.Reset();
		}
	}

	public static bool SendNetworkMessageToHost<T>(NetworkChannel channel, MessageBase<T> message, bool logWarning = true) where T : MessageBase<T>, new()
	{
		return SendNetworkMessageDirect(_hostId, NetworkClient.ConnectionMethod, channel, message, logWarning);
	}

	public static bool SendNetworkMessageToClient<T>(Client client, NetworkChannel channel, MessageBase<T> message, bool logWarning = true) where T : MessageBase<T>, new()
	{
		return SendNetworkMessageDirect(client.connectionId, client.connectionMethod, channel, message, logWarning);
	}

	public static bool SendNetworkDataToHost(NetworkChannel channel, byte[] data, bool logWarning = true)
	{
		return SendNetworkDataDirect(_hostId, NetworkClient.ConnectionMethod, channel, data, logWarning);
	}

	public static bool SendNetworkDataToClient(Client client, NetworkChannel channel, byte[] data, bool logWarning = true)
	{
		return SendNetworkDataDirect(client.connectionId, client.connectionMethod, channel, data, logWarning);
	}

	public static bool SendNetworkMessageDirect<T>(long connectionId, ConnectionMethod connectionMethod, NetworkChannel channel, MessageBase<T> message, bool logWarning = true) where T : MessageBase<T>, new()
	{
		_messageWriter.WriteMessageType(message.GetType());
		message.Serialize(_messageWriter);
		if (_messageWriter.Position > Buffer.Length)
		{
			ConsoleWindow.PrintError("Network Message size exceeds read buffer size");
		}
		RocketBinaryWriter messageWriter = _messageWriter;
		bool result = SendNetworkDataDirect(connectionId, connectionMethod, channel, messageWriter.AsSpan(), logWarning);
		_messageWriter.Reset();
		return result;
	}

	public static bool SendNetworkDataDirect(long connectionId, ConnectionMethod connectionMethod, NetworkChannel channel, Span<byte> data, bool logWarning = true)
	{
		_outgoingFullBytesCount += data.Length + BaseRocketNetPacketSize - 4;
		switch (connectionMethod)
		{
		case ConnectionMethod.RocketNet:
		{
			if (Instance == null)
			{
				return false;
			}
			Instance.EnsureRakNet();
			int num = data.Length + 1;
			byte[] array = ArrayPool<byte>.Shared.Rent(num);
			try
			{
				array[0] = (byte)channel;
				data.CopyTo(array.AsSpan(1, data.Length));
				PacketPriority priority = PacketPriority.HighPriority;
				PacketReliability reliability = ((channel == NetworkChannel.PhysicsTick) ? PacketReliability.ReliableSequenced : PacketReliability.ReliableOrdered);
				byte orderingChannel = ((channel == NetworkChannel.PhysicsTick) ? ((byte)1) : ((byte)0));
				RakNetGUID guid = new RakNetGUID
				{
					G = (ulong)connectionId
				};
				return RakPeerInterface.Send(systemIdentifier: new AddressOrGUID(guid), instance: Instance.rakNet, data: array.AsSpan(0, num), priority: priority, reliability: reliability, orderingChannel: orderingChannel, broadcast: false) != 0;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
		case ConnectionMethod.FacepunchSteamP2P:
			return SendDataToSteamP2PConnection((ulong)connectionId, data, (int)channel);
		default:
			return false;
		}
	}

	public static bool SendDataToSteamP2PConnection(SteamId steamId, Span<byte> data, int packetId)
	{
		MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(packetId);
		if (data != null)
		{
			binaryWriter.Write(data.Length);
			binaryWriter.Write(data);
		}
		if (steamId.IsValid)
		{
			return SteamNetworking.SendP2PPacket(steamId, memoryStream.ToArray());
		}
		ConsoleWindow.PrintError("!! Could not send Steam P2P data to ID " + steamId.ToString() + " !!");
		return false;
	}

	public static void Close()
	{
		switch (NetworkRole)
		{
		case NetworkRole.Server:
			NetworkServer.StopServer();
			break;
		case NetworkRole.Client:
			NetworkClient.Disconnect(force: true).Forget();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case NetworkRole.None:
			break;
		}
	}

	public static void SerialisePlayerList(RocketBinaryWriter writer)
	{
		bool flag = (int)Time.unscaledTime % PLAYER_LIST_UPDATE_TIME == 0;
		writer.WriteBoolean(flag);
		if (!flag)
		{
			return;
		}
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		if (HostClient != null && Client.Find(HostClient.ClientId) == null)
		{
			HostClient.Write(writer);
			count++;
		}
		foreach (Client client in NetworkBase.Clients)
		{
			if (client != null)
			{
				client.RoundTripTime = -1;
				client.Write(writer);
				count++;
			}
		}
		Network.WriteIndex(writer, count, bufferIndex);
		GameManager.WriteClientInfo(writer);
	}

	public static void DeserialisePlayerList(RocketBinaryReader reader)
	{
		if (reader.ReadBoolean())
		{
			Network.ReadIndex<byte>(reader, out var value);
			for (int i = 0; i < value; i++)
			{
				Client.DeserialiseClient(reader);
			}
			GameManager.ReadClientInfo(reader);
		}
	}

	public static long GetEpochTime()
	{
		return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
	}

	public static void PrintLatency(long epochServer, string tag)
	{
		long num = GetEpochTime() - epochServer;
		if (num > 1)
		{
			ConsoleWindow.Print($"{tag}: {num}s");
		}
	}

	public static void Init(TransportType transportType)
	{
		if (transportType == TransportType.Rocket)
		{
			MetaServerTransport currentTransport = new MetaServerTransport();
			CurrentTransport = currentTransport;
			CurrentTransport.InitClient();
			Cookie = ((!GameManager.IsBatchMode) ? PlayerCookie.Load() : null);
			Config = NetConfig.Load();
			FetchPublicIp().Forget();
			return;
		}
		throw new ArgumentOutOfRangeException("transportType", transportType, null);
	}

	private static async UniTaskVoid FetchPublicIp()
	{
		string publicIp = await NetUtils.GetPublicIp();
		CurrentTransport.PublicIp = publicIp;
	}

	public static void StartSession(GameSessionConfig config)
	{
		if (!GameManager.IsNewTutorial)
		{
			if (CurrentTransport == null)
			{
				throw new Exception("NetworkManager hasn't been initialised. Use NetworkManager.Init");
			}
			if (!IsServer)
			{
				throw new Exception($"Role '{NetworkRole}' can not start a GameSession");
			}
			ConsoleWindow.Print($"StartSession. config: {config}");
			string gameVersion = GameManager.GetGameVersion();
			ServerType serverType = Application.platform switch
			{
				RuntimePlatform.LinuxServer => ServerType.DedicatedLinux, 
				RuntimePlatform.WindowsServer => ServerType.DedicatedWindows, 
				_ => ServerType.Hosted, 
			};
			CurrentGameSession = new GameSession
			{
				Address = config.ipAddress,
				Name = config.gameName,
				Port = config.port.ToString(),
				Version = gameVersion,
				MaxPlayers = config.maxPlayers,
				MapName = config.mapName,
				Type = serverType,
				Password = config.password,
				Players = ((serverType == ServerType.Hosted) ? 1 : 0),
				StartTime = Mathf.RoundToInt(Time.realtimeSinceStartup * 1000f),
				SteamId = config.SteamId
			};
			CurrentGameSession.GenerateCheckSum();
			if (Settings.CurrentData.ServerVisible)
			{
				CurrentTransport.RegisterGameSession(CurrentGameSession).Forget();
			}
		}
	}

	public static void UpdateSessionData(Settings.SettingData settings = null)
	{
		if (CurrentGameSession != null)
		{
			if (settings != null)
			{
				CurrentGameSession.Name = settings.ServerName;
				CurrentGameSession.Password = !string.IsNullOrEmpty(settings.ServerPassword);
				CurrentGameSession.MaxPlayers = settings.ServerMaxPlayers;
				CurrentGameSession.Port = settings.GamePort;
			}
			CurrentGameSession.Players = TotalPlayersInGame;
			int num = Mathf.RoundToInt(Time.realtimeSinceStartup * 1000f);
			CurrentGameSession.UpTime = num - CurrentGameSession.StartTime;
			CurrentGameSession.Latency = HostClient.RoundTripTime;
		}
	}

	public static void UpdateSessionData(string ipAddress, string portString)
	{
		if (CurrentGameSession == null)
		{
			ConsoleWindow.PrintError("error cannot update game session", suppressStacktrace: true);
			return;
		}
		CurrentGameSession.Address = ipAddress;
		CurrentGameSession.Port = portString;
	}

	public static async UniTask<List<GameSession>> GetGameSessionList()
	{
		GameSessionList = await CurrentTransport.GetGameSessionList();
		return GameSessionList;
	}

	public static void StopHost()
	{
		if (CurrentTransport.IsInitialised)
		{
			CurrentTransport.UnRegisterGameSession(CurrentGameSession);
		}
		CurrentTransport.Shutdown();
		CurrentGameSession = null;
	}

	public static void ClearServerList()
	{
		GameSessionList = new List<GameSession>();
	}

	private static async UniTask<Avatar> CachePlayerAvatar(ulong clientId)
	{
		if (AvatarCache.ContainsKey(clientId))
		{
			return AvatarCache[clientId];
		}
		UniTask<Texture2D> avatarAsync = CurrentTransport.GetAvatarAsync(clientId, AvatarSize.Small);
		UniTask<Texture2D> avatarAsync2 = CurrentTransport.GetAvatarAsync(clientId, AvatarSize.Medium);
		UniTask<Texture2D> avatarAsync3 = CurrentTransport.GetAvatarAsync(clientId, AvatarSize.Large);
		var (smallTexture, mediumTexture, largeTexture) = await UniTask.WhenAll(avatarAsync, avatarAsync2, avatarAsync3);
		AvatarCache[clientId] = new Avatar(smallTexture, mediumTexture, largeTexture);
		return AvatarCache[clientId];
	}

	public static async UniTask<Sprite> GetAvatarSprite(ulong clientId, AvatarSize size)
	{
		if (!AvatarCache.TryGetValue(clientId, out var value))
		{
			value = await CachePlayerAvatar(clientId);
		}
		if (value.IsValid())
		{
			return size switch
			{
				AvatarSize.Small => value.SmallSprite, 
				AvatarSize.Medium => value.MediumSprite, 
				AvatarSize.Large => value.LargeSprite, 
				_ => throw new ArgumentOutOfRangeException("size", size, null), 
			};
		}
		return null;
	}

	public static async UniTask<IReadOnlyList<SteamTransport.ItemWrapper>> GetLocalAndWorkshopItems(SteamTransport.WorkshopType type)
	{
		DirectoryInfo localDirInfo = type.GetLocalDirInfo();
		string fileName = type.GetLocalFileName();
		List<SteamTransport.ItemWrapper> items = new List<SteamTransport.ItemWrapper>();
		if (localDirInfo.Exists)
		{
			IEnumerable<SteamTransport.ItemWrapper> collection = from f in localDirInfo.GetDirectories("*", SearchOption.AllDirectories).SelectMany((DirectoryInfo d) => d.GetFiles())
				where f.Name == fileName
				select SteamTransport.ItemWrapper.WrapLocalItem(f, type);
			items.AddRange(collection);
		}
		items.AddRange(await SteamTransport.Workshop_QueryItemsAsync(type));
		items.Sort(delegate(SteamTransport.ItemWrapper b, SteamTransport.ItemWrapper a)
		{
			DateTime lastWriteTime = a.LastWriteTime;
			return lastWriteTime.CompareTo(b.LastWriteTime);
		});
		return items;
	}

	public static long GetIncomingBytes()
	{
		return _incomingFullBytesCount;
	}

	public static long GetOutgoingBytes()
	{
		return _outgoingFullBytesCount;
	}

	public static int GetOutgoingQueueSize()
	{
		return 0;
	}

	public static int GetIncomingQueueSize()
	{
		return 0;
	}

	public static ulong GenerateUniqueClientId()
	{
		while (true)
		{
			ulong num = (ulong)rand.Next(0, int.MaxValue);
			using List<Client>.Enumerator enumerator = NetworkBase.Clients.GetEnumerator();
			Client current;
			do
			{
				if (enumerator.MoveNext())
				{
					current = enumerator.Current;
					continue;
				}
				return num;
			}
			while (current == null || current.ClientId != num);
		}
	}
}
