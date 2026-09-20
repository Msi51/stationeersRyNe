using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networking.Transports;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using DLC;
using ICSharpCode.SharpZipLib.Zip;
using Networking.Servers;
using Networks;
using Objects.RoboticArm;
using Objects.Rockets;
using Objects.Rockets.Scanning;
using Reagents;
using SyncedReferencables;
using TerrainSystem;
using TerrainSystem.Lods;
using Trading;
using UI.UIFade;
using UnityEngine;
using UnityEngine.UI;
using Util;
using Weather;
using WorldLogSystem;

namespace Assets.Scripts.Serialization;

public class XmlSaveLoad : ManagerBase
{
	private enum SaveAction
	{
		BackToMainMenu,
		SaveAndDisconnect,
		SaveAndExit
	}

	[XmlInclude(typeof(ThingSaveData))]
	[XmlRoot("WorldData")]
	public class WorldData
	{
		[XmlAttribute]
		public string Id;

		[XmlElement]
		public string Game = Assembly.GetExecutingAssembly().GetName().Name;

		[XmlElement]
		public string GameVersion = GameManager.GetGameVersion();

		[XmlElement]
		public long DateTime = System.DateTime.Now.ToFileTime();

		[XmlElement]
		public uint DaysPast = WorldManager.DaysPast;

		[XmlElement("WorldSetting")]
		public SerializedId WorldSetting = new SerializedId("Space");

		[XmlElement("WorldName")]
		public string WorldName;

		[XmlElement("Name")]
		public string OldName;

		[XmlElement("DifficultySetting")]
		public SerializedId DifficultySetting = new SerializedId("Fallback");

		[XmlElement("StartCondition")]
		public SerializedId StartCondition;

		[XmlElement("StartLocation")]
		public SerializedId StartLocation;

		[XmlElement("ClientInfo")]
		public List<SerializedClientInfo> ClientInfos = new List<SerializedClientInfo>();

		[XmlElement("Celestial")]
		public OrbitSimulationSaveData CelestialData = new OrbitSimulationSaveData();

		[XmlElement("TerraForming")]
		public TerraFormingSaveData TerraFormingSaveData;

		[XmlArray]
		public List<StationContactData> StationContacts = new List<StationContactData>();

		[XmlArray]
		public List<ContactSlotSaveData> ContactSlotSaveDatas = new List<ContactSlotSaveData>();

		[XmlElement]
		public int OverallIndexOfContacts;

		[XmlElement]
		public int WorldSeed = WorldManager.Seed;

		[XmlArray("Rooms")]
		[XmlArrayItem("Room")]
		public List<RoomData> Rooms = new List<RoomData>();

		[XmlArray("PipeNetworks")]
		[XmlArrayItem("NetworkId")]
		public long[] PipeNetworks;

		[XmlArray("CableNetworks")]
		[XmlArrayItem("NetworkId")]
		public long[] CableNetworks;

		[XmlArray("ChuteNetworks")]
		[XmlArrayItem("NetworkId")]
		public long[] ChuteNetworks;

		[XmlArray("LandingPadNetworks")]
		[XmlArrayItem("NetworkId")]
		public long[] LandingPadNetworks;

		[XmlArray("RocketNetworks")]
		[XmlArrayItem("NetworkId")]
		public long[] RocketNetworks;

		[XmlArray("RoboticArmNetworks")]
		[XmlArrayItem("NetworkId")]
		public long[] RoboticArmNetworks;

		[XmlArray("RocketShuttleNetworks")]
		[XmlArrayItem("NetworkId")]
		public long[] RocketShuttleNetworks;

		[XmlArray("AllThings")]
		public List<ThingSaveData> OrderedThings;

		[XmlArray("SyncedReferencables")]
		public List<SyncedReferencableSaveData> SyncedReferencables = new List<SyncedReferencableSaveData>();

		[XmlArray("Things")]
		public List<ThingSaveData> OldThings;

		[XmlArray("PendingSpawnActions")]
		public List<PendingSpawnActionSaveData> PendingSpawnActions = new List<PendingSpawnActionSaveData>();

		[XmlArray("Atmospheres")]
		public List<AtmosphereSaveData> Atmospheres = new List<AtmosphereSaveData>();

		[XmlElement("SpaceMap")]
		public SpaceMapSaveData SpaceMap;

		[XmlArray("WorldObjectives")]
		[XmlArrayItem("Objective", typeof(WorldObjectiveSaveData))]
		public List<WorldObjectiveSaveData> Objectives = new List<WorldObjectiveSaveData>();

		[XmlArray("Rockets")]
		public List<RocketSaveData> RocketSaveDatas = new List<RocketSaveData>();

		[XmlArray("RocketNameHistory")]
		public List<string> RocketNameHistory = new List<string>();

		[XmlElement]
		public UserInterfaceSaveData UserInterface = new UserInterfaceSaveData();

		[XmlElement]
		public WeatherManagerSavedData WeatherManagerData = new WeatherManagerSavedData();

		[XmlElement]
		public Vector3 OriginPosition = Vector3.zero;

		[XmlArray("WorldLog")]
		[XmlArrayItem("TraderCrashEventData", typeof(TraderCrashEventData))]
		[XmlArrayItem("TraderEnteredRangeEventData", typeof(TraderEnteredRangeEventData))]
		[XmlArrayItem("TraderLeftRangeEventData", typeof(TraderLeftRangeEventData))]
		public List<WorldEventData> WorldEventData = new List<WorldEventData>();

		[XmlElement]
		public PlanetaryAtmosphereSaveData PlanetaryAtmosphere;

		[XmlArray("TerrainChunkChecksums")]
		public int[] TerrainChunkChecksums;

		public List<ThingSaveData> GetThings()
		{
			if (OldThings == null || OldThings.Count <= 0)
			{
				return OrderedThings;
			}
			return OldThings;
		}

		public ThingSaveData GetThing(int i)
		{
			return GetThings()[i];
		}

