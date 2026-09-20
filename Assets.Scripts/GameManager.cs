using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Effects;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.OpenNat;
using Assets.Scripts.PlayerInfo;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using CharacterCustomisation;
using Cysharp.Threading.Tasks;
using DLC;
using GameEventBus;
using ICSharpCode.SharpZipLib.Zip;
using Networking.Servers;
using Networks;
using Objects;
using Objects.Electrical;
using Objects.RoboticArm;
using Objects.Rockets;
using Objects.Rockets.Log;
using Objects.Rockets.UI;
using Objects.Structures;
using Rendering;
using Rooms;
using Steamworks;
using SyncedReferencables;
using TerrainSystem;
using TerrainSystem.Lods;
using ThingImport;
using Trading;
using UI;
using UI.UIFade;
using UnityEngine;
using UnityEngine.Profiling;
using Util.Commands;
using Weather;

namespace Assets.Scripts;

public class GameManager : Singleton<GameManager>
{
	public delegate void Event();

	private static GameState _gameState = GameState.None;

	public static EventBus EventBus = new EventBus();

	[Header("UI")]
	[Tooltip("Dummy camera used to perform UI raycasts from rigidbody rather than interpolated position")]
	public Camera GraphicRaycastCamera;

	public List<InputWindowBase> InputWindows = new List<InputWindowBase>();

	public InputMouse InputMouse;

	public Settings Settings;

	public StatusUpdates StatusUpdates;

	public Stationpedia Stationpedia;

	public PanelToolTip PanelToolTip;

	public MenuCutscene MenuCutscene;

	public AudioSource MenuMusic;

	public float _cachedVolume;

	public GameObject StructuresStaticRoot;

	[Header("Debug")]
	public GameState DebugGameState;

	public static Dictionary<ulong, SerializedClientInfo> ClientInfo = new Dictionary<ulong, SerializedClientInfo>(NetworkManager.MaxConnections);

	public static int MainThreadId;

	public static float DeltaTime;

	public static float FixedTime;

	public static float GameTime;

	public static int FrameCount;

	private static string _gameVersion;

	private static int TotalClients = 0;

	public static bool IsTutorial = false;

	public static bool IsNewTutorial = false;

	public static bool IsScenario = false;

	public static bool IsMarsMission = false;

	[Header("Customization")]
	public Material TextureArrayColorMaterial;

	public List<ColorSwatch> CustomColors = new List<ColorSwatch>();

	public List<MeshDigit> MeshDigits = new List<MeshDigit>();

	public List<Mesh> DisplayDecimals = new List<Mesh>(8);

	public static string[] ColorStrings;

	public static List<int> LogicColorIndices = new List<int>();

	private static Dictionary<char, MeshDigit> _digitMeshLookup = new Dictionary<char, MeshDigit>();

	public static float DigitPixel = 0.01275f;

	public static bool LogFixedUpdateTime;

	private static Stopwatch _stopwatch = new Stopwatch();

	private static readonly Action<IPhysical> ThingPhysicalUpdateAction = delegate(IPhysical physicalThing)
	{
		if (physicalThing != null && physicalThing.RunPhysicsUpdate)
		{
			physicalThing.PhysicsUpdate();
		}
	};

	private static CancellationTokenSource _cancelGameTickTask;

	private static readonly int DefaultTickSpeedMs = 500;

	public static float TicksPerThirtyMinutes = 1f / GameTickSpeedSeconds * 60f * 30f;

	public static float TicksPerHour = 1f / GameTickSpeedSeconds * 60f * 60f;

	public static float TicksPerDay = 1f / GameTickSpeedSeconds * 60f * 20f;

	private static readonly object GameTickPauseLock = new object();

	private static bool _gameTickPauseScheduled;

	private static bool _gameTickPaused;

	public static uint GameTickCount;

	private static readonly Action<IPerishable> ItemDecayServerAction = delegate(IPerishable perishable)
	{
		if (perishable != null && perishable.CanItemDecay())
		{
			perishable.OnDecayServer(LastTickTimeSeconds);
		}
	};

	private static readonly Action<Thing> UpdateThingsOnGameStartAction = delegate(Thing thing)
	{
		if ((object)thing == null)
		{
			return;
		}
		try
		{
			thing.OnFinishedLoad();
			foreach (Interactable interactable in thing.Interactables)
			{
				if (interactable.JoinInProgressSync && (bool)interactable.Animator)
				{
					interactable.SetState();
					thing.OnFinishedInteractionSync(interactable);
				}
			}
		}
		catch (Exception arg)
		{
			ConsoleWindow.PrintError($"OnFinishedLoad failed for {thing.DisplayName} #{thing.ReferenceId}: {arg}");
		}
	};

	private static Action<Thing> _destroyOutOfBounds = delegate(Thing thing)
	{
		if (!(thing == null) && !thing.IsBeingDestroyed && thing.WorldGrid.OutOfBounds())
		{
			OnServer.Destroy(thing);
			ConsoleWindow.PrintAction("Deleting " + thing.DisplayName + "...");
		}
	};

	public List<ManagerBase> Managers = new List<ManagerBase>();

	private float _last100MsUpdateTime;

	private float _last1000MsUpdateTime;

	private float _lastAudioUpdateTime;

	private const float UPDATE_ONE_HUNDRED_MS_SECONDS = 0.1f;

	private const float UPDATE_ONE_SECOND = 1f;

	private const float UPDATE_AUDIO = 0.03f;

	private static readonly Action<Thing> UpdateEachFrameAction = delegate(Thing thing)
	{
		thing?.UpdateEachFrame();
	};

	public static bool IsBatchMode { get; private set; }

	public static GameState GameState
	{
		get
		{
			return _gameState;
		}
		set
		{
			if (_gameState == value || Singleton<GameManager>.IsQuitting)
			{
				return;
			}
			if (!IsBatchMode)
			{
				OnGameStateChanged(_gameState, value);
			}
			_gameState = value;
			if (GameManager.OnGameStateChange != null)
			{
				GameManager.OnGameStateChange();
			}
			switch (_gameState)
			{
			case GameState.None:
				if (CursorManager.Instance != null)
				{
					CursorManager.Instance.SunMovement = true;
				}
				break;
			case GameState.Running:
				if (GameManager.OnGameStartedOnce != null)
				{
					GameManager.OnGameStartedOnce();
					GameManager.OnGameStartedOnce = null;
				}
				WorldManager.OnWorldStarted?.Invoke();
				break;
			}
			GC.Collect();
		}
	}

