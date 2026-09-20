using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networking.Transports;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using CharacterCustomisation;
using Cysharp.Threading.Tasks;
using DLC;
using Networking;
using Networks;
using Objects.Rockets;
using Objects.Rockets.Log;
using Steamworks;
using SyncedReferencables;
using TerrainSystem;
using TerrainSystem.Lods;
using UI;
using UI.UIFade;
using UnityEngine;
using UnityEngine.Networking;
using WorldLogSystem;

namespace Assets.Scripts;

public class NetworkClient : NetworkBase
{
	private static HandshakeType CurrentHandShakeState;

	private const int POSITION_UPDATE_FREQUENCY = 50;

	public static long HostConnectionId = -1L;

	public static long ConnectionId = -1L;

	public static uint ThingCount;

	public static ConnectionMethod ConnectionMethod = ConnectionMethod.None;

	private static uint LoadingSteps;

	private static uint LoadedSteps;

	public static int RoundTripTime;

	private static int _bytesSinceUpdate;

	private static int JoinReceivedBytes;

	public static byte[] JoinPackageBytes;

	private static CancellationTokenSource _cancellation;

	private static int _stepsSinceLast = 0;

	private static int _stepsToUpdate = 0;

	private static readonly Stopwatch _stopWatch = new Stopwatch();

	private static readonly System.Timers.Timer _connectionTimer = new System.Timers.Timer(10000.0);

	public static readonly string MultiplayerCouldNotConnectKey = "MultiplayerCouldNotConnect";

	public static readonly string MultiplayerCheckAddressKey = "MultiplayerCouldNotConnect";

	public static readonly string MultiplayerRejectedKey = "MultiplayerCouldNotConnect";

	public static readonly string MultiplayerBannedKey = "MultiplayerBanned";

	public static readonly string MultiplayerPasswordKey = "MultiplayerPassword";

	public static readonly string MultiplayerIncorrectVersionKey = "MultiplayerIncorrectVersion";

	private static int _joinBytesUntilUpdate;

	private static readonly Action<Thing> ProcessThingsOnFinishJoinAction = delegate(Thing thing)
	{
		if ((bool)thing && !thing.IsBeingDestroyed)
		{
			thing.OnFinishJoin();
		}
	};

	private static bool _isOwnerLoopAlive;

	public static Human OwnerHuman { get; private set; }

	public static string Address { get; set; }

	public static string Port { get; set; }

	private static bool IsConnected { get; set; }

	private static bool ReceivingJoinData { get; set; }

	public static uint JoinPackageTotalBytes { get; set; }

	public static int JoinBytesUntilUpdate
	{
		get
		{
			return _joinBytesUntilUpdate;
		}
		set
		{
			_joinBytesUntilUpdate = Mathf.Min(value, 1000);
			_bytesSinceUpdate = 0;
		}
	}

	public static event Action ClientFinishedJoining;

	public void Awake()
	{
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		_connectionTimer.Elapsed += ConnectionTimerOnElapsed;
		_connectionTimer.AutoReset = true;
		DiscordClient.OnClientJoinedLobby = (Action<string>)Delegate.Combine(DiscordClient.OnClientJoinedLobby, new Action<string>(JoinClientFromMenu));
	}

	public static void StopConnectionTimer()
	{
		_connectionTimer?.Stop();
	}

	private static async void ConnectionTimerOnElapsed(object sender, ElapsedEventArgs e)
	{
		ConsoleWindow.PrintError("Connection could not be established");
		await UniTask.SwitchToMainThread();
		StopConnectionTimer();
		Singleton<ConfirmationPanel>.Instance.Show(MultiplayerCouldNotConnectKey, MultiplayerCheckAddressKey, "ButtonOk", Cancel);
	}

	public static void ResetStatistics()
	{
		LoadingSteps = 0u;
		LoadedSteps = 0u;
		JoinPackageTotalBytes = 0u;
		JoinReceivedBytes = 0;
	}