		public WorldMetaData GetMetaData(string worldFileName, ulong workShopFileHandle)
		{
			return new WorldMetaData
			{
				Id = Id,
				WorldName = WorldSetting.Id,
				DateTime = DateTime,
				Game = Game,
				DaysPast = DaysPast,
				GameVersion = GameVersion,
				Rooms = Rooms.Count,
				PipeNetworks = PipeNetworks.Length,
				CableNetworks = CableNetworks.Length,
				Things = GetThings().Count,
				Atmospheres = Atmospheres.Count,
				WorldFileName = worldFileName,
				WorkShopFileHandle = workShopFileHandle
			};
		}
	}

	[XmlInclude(typeof(ThingSaveData))]
	[XmlRoot("WorldMetaData")]
	public class WorldMetaData
	{
		[XmlAttribute]
		public string Id = string.Empty;

		[XmlElement]
		public string Game = Assembly.GetExecutingAssembly().GetName().Name;

		[XmlElement]
		public string GameVersion = GameManager.GetGameVersion();

		[XmlElement]
		public long DateTime = System.DateTime.Now.ToFileTime();

		[XmlElement]
		public uint DaysPast = WorldManager.DaysPast;

		[XmlElement]
		public string WorldName = WorldManager.CurrentWorldId;

		[XmlElement]
		public string WorldFileName = "";

		[XmlElement]
		public ulong WorkShopFileHandle;

		[XmlElement("NumberOfRooms")]
		public int Rooms;

		[XmlElement("NumberOfPipeNetworks")]
		public int PipeNetworks;

		[XmlElement("NumberOfCableNetworks")]
		public int CableNetworks;

		[XmlElement("NumberOfThings")]
		public int Things;

		[XmlElement("NumberOfAtmospheres")]
		public int Atmospheres;
	}

	private static readonly Dictionary<long, Thing> WrongReference = new Dictionary<long, Thing>();

	[Header("Panels")]
	public GameObject PanelMainMenu;

	public GameObject PanelNewWorld;

	public GameObject NewPanelNewWorld;

	public GameObject PanelSave;

	public GameObject PanelJoinWorld;

	public GameObject PanelSettings;

	public MainMenu MainMenu;

	public static Type[] ExtraTypes;

	public static XmlSaveLoad Instance;

	private static readonly string WorldFileName = "world.xml";

	private static readonly string ChunkFileName = "world.bin";

	private static readonly string TerrainFileName = "terrain.dat";

	private static readonly string MetaFileName = "world_meta.xml";

	public static readonly string WorkShopPreviewFileName = "preview.png";

	public static readonly string LoadWordScreenShot = "screenshot.png";

	public static readonly string AutoSave = "_AutoSave";

	public static bool WorldIsReadOnly;

	private ulong WorkShopFileHandle;

	private SaveAction ActionInSave;

	private Button _loadWorldButton;

	private ButtonSizeControl _loadWorldButtonSizeControl;

	private Button _continueButton;

	private ButtonSizeControl _continueButtonSizeControl;

	private Button _scenariosButton;

	private ButtonSizeControl _scenariosButtonSizeControl;

	public static bool IsReadyToPlayWorldAudio;

	public string CurrentStationName;

	private bool _saveNameSet;

	private InputField _currentInputField;

	private float MIN_SAVE_INTERVAL = 60f;

	private float MAX_SAVE_INTERVAL = 3600f;

	public bool SavingWorld;

	private bool _copyingWorld;

	private int TickSpeed = 1;

	private WorldData _worldData;

	private bool _validWordData;

	private static readonly object _lockWorldData = new object();

	private static readonly Action<Atmosphere> AtmosphereSaveAction = delegate(Atmosphere atmosphere)
	{
		if (atmosphere != null && !atmosphere.BeingDestroyed && atmosphere.WillSave)
		{
			if (atmosphere.Mode == AtmosphereHelper.AtmosphereMode.Network && atmosphere.IsAwaitingEvent)
			{
				throw new Exception(atmosphere.DisplayName + " was modified during the save process. Save aborted due to possible data corruption");
			}
			if (atmosphere.IsValidWorld() || atmosphere.IsValidThing() || (atmosphere.AtmosphericsNetwork != null && atmosphere.AtmosphericsNetwork.IsNetworkValid()))
			{
				_temporaryWorldData.Atmospheres.Add(new AtmosphereSaveData(atmosphere));
			}
		}
	};

	private static WorldData _temporaryWorldData;

	public static readonly XmlReaderSettings XmlReaderSettings = new XmlReaderSettings
	{
		CheckCharacters = false
	};

	private static bool ResetTerrain = false;

	public static readonly Dictionary<long, long> PipeNetworkIdToIReferencable = new Dictionary<long, long>();

	public static readonly Dictionary<long, long> CableNetworkIdToIReferencable = new Dictionary<long, long>();

	public static readonly Dictionary<long, long> ChuteNetworkIdToIReferencable = new Dictionary<long, long>();

	public StationSaveContainer CurrentWorldSave { get; set; }

	public SteamTransport.ItemWrapper? CurrentWorldFile => SteamTransport.ItemWrapper.WrapLocalItem(CurrentWorldSave.World, SteamTransport.WorkshopType.World);

	public bool CopyingWorld => _copyingWorld;