	public static bool ResendClientInfo { get; set; }

	public static bool RunSimulation => !NetworkManager.IsClient;

	public static bool IsDeveloper => NetworkManager.CurrentTransport.IsDlcInstalled(760470u);

	public static bool IsThread => MainThreadId != Thread.CurrentThread.ManagedThreadId;

	public static bool IsMainThread => !IsThread;

	public static bool IsAtmosphericsThread
	{
		get
		{
			if (RunSimulation)
			{
				return AtmosphericsManager.ThreadId == Thread.CurrentThread.ManagedThreadId;
			}
			return true;
		}
	}

	public static bool HelperHintsEnabled
	{
		get
		{
			if (!Settings.CurrentData.DisplayHelperHints)
			{
				return IsNewTutorial;
			}
			return true;
		}
	}

	public static bool HasClient
	{
		get
		{
			if (RunSimulation)
			{
				return NetworkBase.Clients.Count > 0;
			}
			return false;
		}
	}

	public static string Clipboard
	{
		get
		{
			return GUIUtility.systemCopyBuffer;
		}
		set
		{
			GUIUtility.systemCopyBuffer = value;
		}
	}

	public static int ColorCount => Singleton<GameManager>.Instance.CustomColors.Count;

	public int CustomCosmeticsSlot => 0;

	public static int FixedUpdateFrame { get; private set; }

	public static int GameTickSpeedMs => DefaultTickSpeedMs;

	public static float GameTickSpeedSeconds => (float)GameTickSpeedMs / 1000f;

	public static bool IsRunning
	{
		get
		{
			GameState gameState = GameState;
			return gameState == GameState.Running || gameState == GameState.Paused || gameState == GameState.Joining || gameState == GameState.Waiting;
		}
	}

	public static float LastTickTimeSeconds { get; private set; }

	public static bool GameTickPaused
	{
		get
		{
			lock (GameTickPauseLock)
			{
				return _gameTickPaused;
			}
		}
		private set
		{
			_gameTickPaused = value;
		}
	}

	public static bool IsInitialized { get; private set; }

	public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;

	public static string GameName { get; private set; }

	public static event Event OnGameStateChange;

	public static event Event OnGameStartedOnce;

	public static void SetSteamRichPresence(string key, string value)
	{
		if (!IsBatchMode && SteamClient.IsValid)
		{
			SteamFriends.SetRichPresence(key, value);
		}
	}

	public static void AddClientInfo(ulong clientId, SerializedClientInfo info)
	{
		if (ClientInfo.TryAdd(clientId, info) && NetworkManager.IsServer)
		{
			ResendClientInfo = true;
		}
	}