	public static void Cancel()
	{
		ConsoleWindow.Print("canceled connecting");
		StopConnectionTimer();
		_cancellation?.Cancel();
		_cancellation = null;
		GameManager.LeaveGame();
	}

	public static void Connected(long hostConnectionId, ConnectionMethod connectionMethod)
	{
		ConsoleWindow.Print($"Connection ID is {hostConnectionId}");
		HostConnectionId = hostConnectionId;
		ConnectionMethod = connectionMethod;
		NetworkBase._connectionStartTime = Time.unscaledTime;
		IsConnected = true;
	}

	public static async UniTaskVoid Disconnect(bool force = false)
	{
		if (force || IsConnected)
		{
			ConsoleWindow.Print("Client Disconnecting from Host");
			CurrentHandShakeState = HandshakeType.Disconnecting;
			SendHandshakeMessage();
			await UniTask.Delay(500);
			NetworkManager.EndConnection();
			ConnectionMethod = ConnectionMethod.None;
			await UniTask.Delay(500);
			CurrentHandShakeState = HandshakeType.None;
			ConnectionId = -1L;
			ResetStatistics();
			HostConnectionId = -1L;
			ReceivingJoinData = false;
			IsConnected = false;
			GameManager.GameState = GameState.None;
		}
	}

	public static void SendToServer<T>(MessageBase<T> message, NetworkChannel channel = NetworkChannel.GeneralTraffic) where T : MessageBase<T>, new()
	{
		if (NetworkManager.IsServer)
		{
			throw new Exception("Can not send to server from server");
		}
		NetworkManager.SendNetworkMessageToHost(channel, message);
	}

	public static void Handshake(NetworkMessages.Handshake handshake)
	{
		StopConnectionTimer();
		if (ConnectionId == handshake.ConnectionId)
		{
			return;
		}
		NetworkBase.UpdateLoadingScreenContext(handshake);
		switch (handshake.HandshakeState)
		{
		case HandshakeType.Disconnecting:
		{
			if (handshake.ConnectionId == 0L)
			{
				ConsoleWindow.Print("Server disconnected");
				GameManager.LeaveGame();
				break;
			}
			ConsoleWindow.Print("Client disconnected: " + handshake.Username);
			Client client = Client.Find(handshake.ClientId);
			if (client != null)
			{
				NetworkBase.RemoveClient(client);
			}
			break;
		}
		case HandshakeType.Rejected:
			Singleton<ConfirmationPanel>.Instance.Show(MultiplayerRejectedKey, handshake.Message, "ButtonOk", Cancel);
			NetworkManager.EndConnection();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case HandshakeType.None:
		case HandshakeType.ClientReady:
			break;
		}
	}