	public static int CurrentSaveRevision { get; private set; }

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		if ((bool)Instance)
		{
			base.gameObject.DestroyGameObject(this);
			return;
		}
		Instance = this;
		if (!GameManager.IsBatchMode)
		{
			Animator[] componentsInChildren = MainMenu.ButtonGrid.GetComponentsInChildren<Animator>();
			foreach (Animator animator in componentsInChildren)
			{
				MainMenu.Animator.Add(animator);
				MainMenu.SizeControl.Add(animator.gameObject.GetComponent<ButtonSizeControl>());
			}
			componentsInChildren = MainMenu.SmallButtonGrid.GetComponentsInChildren<Animator>();
			foreach (Animator animator2 in componentsInChildren)
			{
				MainMenu.Animator.Add(animator2);
				MainMenu.SizeControl.Add(animator2.gameObject.GetComponent<ButtonSizeControl>());
			}
		}
	}

	public void DisableMainMenu()
	{
		MainMenu.MenuPanel.SetActive(value: false);
		foreach (ButtonSizeControl item in MainMenu.SizeControl)
		{
			item.OnPointerExit();
		}
	}

	public static void AddExtraTypes(ref List<Type> extraTypes)
	{
		extraTypes.Add(typeof(CelestialSaveData));
		extraTypes.Add(typeof(CelestialBodySaveData));
		extraTypes.Add(typeof(RotatingCelestialBodySaveData));
		extraTypes.Add(typeof(WorldManager.GasTradeData));
		extraTypes.Add(typeof(PlayableBodyReference));
		extraTypes.Add(typeof(CelestialBodyReference));
		extraTypes.Add(typeof(CelestialBodyTemplate));
		extraTypes.Add(typeof(CollectableType));
		extraTypes.Add(typeof(SPDAEntryType));
		extraTypes.Add(typeof(TutorialType));
		extraTypes.Add(typeof(Localization.RecordThing));
		extraTypes.Add(typeof(ActionData));
		extraTypes.Add(typeof(InteractionAction));
		extraTypes.Add(typeof(MovePlayerAction));
		extraTypes.Add(typeof(GeneAction));
		extraTypes.Add(typeof(GasAction));
		extraTypes.Add(typeof(QuantityAction));
		extraTypes.Add(typeof(PercentAction));
		extraTypes.Add(typeof(ChargeAction));
		extraTypes.Add(typeof(ConditionData));
		extraTypes.Add(typeof(GasCondition));
		extraTypes.Add(typeof(ConditionDataCollection));
		extraTypes.Add(typeof(QuantityCondition));
		extraTypes.Add(typeof(DecayCondition));
		extraTypes.Add(typeof(PercentCondition));
		extraTypes.Add(typeof(EnergyCondition));
		extraTypes.Add(typeof(DifficultyCondition));
		extraTypes.Add(typeof(SpeciesCondition));
		extraTypes.Add(typeof(ChildItemPrefabCondition));
		extraTypes.Add(typeof(MoleCondition));
		extraTypes.Add(typeof(TemperatureRangeCondition));
		extraTypes.Add(typeof(TransactionData));
		extraTypes.Add(typeof(DynamicSpawnData));
		extraTypes.Add(typeof(SpawnData));
		extraTypes.Add(typeof(BuyData));
		extraTypes.Add(typeof(SellData));
		extraTypes.Add(typeof(IntRangeData));
		extraTypes.Add(typeof(StockData));
		extraTypes.Add(typeof(RandomPoolData));
		extraTypes.Add(typeof(TraderData));
		extraTypes.Add(typeof(StringReference));
		extraTypes.Add(typeof(LocalizedStringReference));
		extraTypes.Add(typeof(ColorSwatchReference));
		extraTypes.Add(typeof(ReagentAction));
		extraTypes.Add(typeof(WorldCollection));
		extraTypes.Add(typeof(ContactSlotData));
		extraTypes.Add(typeof(BuyItem));
		extraTypes.Add(typeof(SellItem));
		extraTypes.Add(typeof(ContactSlotData.Select));
		extraTypes.Add(typeof(ContactSlotData.PlaneCondition));
		extraTypes.Add(typeof(ContactSlotData.EnvironmentCondition));
		extraTypes.Add(typeof(ContactSlotData.ConditionCollection));
		extraTypes.Add(typeof(ContactSlotData.BulkMultiplier));
		extraTypes.Add(typeof(TraderCrashEventData));
		extraTypes.Add(typeof(EvaporationChamberSaveData));
		extraTypes.Add(typeof(SpaceMapNodeData));
		extraTypes.Add(typeof(NodeIcon));
		extraTypes.Add(typeof(SpaceMapData));
		extraTypes.Add(typeof(SpawnContentsData));
		extraTypes.Add(typeof(RocketSaveData));
		extraTypes.Add(typeof(SpaceMapSaveData));
		extraTypes.Add(typeof(ResultData));
		extraTypes.Add(typeof(ResultSaveData));
		extraTypes.Add(typeof(GatherSurveyInfoResultData));
		extraTypes.Add(typeof(GatherSurveyInfoResultSaveData));
	}

	public override void ManagerStart()
	{
		base.ManagerStart();
		List<Type> extraTypes = new List<Type>();
		foreach (Thing sourcePrefab in WorldManager.Instance.SourcePrefabs)
		{
			if (!(sourcePrefab == null))
			{
				ThingSaveData thingSaveData = sourcePrefab.SerializeSave();
				Type type = thingSaveData.GetType();
				if (!extraTypes.Contains(thingSaveData.GetType()))
				{
					extraTypes.Add(type);
				}
				object modXmlType = sourcePrefab.GetModXmlType();
				Type type2 = modXmlType.GetType();
				if (!extraTypes.Contains(modXmlType.GetType()))
				{
					extraTypes.Add(type2);
				}
			}
		}
		AddExtraTypes(ref extraTypes);
		SyncedReferencableTypes.AddSaveDataTypes(extraTypes);
		extraTypes.Add(typeof(JetPackModData));
		ExtraTypes = extraTypes.ToArray();
		PanelSave.SetActive(value: true);
		if (!GameManager.IsBatchMode)
		{
			ImGuiLoadingScreen.SetActive(active: true);
		}
		PanelSave.SetActive(value: false);
		Singleton<MonoBehaviourListener>.Instance.SetType(MonoBehaviourType.XmlSaveLoad);
	}

	public void ShowSavePanel()
	{
		PanelSave.SetActive(value: true);
	}

	public void ButtonSave()
	{
		SaveTask().Forget();
		ActionInSave = SaveAction.BackToMainMenu;
	}

	private async UniTaskVoid SaveTask()
	{
		SaveResult saveResult = await SaveHelper.Save(CurrentStationName, default(CancellationToken));
		if (!saveResult.Success)
		{
			ConsoleWindow.PrintError(saveResult.Message, suppressStacktrace: true);
		}
	}

	public async UniTask PublishSaveToWorkshopTask(string title, string path, ulong workshopId)
	{
		if (workshopId != 0L && !(await SteamTransport.Workshop_ItemExists(workshopId)))
		{
			workshopId = 0uL;
		}
		SteamTransport.WorkShopItemDetail itemDetail = new SteamTransport.WorkShopItemDetail
		{
			Title = title,
			Path = path,
			PreviewPath = "",
			Description = "World save",
			PublishedFileId = workshopId,
			Type = SteamTransport.WorkshopType.World
		};
		try
		{
			ProgressPanel.ShowProgressBar();
			ProgressPanel.ShowProgressSuccessOrFailure(await PublishWorkshopItem(itemDetail));
		}
		catch (Exception)
		{
			ProgressPanel.ShowProgressSuccessOrFailure(success: false);
		}
	}

	private async UniTask<bool> PublishWorkshopItem(SteamTransport.WorkShopItemDetail itemDetail)
	{
		ProgressPanel.ShowProgressBar();
		var (success, publishedFileId, _) = await SteamTransport.Workshop_PublishItemAsync(itemDetail);
		ProgressPanel.ShowProgressSuccessOrFailure(success);
		if (success)
		{
			itemDetail.PublishedFileId = publishedFileId;
			await SaveWorkShopFileHandle(itemDetail);
		}
		return success;
	}

	private async UniTask SaveWorkShopFileHandle(SteamTransport.WorkShopItemDetail itemDetail)
	{
		Dictionary<string, byte[]> entryData = new Dictionary<string, byte[]>();
		await using (ZipInputStream zipInputStream = new ZipInputStream(File.OpenRead(itemDetail.Path)))
		{
			while (true)
			{
				ZipEntry entry = zipInputStream.GetNextEntry();
				if (entry == null)
				{
					break;
				}
				using MemoryStream ms = new MemoryStream();
				await zipInputStream.CopyToAsync(ms);
				entryData[entry.Name] = ms.ToArray();
			}
			using MemoryStream stream = new MemoryStream(entryData[SaveLoadConstants.MetaFileName]);
			WorldMetaData worldMetaData = (WorldMetaData)Serializers.WorldMetaData.Deserialize(stream);
			worldMetaData.WorkShopFileHandle = itemDetail.PublishedFileId;
			MemoryStream memoryStream = new MemoryStream();
			Serializers.WorldMetaData.Serialize(memoryStream, worldMetaData);
			entryData[SaveLoadConstants.MetaFileName] = memoryStream.ToArray();
		}
		MemoryStream zipMs = new MemoryStream();
		await using (ZipOutputStream zipOutputStream = new ZipOutputStream(zipMs)
		{
			IsStreamOwner = false
		})
		{
			zipOutputStream.SetLevel(9);
			foreach (KeyValuePair<string, byte[]> item in entryData)
			{
				item.Deconstruct(out var key, out var value);
				string text = key;
				byte[] array = value;
				ZipEntry entry2 = new ZipEntry(text);
				zipOutputStream.PutNextEntry(entry2);
				zipOutputStream.Write(array, 0, array.Length);
				zipOutputStream.CloseEntry();
			}
			zipOutputStream.Finish();
		}
		await using FileStream destinationStream = new FileStream(itemDetail.Path, FileMode.Truncate, FileAccess.Write);
		zipMs.Position = 0L;
		await zipMs.CopyToAsync(destinationStream);
	}

	public static void ClearAll()
	{
		Instance.CurrentWorldSave = null;
		Instance.CurrentStationName = null;
		ResetTerrain = false;
	}

	private void DisablePanel(GameObject panel, bool isForced)
	{
		if ((bool)panel)
		{
			if (isForced)
			{
				panel.SetActive(value: false);
			}
			else
			{
				panel.SetActive(value: false);
			}
			PanelMainMenu.SetActive(value: true);
			PanelMainMenu.transform.GetChild(1).gameObject.SetActive(value: true);
		}
	}

	public void ButtonCancel()
	{
		DisablePanel(PanelSave, isForced: false);
		DisablePanel(PanelJoinWorld, isForced: false);
		DisablePanel(PanelSettings, isForced: false);
		DisablePanel(PanelNewWorld, isForced: false);
		DisablePanel(NewPanelNewWorld, isForced: false);
	}

	public static void QuickSaveCurrentWorld()
	{
		QuickSaveTask().Forget();
	}

	private static async UniTaskVoid QuickSaveTask()
	{
		SaveResult saveResult = await SaveHelper.QuickSave(Instance.CurrentStationName, default(CancellationToken));
		if (!saveResult.Success)
		{
			ConsoleWindow.PrintError(saveResult.Message);
		}
	}

	public bool CanExitWorld()
	{
		if (!SavingWorld)
		{
			return !_copyingWorld;
		}
		return false;
	}

	public static WorldData GetWorldData()
	{
		WorldData worldData = new WorldData
		{
			Id = World.CurrentId
		};
		OrbitalSimulation.SerializeSave(worldData);
		worldData.WorldSetting = new SerializedId(WorldSetting.Current);
		worldData.DifficultySetting = new SerializedId(DifficultySetting.Current);
		worldData.StartCondition = new SerializedId(WorldSetting.Current.StartConditionData.Id);
		worldData.OrderedThings = new List<ThingSaveData>(OcclusionManager.AllThings.ActiveCount);
		worldData.TerraFormingSaveData = TerraForming.Serialize();
		if (WorldSetting.Current.StartLocationData != null)
		{
			worldData.StartLocation = new SerializedId(WorldSetting.Current.StartLocationData.Id);
		}
		foreach (KeyValuePair<ulong, SerializedClientInfo> item3 in GameManager.ClientInfo)
		{
			worldData.ClientInfos.Add(item3.Value);
		}
		DensePool<Thing>.ActiveEnumerable.Enumerator enumerator2 = OcclusionManager.AllThings.Active().GetEnumerator();
		while (enumerator2.MoveNext())
		{
			AddToSave(enumerator2.Current, worldData, null);
		}
		SyncedReferencableManager.AddToWorldData(worldData);
		worldData.Rooms = Room.SerializeSave();
		worldData.DaysPast = WorldManager.DaysPast;
		worldData.PipeNetworks = PipeNetwork.AllNetworkIds;
		worldData.CableNetworks = CableNetwork.GetAllNetworkIds();
		worldData.ChuteNetworks = ChuteNetwork.AllNetworkIds;
		worldData.LandingPadNetworks = LandingPadNetwork.AllNetworkIds;
		worldData.RocketNetworks = RocketNetwork.AllNetworkIds;
		worldData.RoboticArmNetworks = RoboticArmNetwork.AllNetworkIds;
		worldData.SpaceMap = SpaceMap.Current.SerializeSave();
		worldData.RocketSaveDatas = Rocket.GetRocketSaveDatas;
		worldData.RocketNameHistory = Rocket.RocketNameHistory;
		worldData.PendingSpawnActions = SpawnDataHelper.SerializeSave();
		worldData.Objectives = WorldObjectiveState.Save();
		worldData.TerrainChunkChecksums = VoxelTerrain.ReadOnlyOctree.GetTerrainChecksums();
		_temporaryWorldData = worldData;
		AtmosphericsManager.AllAtmospheres.ForEach(AtmosphereSaveAction);
		worldData.PlanetaryAtmosphere = PlanetaryAtmosphereSimulation.Save();
		foreach (TraderContact allStationContact in TraderContact.AllStationContacts)
		{
			StationContactData item = new StationContactData
			{
				ContactName = allStationContact.ContactName,
				WattsToResolve = allStationContact.WattsToResolve,
				MinimumWattsToResolve = allStationContact.MinimumWattsToResolve,
				MinimumWattsToContact = allStationContact.MinimumWattsToContact,
				SecondsRequiredToContact = allStationContact.SecondsRequiredToContact,
				Contacted = allStationContact.Contacted,
				Angle = allStationContact.Angle,
				ReferenceId = allStationContact.ReferenceId,
				Lifetime = allStationContact.Lifetime - (Time.time - allStationContact.InitialLifeTime),
				InstanceSaveData = allStationContact.DataInstance.SerializeInstanceData(),
				ContactSlotId = allStationContact.ContactSlot.IdHash,
				ShuttleType = allStationContact.ShuttleType,
				RequiredPadEnvironment = allStationContact.RequiredPadEnvironment
			};
			worldData.StationContacts.Add(item);
		}
		foreach (ContactSlot contactSlot in ContactSlot.ContactSlots)
		{
			ContactSlotSaveData item2 = new ContactSlotSaveData
			{
				IdHash = contactSlot.IdHash,
				CurrentContactReferenceId = (contactSlot.CurrentContact?.ReferenceId ?? 0),
				CurrentDownTime = contactSlot.CurrentDownTime,
				DownTimeRemaining = ((contactSlot.CurrentContact == null) ? (contactSlot.LastExpirationTime + contactSlot.CurrentDownTime - GameManager.GameTime) : 0f)
			};
			worldData.ContactSlotSaveDatas.Add(item2);
		}
		worldData.WeatherManagerData = WeatherManager.Instance.CreateSaveData();
		if (!GameManager.IsBatchMode)
		{
			worldData.UserInterface = InventoryWindowManager.Instance.GenerateUISaveData();
		}
		worldData.WorldEventData = WorldLog.GetSaveData();
		return worldData;
	}

	private static void AddToSave(Thing thing, WorldData worldData, Thing parent)
	{
		if ((object)thing == null || thing.IsBeingDestroyed || thing.IgnoreSave || !thing.WillSave || ((object)parent == null && thing is DynamicThing { ParentSlot: not null }) || ((object)parent != null && thing is DynamicThing dynamicThing2 && dynamicThing2.ParentSlot?.Parent != parent))
		{
			return;
		}
		worldData.OrderedThings.Add(thing.SerializeSave());
		foreach (Slot slot in thing.Slots)
		{
			if (!slot.IsEmpty())
			{
				AddToSave(slot.Get(), worldData, thing);
			}
		}
	}

	public static async UniTask UpdateLoadingScreen(string message, float value, string type = "Loading")
	{
		if (!GameManager.IsBatchMode)
		{
			ImGuiLoadingScreen.SetActive(active: true);
			await ImGuiLoadingScreen.SetState(message, resetProgress: false);
			await ImGuiLoadingScreen.SetProgress(value);
		}
	}

	public static void UpdateLoadingScreen(bool display)
	{
		ImGuiLoadingScreen.SetActive(display);
	}

	public static int GetRevisionNumber(string gameVersion)
	{
		if (string.IsNullOrEmpty(gameVersion))
		{
			return 0;
		}
		return int.Parse(gameVersion.Split('.')[3]);
	}

	public static T Load<T>(ThingSaveData thingData, bool generatesTerrain = true) where T : Thing
	{
		T val = Prefab.Find<T>(thingData.PrefabName);
		if (val == null)
		{
			UnityEngine.Debug.LogWarning("Can't spawn " + thingData.PrefabName);
			return null;
		}
		if (!thingData.IsValidData())
		{
			return null;
		}
		Vector3 worldPosition = thingData.WorldPosition;
		Quaternion worldRotation = thingData.WorldRotation;
		if (thingData is StructureSaveData structureSaveData)
		{
			worldPosition = structureSaveData.RegisteredWorldPosition;
			worldRotation = structureSaveData.RegisteredWorldRotation;
			RocketRecordData rocketRecord = structureSaveData.RocketRecord;
			if (rocketRecord != null && rocketRecord.RocketNetworkId != 0L)
			{
				EngineFuselage engineFuselage = Referencable.Find<RocketNetwork>(rocketRecord.RocketNetworkId)?.Anchor;
				if ((object)engineFuselage != null)
				{
					worldPosition = engineFuselage.ThingTransformPosition + rocketRecord.Offset;
				}
			}
		}
		T val2 = Thing.Create<T>(val, worldPosition, worldRotation, thingData.ReferenceId);
		if (!val2)
		{
			return null;
		}
		val2.generateTerrain = generatesTerrain;
		val2.DeserializeSave(thingData);
		val2.ValidateOnLoad(CurrentSaveRevision);
		return val2;
	}

	public static Thing Load(ThingSaveData thingData, bool generatesTerrain = true)
	{
		Thing thing = Prefab.Find(thingData.PrefabName);
		if (thing == null)
		{
			UnityEngine.Debug.LogWarning("Can't spawn " + thingData.PrefabName);
			return null;
		}
		if (!thingData.IsValidData())
		{
			return null;
		}
		Vector3 worldPosition = thingData.WorldPosition;
		Quaternion worldRotation = thingData.WorldRotation;
		if (thingData is StructureSaveData structureSaveData)
		{
			worldPosition = structureSaveData.RegisteredWorldPosition;
			worldRotation = structureSaveData.RegisteredWorldRotation;
			RocketRecordData rocketRecord = structureSaveData.RocketRecord;
			if (rocketRecord != null && rocketRecord.RocketNetworkId != 0L)
			{
				EngineFuselage engineFuselage = Referencable.Find<RocketNetwork>(rocketRecord.RocketNetworkId)?.Anchor;
				if ((object)engineFuselage != null)
				{
					worldPosition = engineFuselage.ThingTransformPosition + rocketRecord.Offset;
				}
			}
		}
		Thing thing2 = Thing.Create<Thing>(thing, worldPosition, worldRotation, thingData.ReferenceId);
		if (!thing2)
		{
			return null;
		}
		thing2.generateTerrain = generatesTerrain;
		thing2.DeserializeSave(thingData);
		thing2.ValidateOnLoad(CurrentSaveRevision);
		return thing2;
	}

	private static void FlagTerrainReset()
	{
		ResetTerrain = true;
	}

	public static async UniTask LoadWorld(bool loadWithoutChars = false)
	{
		WorldManager.SetGamePause(pauseGame: true);
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenInitializing.DisplayString);
		FadePanel.ToBlack(0.5f);
		await UniTask.Delay(500, DelayType.UnscaledDeltaTime);
		Stopwatch overallLoadTime = new Stopwatch();
		overallLoadTime.Start();
		await UniTask.DelayFrame(2);
		GameManager.GameState = GameState.Loading;
		ContactManager.Instance.StartCheckContactLifeTime();
		Stopwatch loadTimer = Stopwatch.StartNew();
		string loadTimeLog = "";
		TimeSpan splitTime = loadTimer.Elapsed;
		string fullName = Instance.CurrentWorldSave.World.FullName;
		object obj = XmlSerialization.Deserialize(Serializers.WorldData, fullName);
		if (!(obj is WorldData worldData))
		{
			UpdateLoadingScreen(display: false);
			throw new NullReferenceException("Failed to load the world.xml: " + fullName);
		}
		World.CurrentId = worldData.Id;
		WorldManager.Seed = worldData.WorldSeed;
		loadTimeLog = loadTimeLog + "Loaded World Settings.  Elapsed: " + loadTimer.Elapsed.ToCompactString() + " ( + " + (loadTimer.Elapsed - splitTime).ToCompactString() + ") \n";
		splitTime = loadTimer.Elapsed;
		GridController.InitializeWorldController();
		CurrentSaveRevision = GetRevisionNumber(worldData.GameVersion);
		WorldSetting worldSetting = WorldSetting.Find(worldData.WorldSetting);
		if (worldSetting == null)
		{
			throw new NullReferenceException("error unable to load as world setting for '" + worldData.WorldSetting.Id + "' is not found");
		}
		StartConditionData startConditionData;
		if (worldData.StartCondition != null)
		{
			startConditionData = DataCollection.Get<StartConditionData>(worldData.StartCondition);
			if (startConditionData == null)
			{
				startConditionData = StartConditionData.GetFallBack(worldData.Id);
				ConsoleWindow.Print("Applying Fallback Start Condition: " + startConditionData.Id);
			}
		}
		else
		{
			startConditionData = StartConditionData.GetFallBack(worldData.Id);
			ConsoleWindow.Print("Applying Fallback Start Condition: " + startConditionData.Id);
		}
		StartLocationData startLocationData = ((worldData.StartLocation == null) ? DataCollection.Get<StartLocationData>("DefaultStartLocation") : DataCollection.Get<StartLocationData>(worldData.StartLocation));
		WorldSetting.SetCurrent(worldSetting, startConditionData, startLocationData);
		DifficultySetting.SetCurrent(worldData.DifficultySetting);
		if (WorldSetting.Current.Data.TerrainSettings == null)
		{
			throw new NullReferenceException("No terrainSettings data found");
		}
		await VoxelTerrain.LoadTerrain(WorldSetting.Current.Data.TerrainSettings, newGame: false);
		RegionManager.LoadRegionSets(WorldSetting.Current.Data);
		if (!VoxelTerrain.IsChecksumValid(worldData.TerrainChunkChecksums))
		{
			await UniTask.NextFrame();
			await PromptPanel.Instance.AwaitShowPrompt(GameStrings.StartGameFailurePromptTitle, GameStrings.SaveInvalidTerrainDataModified, GameStrings.ResetTerrainButton, FlagTerrainReset, GameStrings.CancelLoadButton, GameManager.LeaveGame, isEscapable: false);
			if (!ResetTerrain)
			{
				return;
			}
		}
		string text = Instance.CurrentWorldSave.TerrainData?.FullName;
		bool flag = false;
		if (!ResetTerrain && (!string.IsNullOrEmpty(text) || File.Exists(text)))
		{
			if (!VoxelTerrain.Deserialize(text))
			{
				flag = true;
			}
		}
		else
		{
			flag = true;
		}
		if (flag)
		{
			VoxelTerrain.Octree.PrepareOctree();
		}
		foreach (SerializedClientInfo clientInfo in worldData.ClientInfos)
		{
			GameManager.AddClientInfo(clientInfo.ClientId, clientInfo);
		}
		WorldObjectiveState.Load(worldData);
		TerraForming.Deserialize(worldData);
		Referencable.FindAndSetNextReferenceId(worldData);
		WorldManager.Instance.InitializeWorldEnvironment();
		if (GameManager.IsNewTutorial)
		{
			DifficultySetting.SetCurrent(DifficultySetting.Find("Normal"));
		}
		WorldManager.DaysPast = worldData.DaysPast;
		LoadInNetworks(worldData);
		int num = worldData.PipeNetworks.Length + worldData.CableNetworks.Length + worldData.ChuteNetworks.Length;
		loadTimeLog += string.Format("Loaded {0} Networks in {2} (Elapsed: {1})\n", num, loadTimer.Elapsed.ToCompactString(), (loadTimer.Elapsed - splitTime).ToCompactString());
		splitTime = loadTimer.Elapsed;
		List<Room> rooms = Room.DeserializeSave(worldData.Rooms);
		SpaceMap.LoadGameSave(worldSetting, worldData.SpaceMap);
		for (int i = 0; i < worldData.RocketSaveDatas.Count; i++)
		{
			RocketSaveData rocketSaveData = worldData.RocketSaveDatas[i];
			if (rocketSaveData != null)
			{
				Rocket.LoadRocket(rocketSaveData);
			}
		}
		Rocket.RocketNameHistory = worldData.RocketNameHistory;
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenLoadingRooms.DisplayString);
		for (int j = 0; j < rooms.Count; j++)
		{
			await ImGuiLoadingScreen.SetProgress((float)j / (float)rooms.Count);
			Room room = rooms[j];
			if (room.Grids.Count > 0 && RoomController.World.GetRoom(room.Grids[0]) == null)
			{
				RoomController.World.Register(room, 0L);
			}
		}
		RoomController.RegenStormCurtain = true;
		loadTimeLog += string.Format("Loaded {0} Rooms in {2} (Elapsed: {1})\n", rooms.Count, loadTimer.Elapsed.ToCompactString(), (loadTimer.Elapsed - splitTime).ToCompactString());
		splitTime = loadTimer.Elapsed;
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenLoadingThings.AsString(worldData.GetThings().Count.ToString()));
		List<long> purgedThings = new List<long>();
		long foundChar = 0L;
		Stopwatch timer = new Stopwatch();
		timer.Start();
		for (int j = 0; j < worldData.GetThings().Count; j++)
		{
			if (j % 500 == 0 && timer.ElapsedMilliseconds > 250)
			{
				ImGuiLoadingScreen.Progress = (float)j / (float)worldData.GetThings().Count;
				await UniTask.NextFrame();
				timer.Reset();
				timer.Start();
			}
			ThingSaveData thingSaveData = worldData.GetThing(j);
			if (loadWithoutChars)
			{
				if (thingSaveData is BrainSaveData brainSaveData)
				{
					if (foundChar != 0L)
					{
						purgedThings.Add(brainSaveData.ReferenceId);
						continue;
					}
					foundChar = brainSaveData.ParentReferenceId;
					brainSaveData.ClientSteamId = NetworkManager.LocalClientId;
					thingSaveData = brainSaveData;
				}
				if (thingSaveData is HumanSaveData && foundChar != 0L && foundChar != thingSaveData.ReferenceId)
				{
					purgedThings.Add(thingSaveData.ReferenceId);
					continue;
				}
				if (thingSaveData is DynamicThingSaveData { ParentReferenceId: not 0L } dynamicThingSaveData && purgedThings.Contains(dynamicThingSaveData.ParentReferenceId))
				{
					purgedThings.Add(thingSaveData.ReferenceId);
					continue;
				}
			}
			_ = Load(thingSaveData) == null;
		}
		timer.Stop();
		loadTimeLog += $"Loaded {worldData.GetThings().Count} Things in {(loadTimer.Elapsed - splitTime).ToCompactString()} (Elapsed: {loadTimer.Elapsed.ToCompactString()})\n";
		splitTime = loadTimer.Elapsed;
		foreach (SyncedReferencableSaveData syncedReferencable in worldData.SyncedReferencables)
		{
			SyncedReferencableManager.Load(syncedReferencable);
		}
		List<int> list = PlayerCookie.Current?.GetDiscoveredPois();
		if (list != null && list.Count > 0)
		{
			PointOfInterestManager.Load(list);
		}
		PlanetaryAtmosphereSimulation.Load(worldData.PlanetaryAtmosphere, CurrentSaveRevision);
		for (int k = 0; k < worldData.Atmospheres.Count; k++)
		{
			AtmosphereSaveData atmosphereSaveData = worldData.Atmospheres[k];
			Atmosphere atmosphere = null;
			if (atmosphereSaveData.Volume <= 0.0)
			{
				continue;
			}
			if (atmosphereSaveData.NetworkReferenceId > 0)
			{
				long networkReferenceId = atmosphereSaveData.NetworkReferenceId;
				AtmosphericsNetwork atmosphericsNetwork = Referencable.Find<AtmosphericsNetwork>(networkReferenceId) ?? new PipeNetwork(networkReferenceId);
				if (atmosphericsNetwork != null)
				{
					atmosphere = new Atmosphere(atmosphericsNetwork, atmosphereSaveData.ReferenceId);
					atmosphericsNetwork.AssignAtmosphere(atmosphere);
					atmosphere.Mode = AtmosphereHelper.AtmosphereMode.Network;
					atmosphere.AtmosphericsNetwork = atmosphericsNetwork;
				}
			}
			else if (atmosphereSaveData.ThingReferenceId > 0)
			{
				Thing thing = Referencable.Find<Thing>(atmosphereSaveData.ThingReferenceId);
				if (!thing)
				{
					continue;
				}
				thing.AssignInternalAtmosphereOnLoad(new VolumeLitres(atmosphereSaveData.Volume), atmosphereSaveData.ReferenceId);
				atmosphere = thing.InternalAtmosphere;
				if (atmosphere == null)
				{
					UnityEngine.Debug.LogWarning("Unable to load atmosphere for " + thing.DisplayName, thing);
					continue;
				}
			}
			else
			{
				atmosphere = GridController.World.AtmosphericsController.CloneGlobalAtmosphere(new WorldGrid(atmosphereSaveData.Position), atmosphereSaveData.ReferenceId);
			}
			atmosphere?.Load(atmosphereSaveData);
		}
		AtmosphericsManager.HandleMainThreadRegistrations();
		loadTimeLog += $"Loaded {worldData.Atmospheres.Count} Atmospheres in {(loadTimer.Elapsed - splitTime).ToCompactString()} (Elapsed: {loadTimer.Elapsed.ToCompactString()})\n";
		_ = loadTimer.Elapsed;
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenInitializingDevices.DisplayString);
		Device.AllDevices.ForEach(Device.InitializeDeviceAction);
		ContactSlot.LoadContactSlots(worldData.ContactSlotSaveDatas);
		TraderContact.LoadStationContacts(worldData.StationContacts);
		TraderContact.InitializeAll();
		_ = loadTimer.Elapsed;
		if (WrongReference.Count > 0)
		{
			foreach (KeyValuePair<long, Thing> item in WrongReference)
			{
				Referencable.RegisterNew(item.Value);
			}
			WrongReference.Clear();
		}
		for (int num2 = StructureNetwork.AllStructureNetworks.Count - 1; num2 >= 0; num2--)
		{
			StructureNetwork structureNetwork = StructureNetwork.AllStructureNetworks[num2];
			if (structureNetwork == null)
			{
				StructureNetwork.AllStructureNetworks.RemoveAt(num2);
			}
			else
			{
				structureNetwork.ValidateOnLoad(CurrentSaveRevision);
			}
		}
		RocketParkSlot.Initialise();
		AtmosphericsManager.CleanUpInvalidAtmospheres();
		splitTime = loadTimer.Elapsed;
		WeatherManager.Instance.LoadSaveData(worldData.WeatherManagerData);
		loadTimeLog = loadTimeLog + "Loaded TileSystem Data in " + (loadTimer.Elapsed - splitTime).ToCompactString() + " (Elapsed: " + loadTimer.Elapsed.ToCompactString() + ")\n";
		if (!GameManager.IsBatchMode)
		{
			InventoryWindowManager.WorldXmlUISaveData = worldData.UserInterface;
		}
		SpawnDataHelper.LoadSave(worldData);
		Referencable.AssignNewIdToDuplicates();
		await UniTask.NextFrame();
		GameManager.UnloadAssetsAndCollectGarbage();
		ConsoleWindow.Print($"World Loaded in {loadTimer.Elapsed.Minutes}:{loadTimer.Elapsed.Seconds}");
		char[] separator = new char[2] { '\r', '\n' };
		string[] array = loadTimeLog.Split(separator, StringSplitOptions.RemoveEmptyEntries);
		for (int l = 0; l < array.Length; l++)
		{
			ConsoleWindow.Print(array[l]);
		}
		loadTimer.Reset();
		if (DynamicThing.AwaitingMoveWhenReady.Count > 0)
		{
			ConsoleWindow.Print("Waiting for ophaned items");
		}
		while (DynamicThing.AwaitingMoveWhenReady.Count > 0)
		{
			await UniTask.NextFrame();
		}
		if (GameManager.IsBatchMode)
		{
			await LodManager.InitialiseLodsOnLoad();
		}
		else if ((bool)World.HandlePlayerControl())
		{
			await LodManager.InitialiseLodsOnLoad();
		}
		else
		{
			LodManager.EnqueueRequesterToUpdate(World.CreateCharacterAndTakeControl());
			await LodManager.InitialiseLodsOnLoad();
		}
		await VoxelTerrain.InitialiseMinablesOnLoad();
		InventoryManager.ParentHuman?.SetPhysicsOnControl();
		GameManager.DeleteOutOfBoundsObjects();
		GameManager.OnReadyToPlay();
		WorldManager.Instance?.ResumePlay();
		World.OnLoadingFinished(worldData);
		WorldLog.Load(worldData.WorldEventData);
		SharedDLCManager.HostFinishedLoad();
		loadTimer.Stop();
		ConsoleWindow.Print("loaded '" + worldData.WorldSetting.Id + "' " + StringManager.Get(worldData.GetThings().Count) + " things in " + StringManager.Get(overallLoadTime.ElapsedMilliseconds) + "ms");
	}

	private static void LoadInNetworks(WorldData worldData)
	{
		PipeNetworkIdToIReferencable.Clear();
		CableNetworkIdToIReferencable.Clear();
		ChuteNetworkIdToIReferencable.Clear();
		long[] pipeNetworks = worldData.PipeNetworks;
		for (int i = 0; i < pipeNetworks.Length; i++)
		{
			new PipeNetwork(pipeNetworks[i]);
		}
		pipeNetworks = worldData.CableNetworks;
		for (int i = 0; i < pipeNetworks.Length; i++)
		{
			new CableNetwork(pipeNetworks[i]);
		}
		pipeNetworks = worldData.ChuteNetworks;
		for (int i = 0; i < pipeNetworks.Length; i++)
		{
			new ChuteNetwork(pipeNetworks[i]);
		}
		if (worldData.LandingPadNetworks != null)
		{
			pipeNetworks = worldData.LandingPadNetworks;
			for (int i = 0; i < pipeNetworks.Length; i++)
			{
				new LandingPadNetwork(pipeNetworks[i]);
			}
		}
		if (worldData.RocketNetworks != null)
		{
			pipeNetworks = worldData.RocketNetworks;
			for (int i = 0; i < pipeNetworks.Length; i++)
			{
				new RocketNetwork(pipeNetworks[i]);
			}
		}
		if (worldData.RoboticArmNetworks != null)
		{
			pipeNetworks = worldData.RoboticArmNetworks;
			for (int i = 0; i < pipeNetworks.Length; i++)
			{
				new RoboticArmNetwork(pipeNetworks[i]);
			}
		}
	}
}
