using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Util;

public class NetworkBase : ManagerBase
{
	public delegate void PausedForClientDelegate(bool isGamePaused, string contextMessage = null);

	private const string FIELD_NAME = "Singleton";

	public static readonly List<Client> Clients = new List<Client>(NetworkManager.MaxConnections);

	protected static float _connectionStartTime;

	private static readonly CancellationTokenWrapper _lastClientLeaveCancellation = new CancellationTokenWrapper();

	private static readonly ConcurrentDictionary<Type, IMessageSerialisable> _messageInstances = new ConcurrentDictionary<Type, IMessageSerialisable>();

	private static readonly string PlayerConnectingKey = "PlayerIsConnecting";

	private static readonly string PlayerConnectedKey = "PlayerHasConnected";

	public static float ConnectedTime => Time.unscaledTime - _connectionStartTime;

	public static bool IsPaused { get; private set; }

	public static event PausedForClientDelegate PausedForClientConnectEvent;

	public static void AddClient(Client client)
	{
		Clients.Add(client);
		OnClientAdded();
	}

	public static void RemoveClient(Client client)
	{
		Clients.Remove(client);
		OnClientRemoved();
	}

	public static void ClearClientsList()
	{
		Clients.Clear();
		_lastClientLeaveCancellation.Cancel();
	}

	private static void OnClientAdded()
	{
		if (GameManager.IsBatchMode && Settings.CurrentData.AutoPauseServer)
		{
			_lastClientLeaveCancellation.Cancel();
		}
	}

	private static void OnClientRemoved()
	{
		if (GameManager.IsBatchMode && Settings.CurrentData.AutoPauseServer && Clients.Count <= 0)
		{
			_lastClientLeaveCancellation.CancelAndInitialize();
			AutoSaveOnLastClientLeave(_lastClientLeaveCancellation.Token).Forget();
		}
	}

	private static async UniTaskVoid AutoSaveOnLastClientLeave(CancellationToken cancellationToken)
	{
		ConsoleWindow.PrintAction("No clients connected. Will save and pause in 10 seconds.");
		await UniTask.Delay(10000, ignoreTimeScale: false, PlayerLoopTiming.Update, cancellationToken);
		if (!cancellationToken.IsCancellationRequested)
		{
			await SaveHelper.AutoSave(XmlSaveLoad.Instance.CurrentStationName, cancellationToken);
			if (!cancellationToken.IsCancellationRequested)
			{
				ConsoleWindow.PrintAction("Server Paused");
				WorldManager.SetGamePause(pauseGame: true);
			}
		}
	}

	public override void ManagerStart()
	{
		base.ManagerStart();
		NetworkMessages.UpdatePauseMessage.OnPauseChanged += PauseEvent;
	}

	private static IMessageSerialisable ResolveMessageInstance(Type type)
	{
		PropertyInfo property = type.GetProperty("Singleton", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
		if (property == null)
		{
			throw new NullReferenceException(string.Format("Failed find {0} on type {1}", "Singleton", type));
		}
		return (property.GetGetMethod()?.Invoke(null, null) as IMessageSerialisable) ?? throw new InvalidCastException(string.Format("Type {0} could not be casted to {1}", type, "IMessageSerialisable"));
	}

	public static void DeserializeReceivedData(long hostId, RocketBinaryReader reader)
	{
		Type type = reader.ReadMessageType();
		IMessageSerialisable orAdd = _messageInstances.GetOrAdd(type, ResolveMessageInstance);
		try
		{
			orAdd.Deserialize(reader);
		}
		catch (EndOfStreamException exception)
		{
			Debug.LogException(exception);
			Debug.LogError($"Message: ({type}) {orAdd}");
		}
		if (orAdd is IMessageProcessable messageProcessable)
		{
			if (!NetworkManager.IsServer && !(orAdd is NetworkMessages.Handshake) && !(orAdd is NetworkMessages.VerifyPlayerRequest) && !(orAdd is ChatMessage) && !(orAdd is AnnounceMessage) && !(orAdd is AnimationEmoteMessage) && !(orAdd is BlendShapeEmoteMessage) && !(orAdd is BlendShapeEmoteResetMessage) && !(orAdd is BlendShapeNuancedMessage) && !(orAdd is MoveToSlotMessage) && !(orAdd is TradingResultMessageFromServer))
			{
				ConsoleWindow.PrintError($"Messages should only be processed on the server. Message: ({type}) {orAdd}");
			}
			messageProcessable.Process(hostId);
		}
	}

	protected static void PauseEvent(bool pause)
	{
		if (pause != IsPaused)
		{
			IsPaused = pause;
			ConsoleWindow.Print(pause ? "Game is Paused" : "Game is resumed");
			if (NetworkManager.IsServer)
			{
				NetworkServer.SendToClients(new NetworkMessages.UpdatePauseMessage
				{
					Pause = pause
				}, NetworkChannel.GeneralTraffic, -1L);
			}
			WorldManager.SetGamePause(pause);
			if (!ImGuiLoadingScreen.IsShowing)
			{
				NetworkBase.PausedForClientConnectEvent?.Invoke(pause, pause ? PlayerConnectingKey : PlayerConnectedKey);
			}
		}
	}

	protected static void UpdateLoadingScreenContext(NetworkMessages.Handshake handshake)
	{
		ImGuiLoadingScreen.SetState(handshake.Username + " " + handshake.LoadingState, resetProgress: false).Forget();
		ImGuiLoadingScreen.SetProgress(handshake.ProgressByteToFloat()).Forget();
	}
}