	public static async UniTaskVoid ReceiveJoinFragment(byte[] bytes, int size)
	{
		if (_connectionTimer.Enabled)
		{
			_connectionTimer.Stop();
		}
		if (JoinPackageBytes == null)
		{
			ConsoleWindow.PrintError("JoinPackageBytes is null");
			return;
		}
		ReceivingJoinData = true;
		for (int i = 0; i < size; i++)
		{
			JoinPackageBytes[JoinReceivedBytes] = bytes[i];
			JoinReceivedBytes++;
			_bytesSinceUpdate++;
		}
		if (_bytesSinceUpdate >= JoinBytesUntilUpdate)
		{
			_bytesSinceUpdate = 0;
		}
		if (JoinReceivedBytes >= JoinPackageTotalBytes)
		{
			GridController.InitializeWorldController();
			WorldManager.StartWorld();
			await ProcessJoinData();
			ReceivingJoinData = false;
		}
		else
		{
			await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenReceivingJoinData.DisplayString);
			SendHandshakeMessage();
			await ImGuiLoadingScreen.SetProgress((float)JoinReceivedBytes / (float)JoinPackageTotalBytes);
		}
	}

	public bool JoinWithSteamP2P(SteamId hostId)
	{
		ClientPreJoin();
		if (NetworkManager.StartClient(hostId))
		{
			OnJoinStart();
			return true;
		}
		OnJoinFailed();
		return false;
	}

	public static void ClientPreJoin()
	{
		GameManager.GameState = GameState.Joining;
		WorldManager.Instance.UpdateFoV();
	}

	public static void OnJoinStart()
	{
		MainMenu.Instance.SetActive(active: false);
		PauseEventJoiningClient();
		SteamTransport.SetSteamRichPresenceStatus();
	}

	public static void OnJoinFailed(string message = "Did not connect successfully")
	{
		NetworkManager.ShutDownRaknet();
		ConsoleWindow.PrintError(message);
	}

	public void JoinClientFromMenu(string ipPort)
	{
		ClientPreJoin();
		string[] array = ipPort.Split(':');
		if (array.Length == 1 && array[0].Length == 17)
		{
			JoinWithSteamP2P(ulong.Parse(ipPort));
			return;
		}
		if (array.Length != 2)
		{
			_connectionTimer.Elapsed -= ConnectionTimerOnElapsed;
			Singleton<ConfirmationPanel>.Instance.Show(MultiplayerCouldNotConnectKey, MultiplayerCheckAddressKey, "ButtonOk", Cancel);
			return;
		}
		if (array[1] == "localhost:")
		{
			array[1] = GetLocalHost();
		}
		string address = array[0];
		ushort num = ushort.Parse(array[1]);
		ushort localPort = (ushort)(num + 1);
		if (NetworkManager.StartClient(address, num, localPort))
		{
			OnJoinStart();
		}
		else
		{
			OnJoinFailed();
		}
	}

	private static void PauseEventJoiningClient()
	{
		WorldManager.SetGamePause(pauseGame: true);
		_cancellation = new CancellationTokenSource();
		ImGuiLoadingScreen.SetActive(active: true);
		ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenConnectingToServer.DisplayString).Forget();
		ImGuiLoadingScreen.SetProgress(0f).Forget();
		_connectionTimer.Start();
	}

	private string GetLocalHost()
	{
		NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
		foreach (NetworkInterface networkInterface in allNetworkInterfaces)
		{
			if (networkInterface.OperationalStatus != OperationalStatus.Up)
			{
				continue;
			}
			foreach (UnicastIPAddressInformation unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
			{
				if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(unicastAddress.Address))
				{
					return unicastAddress.Address.ToString();
				}
			}
		}
		return "0.0.0.0";
	}

	private static async UniTask ProcessJoinData()
	{
		ImGuiLoadingScreen.OnStateChanged += SendHandshakeMessage;
		_stopWatch.Start();
		PingHandshake().Forget();
		FadePanel.ToBlack(3f);
		byte[] array = NetworkManager.Decompress(JoinPackageBytes);
		await using MemoryStream ms = new MemoryStream(array, 0, array.Length);
		using RocketBinaryReader reader = new RocketBinaryReader(ms);
		GameManager.DeserializeGameTime(reader);
		GameManager.DeserializeTerrainSeed(reader);
		CancellationToken cancellationToken = _cancellation.Token;
		WorldManager.DeserializeOnJoin(reader);
		await VoxelTerrain.LoadTerrain(WorldSetting.Current.Data.TerrainSettings, newGame: false);
		RegionManager.LoadRegionSets(WorldSetting.Current.Data);
		await Vein.DeserializeOnJoin(reader).AttachExternalCancellation(cancellationToken);
		OrbitalSimulation.DeserializeOnJoin(reader);
		TerraForming.DeserializeOnJoin(reader);
		try
		{
			VoxelTerrain.DeSerializeOnJoin(reader);
			await StructureNetwork.DeserializeOnJoin(reader).AttachExternalCancellation(cancellationToken);
			await CableNetwork.DeserializeOnJoin(reader).AttachExternalCancellation(cancellationToken);
			await TraderContact.DeserializeOnJoin(reader).AttachExternalCancellation(cancellationToken);
			await SpaceMap.DeserializeOnJoin(reader).AttachExternalCancellation(cancellationToken);
			await Rocket.DeserializeOnJoin(reader).AttachExternalCancellation(cancellationToken);
			await WorldLog.DeserializeOnJoin(reader).AttachExternalCancellation(cancellationToken);
			await WorldObjectiveState.DeserializeOnJoin(reader).AttachExternalCancellation(cancellationToken);
			await ProcessThings(reader).AttachExternalCancellation(cancellationToken);
			SyncedReferencableManager.DeserializeOnJoin(reader);
			await RocketLog.DeserializeOnJoin(reader).AttachExternalCancellation(cancellationToken);
			RoomController.DeserializeOnJoin(reader);
			await AtmosphericsManager.DeserializeOnJoin(reader).AttachExternalCancellation(cancellationToken);
			StructureNetwork.StructureNetworksOnFinishedLoad();
		}
		catch (OperationCanceledException)
		{
			reader.Close();
			goto end_IL_00d6;
		}
		reader.Close();
		await UniTask.Yield();
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenRequestingCharacter.DisplayString);
		await RequestCharacterAsync(isRespawn: false, cancellationToken);
		await UniTask.Yield();
		await VoxelTerrain.InitialiseMinablesOnLoad();
		GameManager.OnReadyToPlay();
		PointOfInterestManager.DiscoverPoiAtStartLocationWithNoMessage();
		GameManager.GameState = GameState.Running;
		GameManager.UpdateThingsOnGameStart();
		Device.InitAllDevices();
		Singleton<DiscordClient>.Instance.UpdateActivityInGame();
		Rocket.OnFinishedLoad();
		OrbitalSimulation.OnGameStarted();
		SharedDLCManager.ClientFinishedLoad();
		_stopWatch.Stop();
		UpdateHandshakeState(HandshakeType.ClientReady);
		ImGuiLoadingScreen.SetActive(active: false);
		NetworkClient.ClientFinishedJoining?.Invoke();
		ImGuiLoadingScreen.OnStateChanged -= SendHandshakeMessage;
		ConsoleWindow.Print("Done processing Join Package");
		end_IL_00d6:;
	}

	private static void UpdateHandshakeState(HandshakeType state)
	{
		CurrentHandShakeState = state;
		SendHandshakeMessage();
	}

	private static async UniTask WaitForTerrain(CancellationToken token)
	{
	}

	private static async UniTaskVoid PingHandshake()
	{
		while (CurrentHandShakeState != HandshakeType.ClientReady)
		{
			if (CurrentHandShakeState != HandshakeType.None)
			{
				SendHandshakeMessage();
			}
			await UniTask.Delay(100);
		}
	}

	private static void SendHandshakeMessage()
	{
		SendToServer(new NetworkMessages.Handshake
		{
			ConnectionId = ConnectionId,
			Username = NetworkManager.Username,
			ClientId = NetworkManager.LocalClientId,
			HandshakeState = CurrentHandShakeState,
			LoadingState = ImGuiLoadingScreen.State,
			LoadingProgress = NetworkMessages.Handshake.ProgressToByte(ImGuiLoadingScreen.Progress)
		});
	}

	public static async UniTask RequestCharacterAsync(bool isRespawn, CancellationToken token)
	{
		ConsoleWindow.Print("[NetworkClient] Sending respawn message and waiting to take control");
		ulong localClientId = NetworkManager.LocalClientId;
		PlayerCosmetics cosmetics = PlayerCosmetics.Load(Singleton<GameManager>.Instance.CustomCosmeticsSlot);
		Brain.GetValidatedBrain(localClientId, out var playerBrain);
		if ((object)playerBrain == null || isRespawn)
		{
			SendToServer(new RespawnMessage(localClientId, cosmetics));
		}
		await TakeControl(token);
	}

	public static void ReturnCharacter()
	{
		ConsoleWindow.Print("[NetworkClient] Sending return character message and relinquish control");
		SendToServer(new RelinquishControlMessage
		{
			HumanId = NetworkManager.LocalClientId
		});
	}

	public static async UniTask TakeControl(CancellationToken externalToken)
	{
		Brain playerBrain = null;
		float startTime = Time.time;
		while (!playerBrain)
		{
			ulong localClientId = NetworkManager.LocalClientId;
			Brain.GetValidatedBrain(localClientId, out playerBrain);
			if ((bool)playerBrain)
			{
				if (playerBrain.ParentHuman != null)
				{
					ConsoleWindow.Print($"Taking control of brain with id {localClientId}, userName: {NetworkManager.Username}");
					playerBrain.TakeControl(setPhysics: false);
					LodManager.EnqueueRequesterToUpdate(playerBrain.ParentHuman);
					ConsoleWindow.Print("Awaiting Terrain Generation");
					await LodManager.InitialiseLodsOnLoad();
					playerBrain.ParentHuman.SetPhysicsOnControl();
					TakeControlMessage takeControlMessage = new TakeControlMessage();
					takeControlMessage.HumanId = playerBrain.ParentHuman.ReferenceId;
					takeControlMessage.ClientId = localClientId;
					takeControlMessage.SendToServer();
					KeyManager.ResetKeyStateToDefault();
					OwnerHuman = playerBrain.ParentHuman;
					if (!_isOwnerLoopAlive)
					{
						OwnerToServerStream().Forget();
					}
				}
				else if (playerBrain.ParentBodyBag != null)
				{
					ConsoleWindow.Print($"Taking control of brain in body bag with id {localClientId}, userName: {NetworkManager.Username}");
					playerBrain.TakeControlInBodyBag();
				}
				else
				{
					ConsoleWindow.PrintError($"Brain {localClientId} has no Human or body bag parent, retrying");
					playerBrain = null;
					await UniTask.Yield(externalToken);
				}
			}
			else
			{
				if (Time.time - startTime > 60f)
				{
					ConsoleWindow.PrintError($"No Brain was found with id {localClientId} after {60f}s of searching");
					break;
				}
				await UniTask.Yield(externalToken);
			}
		}
	}

	public static void TakeControlOfExistingBrain(Brain brain)
	{
		brain.TakeControl(setPhysics: false);
		brain.ParentHuman.SetPhysicsOnControl();
		TakeControlMessage takeControlMessage = new TakeControlMessage();
		takeControlMessage.HumanId = brain.ParentHuman.ReferenceId;
		takeControlMessage.ClientId = NetworkManager.LocalClientId;
		takeControlMessage.SendToServer();
		KeyManager.ResetKeyStateToDefault();
		OwnerHuman = brain.ParentHuman;
		if (!_isOwnerLoopAlive)
		{
			OwnerToServerStream().Forget();
		}
	}

	private static async UniTask ProcessThings(RocketBinaryReader reader)
	{
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenProcessingThings.DisplayString);
		ThingCount = reader.ReadUInt32();
		ConsoleWindow.Print($"ThingCount: {ThingCount}");
		for (int i = 0; i < ThingCount; i++)
		{
			await ImGuiLoadingScreen.SetProgress((float)i / (float)ThingCount);
			Network.ReadPackedId(reader, out var referenceId);
			int prefabHash = reader.ReadInt32();
			Vector3 position = reader.ReadVector3();
			Quaternion rotation = reader.ReadQuaternion();
			Thing thing = Thing.Create<Thing>(prefabHash, position, rotation, referenceId);
			thing.DeserializeOnJoin(reader);
			thing.OnFinishedThingSync();
		}
		OcclusionManager.AllThings.ForEach(ProcessThingsOnFinishJoinAction);
	}

	private static async UniTaskVoid OwnerToServerStream()
	{
		if (NetworkManager.IsServer)
		{
			ConsoleWindow.PrintError("error server is attempting to connect to itself as human '" + (OwnerHuman?.DisplayName ?? "null") + "'");
			return;
		}
		if (!OwnerHuman)
		{
			ConsoleWindow.PrintError("error communicating with server as player has no character");
			return;
		}
		ConsoleWindow.Print("Begin Owner To Server Stream");
		OwnerMessage message = MessageBase<OwnerMessage>.Singleton;
		if (_isOwnerLoopAlive)
		{
			return;
		}
		_isOwnerLoopAlive = true;
		while (NetworkManager.IsActive && !(await UniTask.Delay(50).SuppressCancellationThrow()))
		{
			if (GameManager.GameState == GameState.Running)
			{
				SendToServer(message);
			}
		}
		_isOwnerLoopAlive = false;
	}

	public static void Interact(Interactable interactable, int state)
	{
		if (NetworkManager.IsClient)
		{
			SendToServer(new RequestInteractionToServer
			{
				InteractThingId = interactable.Parent.ReferenceId,
				InteractionId = interactable.InteractableId,
				NewState = state
			});
		}
	}

	public static void Merge(IMergeable parent, IMergeable child)
	{
		if (NetworkManager.IsClient)
		{
			SendToServer(new MergeStackablesMessage
			{
				ParentItemId = parent.ReferenceId,
				ChildItemId = child.ReferenceId
			});
		}
	}

	public static void DismissHelperHint(WorldObjectiveState worldObjectiveState, bool isDismissed)
	{
		if (NetworkManager.IsClient)
		{
			SendToServer(new DismissHelperHintMessage
			{
				ObjectiveStateId = worldObjectiveState.ReferenceId,
				IsDismissed = isDismissed
			});
		}
	}

	public static void InteractWith(Interactable interactable, Interaction interaction)
	{
		if (NetworkManager.IsClient && !interactable.Parent.PreventInteraction(out var _, interactable, interaction) && !interactable.Parent.InteractWith(interactable, interaction).IsDisabled)
		{
			SendToServer(new InteractionMessage
			{
				DestinationId = interaction.DestinationThing.ReferenceId,
				InteractionId = interactable.InteractableId,
				SourceId = interaction.SourceThing.ReferenceId,
				SourceSlotId = interaction.SourceSlot.SlotIndex,
				State = interactable.State,
				AltKey = interaction.AltKey,
				InteractWith = true
			});
		}
	}

	public static void SetRecipe(long targetId, int recipeIndex)
	{
		if (NetworkManager.IsClient)
		{
			SendToServer(new SetRecipeMessage
			{
				TargetId = targetId,
				RecipeIndex = recipeIndex
			});
		}
	}

	public static void RenameThing(long targetId, string newName)
	{
		if (NetworkManager.IsClient)
		{
			SendToServer(new ThingRenameMessage
			{
				ThingId = targetId,
				ThingName = newName
			});
		}
	}

	public static void UseItemSecondary(Entity parent, int slotId, float completedRatio)
	{
		SendToServer(new RequestUseItemToServer
		{
			ParentReferenceId = parent.ReferenceId,
			ActiveHandSlotId = (byte)slotId,
			CompletedRation = completedRatio
		});
	}

	public static void CallTrader(bool isLanding, TraderContact trader, ITraderDestination landingPad)
	{
		SendToServer(new CallTrader
		{
			IsLanding = isLanding,
			TraderReferenceId = trader.ReferenceId,
			LandingPadReferenceId = landingPad.ReferenceId
		});
	}

	public static void InterrogateTrader(TraderContact trader, SatelliteDish dish)
	{
		SendToServer(new InterrogateTrader
		{
			TraderReferenceId = trader.ReferenceId,
			SatelliteDishReferenceId = dish.ReferenceId
		});
	}
}