	public static void ReadClientInfo(RocketBinaryReader reader)
	{
		ClientInfo.Clear();
		Network.ReadIndex<byte>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			SerializedClientInfo serializedClientInfo = SerializedClientInfo.Read(reader);
			ClientInfo.Add(serializedClientInfo.ClientId, serializedClientInfo);
		}
	}

	public static void WriteClientInfo(RocketBinaryWriter writer)
	{
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		foreach (SerializedClientInfo value in ClientInfo.Values)
		{
			value.Write(writer);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public static SerializedClientInfo GetClientInfo(ulong clientId)
	{
		ClientInfo.TryGetValue(clientId, out var value);
		return value;
	}

	public static string GetGameVersion()
	{
		return _gameVersion ?? (_gameVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString());
	}

	public static ulong GetSteamId()
	{
		if (SteamClient.IsValid)
		{
			return SteamClient.SteamId.Value;
		}
		return 0uL;
	}

	public static Material GetTextureArrayColorMaterial()
	{
		return Singleton<GameManager>.Instance.TextureArrayColorMaterial;
	}

	public static void GenerateColorStrings()
	{
		if (Singleton<GameManager>.Instance == null)
		{
			return;
		}
		LogicColorIndices.Clear();
		List<string> list = new List<string>(Singleton<GameManager>.Instance.CustomColors.Count);
		for (int i = 0; i < Singleton<GameManager>.Instance.CustomColors.Count; i++)
		{
			if (!Singleton<GameManager>.Instance.CustomColors[i].PaintOnly)
			{
				LogicColorIndices.Add(i);
				list.Add(Singleton<GameManager>.Instance.CustomColors[i].DisplayName);
			}
		}
		ColorStrings = list.ToArray();
	}

	public static bool IsLogicSelectableColor(int colorIndex)
	{
		if (0 <= colorIndex && colorIndex < Singleton<GameManager>.Instance.CustomColors.Count)
		{
			return !Singleton<GameManager>.Instance.CustomColors[colorIndex].PaintOnly;
		}
		return false;
	}

	public static int LogicColorIndexFromDropdown(int dropdownPosition)
	{
		if (0 > dropdownPosition || dropdownPosition >= LogicColorIndices.Count)
		{
			return 0;
		}
		return LogicColorIndices[dropdownPosition];
	}

	public static int LogicDropdownFromColorIndex(int colorIndex)
	{
		int num = LogicColorIndices.IndexOf(colorIndex);
		if (num >= 0)
		{
			return num;
		}
		return 0;
	}

	public static void UnloadAssetsAndCollectGarbage()
	{
		Resources.UnloadUnusedAssets();
		GC.Collect();
	}

	public static ColorSwatch GetRandomColor()
	{
		return Singleton<GameManager>.Instance.CustomColors.Pick();
	}

	public bool HasColor(Material material)
	{
		if (material == null)
		{
			return false;
		}
		foreach (ColorSwatch customColor in CustomColors)
		{
			if (customColor.Normal == material)
			{
				return true;
			}
		}
		return false;
	}

	public static int GetColorIndex(Material material)
	{
		if (material == null)
		{
			return -1;
		}
		GameManager instance = Singleton<GameManager>.Instance;
		for (int i = 0; i < instance.CustomColors.Count; i++)
		{
			ColorSwatch colorSwatch = instance.CustomColors[i];
			if (colorSwatch.Normal == material)
			{
				return i;
			}
			if (colorSwatch.Emissive == material)
			{
				return i;
			}
		}
		return -1;
	}

	public static int GetColorIndex(ColorSwatch swatch)
	{
		if (swatch == null)
		{
			return -1;
		}
		for (int i = 0; i < Singleton<GameManager>.Instance.CustomColors.Count; i++)
		{
			if (Singleton<GameManager>.Instance.CustomColors[i] == swatch)
			{
				return i;
			}
		}
		return -1;
	}

	public static int GetColorIndex(int key)
	{
		if (key == 0)
		{
			return -1;
		}
		for (int i = 0; i < Singleton<GameManager>.Instance.CustomColors.Count; i++)
		{
			if (Singleton<GameManager>.Instance.CustomColors[i].StringKey == key)
			{
				return i;
			}
		}
		return -1;
	}

	public static bool IsValidColor(int index)
	{
		if (0 <= index)
		{
			return index < Singleton<GameManager>.Instance.CustomColors.Count;
		}
		return false;
	}

	public static ColorSwatch GetColorSwatch(int index)
	{
		if (0 <= index && index < Singleton<GameManager>.Instance.CustomColors.Count)
		{
			return Singleton<GameManager>.Instance.CustomColors[index];
		}
		return null;
	}

	public static ColorSwatch GetColorSwatch(Material material)
	{
		if (material == null)
		{
			return null;
		}
		for (int i = 0; i < Singleton<GameManager>.Instance.CustomColors.Count; i++)
		{
			ColorSwatch colorSwatch = Singleton<GameManager>.Instance.CustomColors[i];
			if (colorSwatch.Normal == material)
			{
				return colorSwatch;
			}
		}
		return null;
	}

	public static ColorSwatch GetColorSwatch(string colorName)
	{
		if (string.IsNullOrEmpty(colorName))
		{
			return null;
		}
		for (int i = 0; i < Singleton<GameManager>.Instance.CustomColors.Count; i++)
		{
			ColorSwatch colorSwatch = Singleton<GameManager>.Instance.CustomColors[i];
			if (colorSwatch.Normal.name.ToLower() == colorName.ToLower())
			{
				return colorSwatch;
			}
		}
		return null;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	private static void SetMatchMode()
	{
		int isBatchMode;
		if (!Application.isBatchMode)
		{
			RuntimePlatform platform = Application.platform;
			isBatchMode = ((platform == RuntimePlatform.LinuxServer || platform == RuntimePlatform.WindowsServer) ? 1 : 0);
		}
		else
		{
			isBatchMode = 1;
		}
		IsBatchMode = (byte)isBatchMode != 0;
	}

	private void OnDisable()
	{
		NetworkManager.StopHost();
		GridController.World?.Dispose();
	}

	public static MeshDigit GetDigitMesh(char digit)
	{
		_digitMeshLookup.TryGetValue(digit, out var value);
		return value;
	}

	public static int GetMaxDecimalDigit()
	{
		return Singleton<GameManager>.Instance.DisplayDecimals.Count;
	}

	public static Mesh GetDecimalMesh(int index)
	{
		return Singleton<GameManager>.Instance.DisplayDecimals[index];
	}

	public void LateUpdate()
	{
		SpawnDataHelper.ProcessPendingActions(Time.deltaTime);
		if (!IsBatchMode)
		{
			BatchRenderer.ClearBatches();
			LogicDisplayDigitRenderer.Render();
			PylonHelper.RenderCables();
		}
	}

	public void FixedUpdate()
	{
		if (!WorldManager.IsGamePaused)
		{
			FixedUpdateFrame++;
			if (LogFixedUpdateTime)
			{
				_stopwatch.Reset();
				_stopwatch.Start();
			}
			Thing.PhysicalPoolActive.ForEach(ThingPhysicalUpdateAction);
			if (LogFixedUpdateTime)
			{
				ConsoleWindow.Print("PhysicsTick: " + StringManager.Get(_stopwatch.ElapsedMilliseconds) + "ms");
			}
		}
	}

	public static void StopGameTick()
	{
		_cancelGameTickTask?.Cancel();
		UnpauseGameTick();
		AtmosphericsManager.ClearAll();
	}

	public static void StartGameTick()
	{
		GameTickCount = 0u;
		_cancelGameTickTask = new CancellationTokenSource();
		GameTick(_cancelGameTickTask.Token).Forget();
	}

	public static void PauseGameTick()
	{
		lock (GameTickPauseLock)
		{
			_gameTickPauseScheduled = true;
		}
	}

	public static void UnpauseGameTick()
	{
		lock (GameTickPauseLock)
		{
			_gameTickPauseScheduled = false;
			GameTickPaused = false;
		}
	}

	private static async UniTask GameTick(CancellationToken cancellationToken = default(CancellationToken))
	{
		Stopwatch gameTickStopwatch = new Stopwatch();
		gameTickStopwatch.Start();
		while (!cancellationToken.IsCancellationRequested && GameState != GameState.None)
		{
			LastTickTimeSeconds = (float)gameTickStopwatch.ElapsedMilliseconds / 1000f;
			while (WorldManager.IsGamePaused || GameTickPaused)
			{
				if (_gameTickPauseScheduled)
				{
					lock (GameTickPauseLock)
					{
						GameTickPaused = true;
					}
				}
				await UniTask.Delay(GameTickSpeedMs, DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, cancellationToken);
				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}
			}
			gameTickStopwatch.Restart();
			if (RunSimulation)
			{
				WeatherManager.DamageDynamicItems();
				SolarRadiators.DamageSolarRadiators();
				TerraForming.UpdateGlobalVegetation(LastTickTimeSeconds);
			}
			ThingFire.UpdateFlames();
			GlobalAtmosphereLiquid.UpdateLiquid();
			await UniTask.SwitchToThreadPool();
			Thread.CurrentThread.Priority = Settings.NonFrameCriticalThreadPriority;
			ImGuiProfiler.Begin("GameTick");
			try
			{
				if (RunSimulation)
				{
					AtmosphericsController.HandleMainThreadEvents();
					ImGuiProfiler.Update("GameTick", "AtmosphericsController.HandleMainThreadEvents");
					AtmosphericsManager.CleanUpInvalidAtmospheres();
					ImGuiProfiler.Update("GameTick", "AtmosphericsController.CleanUpInvalidAtmospheres");
					AtmosphericsController.World.RunOpenNeighboursJobs();
					ImGuiProfiler.Update("GameTick", "AtmosphericsController.World.RunOpenNeighboursJobs");
					PlanetaryAtmosphereSimulation.TickPlanetarySimulation();
					ImGuiProfiler.Update("GameTick", "PlanetaryAtmosphereSimulation.TickPlanetarySimulation");
					AtmosphericsManager.RunCacheAtmosphereDataJobs();
					ImGuiProfiler.Update("GameTick", "AtmosphericsManager.RunCacheAtmosphereDataJobs");
					AtmosphericsManager.RunThingFireTickJobs();
					ImGuiProfiler.Update("GameTick", "AtmosphericsManager.RunThingFireTickJobs");
					AtmosphericsController.World.RefreshNetworks();
					ImGuiProfiler.Update("GameTick", "AtmosphericsController.World.RefreshNetworks");
					AtmosphericsManager.HandleMainThreadRegistrations();
					ImGuiProfiler.Update("GameTick", "AtmosphericsManager.HandleMainThreadRegistrations");
					Rocket.RocketAtmospherics();
					ImGuiProfiler.Reset("GameTick");
					AtmosphericsManager.ThingPreAtmosphere();
					ImGuiProfiler.Update("GameTick", "AtmosphericsManager.ThingPreAtmosphere");
				}
				RoomEvaluator.Instance.ThreadedWork();
				ImGuiProfiler.Update("GameTick", "RoomController.World.ThreadedWork");
				if (RunSimulation)
				{
					AtmosphericsManager.BeforeAtmosphericsTick();
					ImGuiProfiler.Update("GameTick", "AtmosphericsManager.BeforeAtmosphericsTick");
					AtmosphericsController.World.DoAtmosphereMixJobs();
					ImGuiProfiler.Update("GameTick", "AtmosphericsController.World.DoAtmosphereMixJobs");
					AtmosphericsController.World.RunInternalReactionsJobs();
					ImGuiProfiler.Update("GameTick", "AtmosphericsController.World.RunInternalReactionsJobs");
					SubmergedHandler.Tick();
					ImGuiProfiler.Reset("GameTick");
					AtmosphericsManager.ThingAtmosphereTick();
					ImGuiProfiler.Update("GameTick", "AtmosphericsManager.ThingAtmosphereTick");
					AtmosphericsManager.LifeTicksTick();
					ImGuiProfiler.Update("GameTick", "AtmosphericsManager.LifeTicksTick");
					AtmosphericsManager.AtmosphericsNetworksTick();
					ImGuiProfiler.Update("GameTick", "AtmosphericsManager.AtmosphericsNetworksTick");
					Item.AllDecayingItems.ForEach(ItemDecayServerAction);
					ImGuiProfiler.Update("GameTick", "ItemDecayServerAction");
					ElectricityManager.ElectricityTick();
					ImGuiProfiler.Update("GameTick", "ElectricityManager.ElectricityTick");
					LogicStack.LogicStackTick();
					ImGuiProfiler.Update("GameTick", "LogicStack.LogicStackTick");
					Room.RunCacheRoomDataJobs();
					ImGuiProfiler.Update("GameTick", "Room.RunCacheRoomDataJobs");
				}
				CartridgeManager.CartridgeTick();
				ImGuiProfiler.Update("GameTick", "CartridgeManager.CartridgeTick");
				if (!IsBatchMode)
				{
					AtmosphericsManager.CalculateLiquidAtmospheres();
					ImGuiProfiler.Update("GameTick", "AtmosphericsManager.CalculateLiquidAtmospheres");
					LiquidSolver.PrepareLiquidRenderBatches();
					ImGuiProfiler.Update("GameTick", "PrepareLiquidRenderBatches");
				}
				if (RunSimulation)
				{
					AtmosphericsManager.AtmosphericProcessing();
					ImGuiProfiler.Update("GameTick", "AtmosphericsManager.AtmosphericProcessing");
					AtmosphericsManager.ProcessMarkedForRemoval();
					ImGuiProfiler.Update("GameTick", "AtmosphericsManager.ProcessMarkedForRemoval");
					AtmosphericsManager.CleanUpAllAtmospheresList();
					ImGuiProfiler.Update("GameTick", "AtmosphericsManager.CleanUpAllAtmospheresList");
				}
			}
			catch (Exception exception)
			{
				Profiler.EndThreadProfiling();
				UnityEngine.Debug.LogException(exception);
			}
			Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
			ImGuiProfiler.End("GameTick");
			await UniTask.SwitchToMainThread(cancellationToken);
			if (RunSimulation)
			{
				AtmosphericsManager.PlantLifeTick(LastTickTimeSeconds);
			}
			AtmosphericsManager.SendToClients = true;
			if (_gameTickPauseScheduled)
			{
				lock (GameTickPauseLock)
				{
					GameTickPaused = true;
				}
			}
			while (gameTickStopwatch.ElapsedMilliseconds < GameTickSpeedMs)
			{
				await UniTask.Delay(1, DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, cancellationToken);
				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}
			}
			GameTickCount++;
		}
	}

	public static void QuitPrompt()
	{
		PromptPanel.Instance.ShowPrompt(PromptQuitStrings.Title, PromptQuitStrings.Body, PromptQuitStrings.Button, delegate
		{
			QuitGame();
		});
	}

	public static void QuitGame()
	{
		NetworkManager.Close();
		GameState = GameState.None;
		World.CurrentId = null;
		if (!Application.isEditor)
		{
			Process.GetCurrentProcess().Kill();
		}
	}

	private static void OnGameStateChanged(GameState oldState, GameState newState)
	{
		ConsoleWindow.PrintAction("Game State: " + newState, aged: true);
		if (newState == GameState.None)
		{
			Singleton<GameManager>.Instance.MenuCutscene.gameObject.SetActive(value: true);
			UIAudioManager.PlayMainMenuMusic(2f);
			Singleton<GameManager>.Instance.MenuCutscene.GetComponent<MenuCutscene>().SetPosition();
			GC.Collect();
		}
		if (oldState == GameState.None)
		{
			Singleton<GameManager>.Instance.MenuCutscene.gameObject.SetActive(value: false);
			Singleton<GameManager>.Instance._cachedVolume = Singleton<GameManager>.Instance.MenuMusic.volume;
			AudioFade.FadeOut(Singleton<GameManager>.Instance.MenuMusic, 3f).Forget();
			GC.Collect();
		}
	}

	public static void UpdateRichPresenceState()
	{
		if (IsRunning && SteamClient.IsValid)
		{
			SetSteamRichPresence("steam_display", "#Status_DaysInWorld");
		}
	}

	public void OpenWebsite(string URL)
	{
		Application.OpenURL(URL);
	}

	public static async UniTask StartGame()
	{
		GameState = GameState.Running;
		GameTime = Time.time;
		HelperHintsTextController.InitializePanel();
		UpdateThingsOnGameStart();
		StructureNetwork.StructureNetworksOnFinishedLoad();
		Rocket.OnFinishedLoad();
		SpaceMap.CleanUpOnFinishedLoad();
		if (InventoryManager.ParentBrain != null)
		{
			InventoryManager.ParentBrain.OnEnterInventory(InventoryManager.ParentBrain.ParentHuman);
		}
		Time.timeScale = 1f;
		XmlSaveLoad.UpdateLoadingScreen(display: false);
		WorldManager.Instance.UpdateFoV();
		if ((bool)InventoryManager.Instance && (bool)InventoryManager.Instance.ScreenScoreBoard)
		{
			InventoryManager.Instance.ScreenScoreBoard.SetActive(active: false);
		}
		if ((bool)PanelProfile.Instance)
		{
			PanelProfile.Instance.ClosePlayerProfile();
		}
		if ((bool)PanelServerInfo.Instance)
		{
			PanelServerInfo.Instance.OnCloseServerInfo();
		}
		if (Settings.CurrentData.StartLocalHost || IsBatchMode)
		{
			World.PopulateEmptyId();
			await NetworkServer.Host();
		}
		if (RunSimulation)
		{
			NetworkServer.PopulateHostClient();
		}
		if (RunSimulation)
		{
			StationAutoSave.ResetAutoSave();
		}
		WorldManager.Instance.UpdateFrameLimiter();
		OrbitalSimulation.OnGameStarted();
		if (SteamClient.IsValid)
		{
			SetSteamRichPresence("steam_display", "#Status_DaysInWorld");
		}
	}

	public static void SetTickSpeed()
	{
		RoomManager.Instance.TickSpeed = Settings.CurrentData.RoomControlTickSpeed;
	}

	public static void UpdateThingsOnGameStart()
	{
		OcclusionManager.AllThings.ForEach(UpdateThingsOnGameStartAction);
	}

	public static void OnReadyToPlay()
	{
		FadePanel.ToBlackInstant();
		if (NetworkManager.bHasSteamP2PConnections)
		{
			Singleton<ConfirmationPanel>.Instance.Show("SteamP2PWarningTitle", "SteamP2PWarningMessage", "ButtonOk", null, null, null, null, null, closeOnEscape: false);
		}
		if (NetworkManager.IsClient && NetworkBase.IsPaused)
		{
			Singleton<ConfirmationPanel>.Instance.Show("GamePaused", "PlayerIsConnecting", null, null, null, null, null, null, closeOnEscape: false);
		}
		else
		{
			FadePanel.ToTransparent(4f, 2f);
		}
		MouseModeController.InGame = true;
	}

	public static async UniTask LeaveGameAfterFade()
	{
		float fadeToTransparentTime = 0.5f;
		FadePanel.ToBlack(0.5f);
		await UniTask.Delay(500, DelayType.UnscaledDeltaTime);
		InventoryManager.Instance.GameMenuPanel.SetActive(value: false);
		LeaveGame();
		while (GameState != GameState.None)
		{
			await UniTask.WaitForEndOfFrame();
		}
		await UniTask.WaitForEndOfFrame();
		FadePanel.ToTransparent(fadeToTransparentTime);
	}

	public static void LeaveGame()
	{
		NetworkManager.Close();
		CameraController.Instance.SetThirdPersonCamera(show: false, setCullingMask: false);
		KeyManager.ResetKeyStateToDefault();
		Singleton<GameManager>.Instance.WaitingCloseThreads().Forget();
		ImGuiLoadingScreen.SetActive(active: false);
		NetworkManager.CurrentTransport.UnRegisterGameSession(NetworkManager.CurrentGameSession);
		World.CurrentId = null;
	}

	private async UniTaskVoid WaitingCloseThreads()
	{
		ResetKeepAliveCoroutine(restart: false);
		WorldManager.Instance.ResumePlay();
		await UniTask.WaitForEndOfFrame();
		float waitingStartTime = Time.time;
		while (RoomManager.Instance.IsRunning && waitingStartTime + 5f < Time.time)
		{
			await UniTask.WaitForEndOfFrame();
		}
		waitingStartTime = Time.time;
		while (AtmosphericsManager.Instance.IsRunning && waitingStartTime + 5f < Time.time)
		{
			await UniTask.WaitForEndOfFrame();
		}
		waitingStartTime = Time.time;
		while (OcclusionManager.Instance.IsRunning && waitingStartTime + 5f < Time.time)
		{
			await UniTask.WaitForEndOfFrame();
		}
		waitingStartTime = Time.time;
		while (ElectricityManager.Instance.IsRunning && waitingStartTime + 5f < Time.time)
		{
			await UniTask.WaitForEndOfFrame();
		}
		waitingStartTime = Time.time;
		while (LightManager.Instance.IsRunning && waitingStartTime + 5f < Time.time)
		{
			await UniTask.WaitForEndOfFrame();
		}
		if ((bool)InventoryManager.Instance)
		{
			InventoryManager.Instance.CancelPlacement();
		}
		ClearGameAll();
		TotalClients = 0;
		if (!IsBatchMode)
		{
			PromptPanel.Instance.DisablePromptPanel();
		}
		Time.timeScale = 1f;
	}

	private void ResetKeepAliveCoroutine(bool restart = true)
	{
		StopCoroutine(WaitDuringJoin());
		if (restart && GameState != GameState.Running)
		{
			StartCoroutine(WaitDuringJoin());
		}
	}

	private static IEnumerator WaitDuringJoin()
	{
		if (RunSimulation)
		{
			yield break;
		}
		while (GameState == GameState.Joining && GameState != GameState.None)
		{
			yield return Yielders.EndOfFrame;
		}
		yield return ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenPleaseWait.DisplayString).ToCoroutine();
		DateTime resendTimeStamp = DateTime.Now.AddSeconds(3.0);
		while (GameState != GameState.None)
		{
			if (DateTime.Compare(resendTimeStamp, DateTime.Now) < 0)
			{
				resendTimeStamp = DateTime.Now.AddSeconds(3.0);
				NetworkServer.SendToClients(new NetworkMessages.GameStateMessage
				{
					GameState = 2
				}, NetworkChannel.GeneralTraffic, -1L);
			}
			yield return Yielders.EndOfFrame;
		}
	}

	private void ClearGameAll()
	{
		GameState = GameState.None;
		StopGameTick();
		ClientInfo.Clear();
		AtmosphericsWorker.Clear();
		RoomManager.Instance.StopManager();
		OcclusionManager.Instance.StopManager();
		ElectricityManager.Instance.StopManager();
		AtmosphericsManager.Instance.StopManager();
		LightManager.Instance.StopManager();
		WorldManager.Instance.UpdateFoV();
		WeatherManager.ClearAll();
		TerraForming.Clear();
		UnityMainThreadDispatcher.Instance().ClearAll();
		CameraController.SetNightVision(show: false);
		CameraController.SetUnderwater(show: false);
		NetworkAtmosphereEvent.Clear();
		AtmosphericEventInstance.Clear();
		CableNetwork.ClearAll();
		GridController.World?.ClearAll();
		Referencable.ClearReferences();
		SyncedReferencableManager.ClearAll();
		ReferencableNetworkHelper.ClearAll();
		StructureNetwork.ClearAll();
		SolarRadiators.AllSolarRadiators.Clear();
		WorldParticleEffect.GridsInUse.Clear();
		Chicken.AllChickens.Clear();
		ElectricityManager.ClearAll();
		LightManager.ClearAll();
		GridPathfinder.DeviceBlockedList.Clear();
		SubmergedHandler.Clear();
		AtmosphericsManager.ClearAll();
		OcclusionManager.ClearAll();
		LogicStack.ClearAll();
		CursorManager.ClearAll();
		InventoryWindowManager.ClearAll();
		XmlSaveLoad.ClearAll();
		BatchRenderer.ClearAll();
		ThingSpawnData.ClearAll();
		LiquidSolver.Instance.ClearAll();
		PlanetaryAtmosphereSimulation.Clear();
		Geyser.ClearAll();
		AlertMessage.ClearAll();
		PointOfInterestMessage.ClearAll();
		Transmitters.ClearAll();
		RocketDataDownLink.AllITransmitDataNetworkDevices.Clear();
		Singleton<TraderShuttlePool>.Instance.Clear();
		SpaceMap.ClearAll();
		Rocket.ClearAll();
		RocketParkSlot.ClearAll();
		OrbitalSimulation.ClearAll();
		RocketLog.ClearAll();
		RocketCanvas.Instance.ClearAll();
		RocketMotherboard.ClearAll();
		Thing.ClearAll();
		SharedDLCManager.ClearAll();
		Cartridge.ClearAll();
		SpawnDataHelper.ClearAll();
		HelperHintsManager.ClearAll();
		RoboticArmDockCollector.ClearAll();
		GlobalAtmosphereLiquid.Clear();
		VoxelTerrain.ClearAll();
		VeinCluster.ClearAll();
		RegionManager.ClearAll();
		PointOfInterestManager.ClearAll();
		VeinGenerationWorker.ClearAll();
		NonThingOcclusionHandler.ClearAll();
		PylonHelper.Clear();
		if (!IsBatchMode)
		{
			CameraController.Instance.IsSensorLensesFxActive = false;
			CameraController.Instance.ClearSolarStormEffect();
		}
		CharacterCustomisationManager.UnloadScene();
		if (!IsBatchMode)
		{
			if (InventoryManager.Instance != null)
			{
				InventoryManager.Instance.ToggleScoreboard(forceHide: true);
				if (GameState != GameState.Joining)
				{
					InventoryWindowManager.HideAll();
				}
			}
			XmlSaveLoad.Instance.PanelMainMenu.transform.GetChild(1).gameObject.SetActive(value: true);
			Assets.Scripts.UI.MainMenu.Instance.PageManager.DisableAllPages();
			Assets.Scripts.UI.MainMenu.Instance.PageManager.EnableMainMenuPage("MainMenu");
			SetSteamRichPresence("steam_display", "#Status_AtMainMenu");
			SpinnerPannel.Instance.Close();
		}
		if (PlayerInfoManager.PlayerDictionary != null)
		{
			PlayerInfoManager.PlayerDictionary.Clear();
		}
		Entity.AllEntities.Clear();
		for (int num = Human.AllHumans.Count - 1; num >= 0; num--)
		{
			UnityEngine.Object.Destroy(Human.AllHumans[num]);
		}
		RoomEvaluator.Instance.Clear();
		Human.AllHumans.Clear();
		Brain.PlayerBrains.Clear();
		ITrackable.Trackables.Clear();
		RoomManager.ClearAll();
		Structure.ClearAll();
		InventoryManager.ClearAll();
		StatusUpdates.Parent = null;
		TraderContact.Clear();
		ContactSlot.ClearAll();
		CommsMotherboard.CommsMotherboards.Clear();
		WorldManager.Instance.ClearWorld();
		StatusUpdates.ResetStatusIcons();
		NetworkManager.EndConnection();
		NetworkManager.ClearAll();
		NetworkBase.ClearClientsList();
		BatchedRenderer.ClearAll();
		Clearables.ClearAll();
		IThreadedWorker.ResetThreadStatistics();
		StreamingAssetLoader.UnloadOnDemandTextures();
		Vein.AllAimeeQueuedMinables.Clear();
		MouseModeController.InGame = false;
		StartCoroutine(UnloadUnusedAssets());
	}

	public static void DeleteOutOfBoundsObjects()
	{
		if (RunSimulation)
		{
			ConsoleWindow.Print("Checking for out of bounds objects");
			OcclusionManager.AllThings.ForEach(_destroyOutOfBounds);
			ConsoleWindow.Print("Finished checking for out of bounds objects");
		}
	}

	public static void SaveScreenShot(string folder, string file_name, int width, int height, Camera target = null)
	{
		if (target == null)
		{
			target = Camera.main;
		}
		RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
		renderTexture.antiAliasing = 1;
		target.targetTexture = renderTexture;
		target.Render();
		Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false);
		RenderTexture.active = renderTexture;
		texture2D.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, recalculateMipMaps: false);
		byte[] bytes = texture2D.EncodeToPNG();
		UnityEngine.Object.Destroy(texture2D);
		File.WriteAllBytes(folder + "/" + file_name, bytes);
		RenderTexture.active = null;
		target.targetTexture = null;
		renderTexture.DiscardContents();
	}

	public static byte[] CreateScreenShot(int width, int height, Camera target)
	{
		if (IsBatchMode)
		{
			return new byte[0];
		}
		RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
		renderTexture.antiAliasing = 1;
		target.targetTexture = renderTexture;
		target.Render();
		Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false);
		RenderTexture.active = renderTexture;
		texture2D.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, recalculateMipMaps: false);
		byte[] result = texture2D.EncodeToPNG();
		UnityEngine.Object.Destroy(texture2D);
		RenderTexture.active = null;
		target.targetTexture = null;
		renderTexture.DiscardContents();
		return result;
	}

	private IEnumerator UnloadUnusedAssets()
	{
		yield return Yielders.EndOfFrame;
		Resources.UnloadUnusedAssets();
		GC.Collect();
	}

	public static void SerializeGameTime(RocketBinaryWriter writer)
	{
		writer.WriteSingle(GameTime);
	}

	public static void DeserializeGameTime(RocketBinaryReader reader)
	{
		NetworkTime.UpdateServerTimeOffset(reader.ReadSingle());
	}

	public static void SerializeTerrainSeed(RocketBinaryWriter writer)
	{
		writer.WriteInt32(WorldManager.Seed);
	}

	public static void DeserializeTerrainSeed(RocketBinaryReader reader)
	{
		WorldManager.Seed = reader.ReadInt32();
	}

	private void HandleCancelAction()
	{
		if (AlertPanel.Instance.AlertWindow.activeInHierarchy)
		{
			AlertPanel.Instance.DisableAlertPanel();
		}
		else if (!IsBatchMode && PromptPanel.Instance.PromptWindow.activeInHierarchy)
		{
			PromptPanel.Instance.DisablePromptPanel();
		}
		else if (!IsBatchMode && InputWindow.Instance.GameObject.activeInHierarchy)
		{
			InputWindow.Instance.ButtonInputCancel();
		}
		else if (CharacterCustomisationManager.IsSceneLoaded)
		{
			CharacterCustomisationManager.UnloadScene();
		}
		else if (Assets.Scripts.UI.MainMenu.Instance.GameObject.activeInHierarchy)
		{
			Assets.Scripts.UI.MainMenu.Instance.PageManager.PageBackward();
		}
		else
		{
			QuitPrompt();
		}
	}

	public static void SetActive(DynamicThing dynamicThing)
	{
		Thing.PhysicalPoolActive.Add(dynamicThing);
	}

	public static void SetInactive(DynamicThing dynamicThing)
	{
		if (dynamicThing is ICircuitHolder)
		{
			Thing thing = dynamicThing.ParentSlot?.Parent;
			if (thing is Human || thing is IPlayerVehicle)
			{
				return;
			}
		}
		dynamicThing.OnRemoveFromPool(Thing.PhysicalPoolActive);
	}

	[DllImport("user32.dll")]
	public static extern bool SetWindowText(IntPtr hwnd, string lpString);

	[DllImport("user32.dll")]
	public static extern IntPtr FindWindow(string className, string windowName);

	private static void SetApplicationWindowName(string name)
	{
		SetWindowText(FindWindow(null, Application.productName), name);
	}

	private void Awake()
	{
		MainThreadId = Thread.CurrentThread.ManagedThreadId;
		Thread.CurrentThread.Priority = Settings.MainThreadPriority;
		ConsoleWindow.ApplySettings();
		ConsoleWindow.Initialize();
		GameName = $"Stationeers v{Version}";
		SetApplicationWindowName(GameName);
		try
		{
			NetworkManager.Init(TransportType.Rocket);
		}
		catch (Exception exception)
		{
			UnityEngine.Debug.LogException(exception, this);
		}
		_digitMeshLookup.Clear();
		foreach (MeshDigit meshDigit in MeshDigits)
		{
			_digitMeshLookup.Add(meshDigit.Digit.ToCharArray(0, 1)[0], meshDigit);
		}
		Localization.OnLanguageChanged = (Action)Delegate.Combine(Localization.OnLanguageChanged, new Action(GenerateColorStrings));
		if (Settings.CurrentData.UseCustomWorkThreadsCount)
		{
			ThreadPool.SetMinThreads(Settings.CurrentData.MinWorkerThreads, Settings.CurrentData.MinCompletionPortThreads);
			ThreadPool.SetMaxThreads(Settings.CurrentData.MaxWorkerThreads, Settings.CurrentData.MaxCompletionPortThreads);
		}
	}

	private async void Start()
	{
		ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;
		DrawBatch.Initialize();
		ConsoleWindow.Print($"Version : {Assembly.GetExecutingAssembly().GetName().Version}");
		Singleton<NatDiscoverer>.Instance.GamePort = 7777;
		Singleton<MonoBehaviourListener>.Instance.SetType(MonoBehaviourType.GameManager);
		int num = 0;
		foreach (ManagerBase manager in Managers)
		{
			try
			{
				manager.ManagerAwake();
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogException(ex);
				ConsoleWindow.PrintError("error in awake with '" + manager.GetType().Name + "' " + ex.Message);
				num++;
			}
		}
		foreach (ManagerBase manager2 in Managers)
		{
			try
			{
				manager2.ManagerStart();
			}
			catch (Exception ex2)
			{
				UnityEngine.Debug.LogException(ex2);
				ConsoleWindow.PrintError("error in start with '" + manager2.GetType().Name + "' " + ex2.Message);
				num++;
			}
		}
		if (num == 0)
		{
			ConsoleWindow.Print($"loaded {Managers.Count} systems successfully");
		}
		else
		{
			ConsoleWindow.Print($"loaded {Managers.Count} systems with {num} exceptions");
		}
		foreach (InputWindowBase inputWindow in InputWindows)
		{
			inputWindow.Initialize();
		}
		DLCManager.Initialize();
		ControllerAxisItem.InitializeJoysticks();
		InputMouse.Initialize();
		Settings.Initialize();
		Stationpedia.Initialize();
		PanelToolTip.Initialize();
		MoleHelper.Initialize();
		await WorldManager.Initialize();
		StatusUpdates.Initialize();
		StringReferenceInt.Initialize();
		Logicable.Initialize();
		DifficultySetting.SetCurrent();
		CommandLine.ExecutePostLaunchCommands();
		Settings.ApplyVideoSettings();
		Achievements.Initialize();
		LodObjectCache.Initialize();
		ConsoleWindow.Print("game manager initialized");
		IsInitialized = true;
		MajorUpdatePopup();
	}

	private void MajorUpdatePopup()
	{
		if (!PlayerCookie.Current.DismissedOldSavePopup)
		{
			List<DirectoryInfo> oldSaveFolders = new List<DirectoryInfo>();
			DirectoryInfo[] directories = StationSaveUtils.GetSavePathSavesSubDir().GetDirectories();
			foreach (DirectoryInfo directoryInfo in directories)
			{
				if (directoryInfo.GetFiles("world.xml").Length == 1)
				{
					oldSaveFolders.Add(directoryInfo);
				}
			}
			if (oldSaveFolders.Count > 0)
			{
				string saveCountString = StringManager.Get(oldSaveFolders.Count);
				string title = ((oldSaveFolders.Count == 1) ? GameStrings.OldSaveDetected.DisplayString : GameStrings.OldSavesDetected.AsString(saveCountString));
				Singleton<ConfirmationPanel>.Instance.ShowWithRawTitle(title, "OldSavesDetectedMessage", "ButtonDelete", delegate
				{
					string message = ((oldSaveFolders.Count == 1) ? ((string)GameStrings.DeleteOldSave) : GameStrings.DeleteOldSaves.AsString(saveCountString));
					Singleton<ConfirmationPanel>.Instance.ShowWithRawMessage("ButtonConfirm", message, "ButtonDelete", delegate
					{
						foreach (DirectoryInfo item in oldSaveFolders)
						{
							item.Delete(recursive: true);
						}
					}, "ButtonCancel", null, null, null, closeOnEscape: false);
				}, "ButtonClose", null, "ButtonDontShowAgain", delegate
				{
					PlayerCookie.Current.DismissOldSavePopup();
					PlayerCookie.Current.Save();
				}, closeOnEscape: false);
			}
		}
		if (!PlayerCookie.Current.DismissedMajorUpdatePopup)
		{
			Singleton<ConfirmationPanel>.Instance.Show("MajorUpdateTitle", "MajorUpdateDescription", "ButtonOk", null, "ButtonDontShowAgain", delegate
			{
				PlayerCookie.Current.DismissMajorUpdatePopup();
				PlayerCookie.Current.Save();
			}, null, null, closeOnEscape: false);
		}
	}

	public void Update()
	{
		if (!IsInitialized)
		{
			return;
		}
		if (!WorldManager.IsGamePaused)
		{
			DeltaTime = Time.deltaTime;
			FixedTime = Time.fixedTime;
			GameTime = Time.time;
			FrameCount = Time.frameCount;
			DebugGameState = GameState;
			if (RunSimulation)
			{
				Interactable.DoQueuedInteractions();
			}
			OcclusionManager.UpdatingThings.ForEach(UpdateEachFrameAction);
			if (!IsBatchMode)
			{
				OrbitalViewController.Update();
			}
			if (!IsBatchMode && Time.time > _lastAudioUpdateTime + 0.03f)
			{
				float deltaTime = Time.time - _lastAudioUpdateTime;
				_lastAudioUpdateTime = Time.time;
				foreach (Thing updatingAudioThing in OcclusionManager.UpdatingAudioThings)
				{
					updatingAudioThing.UpdateAudio(deltaTime);
				}
			}
			if (Time.time > _last100MsUpdateTime + 0.1f)
			{
				float deltaTime2 = Time.time - _last100MsUpdateTime;
				_last100MsUpdateTime = Time.time;
				foreach (Thing updatingThings100M in OcclusionManager.UpdatingThings100MS)
				{
					updatingThings100M.Update100MS(deltaTime2);
				}
				foreach (ManagerBase manager in Managers)
				{
					manager.SlowUpdate();
				}
			}
			if (Time.time > _last1000MsUpdateTime + 1f)
			{
				float deltaTime3 = Time.time - _last1000MsUpdateTime;
				_last1000MsUpdateTime = Time.time;
				foreach (Thing updatingThings1000M in OcclusionManager.UpdatingThings1000MS)
				{
					updatingThings1000M.Update1000MS(deltaTime3);
				}
			}
			Thing.UpdateLodFlares();
			if (GameState == GameState.None && KeyManager.GetButtonDown(KeyMap.Cancel))
			{
				HandleCancelAction();
			}
		}
		foreach (ManagerBase manager2 in Managers)
		{
			manager2.ManagerUpdate();
		}
		BatchRenderer.RenderAll();
		WindTurbineGenerator.UpdateWind();
	}

	public override void OnApplicationQuit()
	{
		StationAutoSave.Cancel();
	}
}
