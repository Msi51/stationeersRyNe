using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.FirstPerson;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.UI.Motherboard;
using Assets.Scripts.Util;
using ch.sycoforge.Flares;
using CharacterCustomisation;
using ColorBlindUtility.UGUI;
using Cysharp.Threading.Tasks;
using Genetics;
using Networking;
using Objects.Rockets;
using Reagents;
using Steamworks;
using TerrainSystem;
using ThingImport;
using TMPro;
using Trading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Weather;

[DefaultExecutionOrder(-1000)]
public class WorldManager : ManagerBase
{
	public class WorldLight
	{
		public Light TargetLight;

		public float Intensity;

		public Color Color;
	}

	[XmlRoot]
	public class ProcessingData
	{
		public string InputPrefab;

		public string OutputPrefab;

		public float Time;

		public int In;

		public int Out;
	}

	[XmlRoot]
	public class RecipeData
	{
		[XmlElement]
		public string PrefabName;

		[XmlElement]
		public Recipe Recipe;

		[XmlElement]
		public MachineTier RecipeTier;

		[XmlElement]
		public float Output = 1f;
	}

	[XmlRoot]
	public class ReagentTradeData
	{
		[XmlElement]
		public string Reagent;

		[XmlElement]
		public float Value;
	}

	[XmlRoot]
	public class GasTradeData
	{
		[XmlElement]
		public string GasName;

		[XmlElement]
		public float TradeValue;
	}

	[XmlRoot]
	public class RocketName
	{
		[XmlAttribute]
		public string Name;
	}

	[XmlRoot]
	public class ItemReplacement
	{
		[XmlAttribute]
		public string Substitute;

		[XmlAttribute]
		public string Replaces;
	}

	[XmlRoot]
	public class GameData
	{
		public List<RecipeData> CentrifugeRecipes = new List<RecipeData>();

		public List<RecipeData> FurnaceRecipes = new List<RecipeData>();

		public List<RecipeData> AdvancedFurnaceRecipes = new List<RecipeData>();

		public List<RecipeData> ArcFurnaceRecipes = new List<RecipeData>();

		public List<RecipeData> MicrowaveRecipes = new List<RecipeData>();

		public List<RecipeData> PackagingMachineRecipes = new List<RecipeData>();

		public List<RecipeData> AutolatheRecipes = new List<RecipeData>();

		public List<RecipeData> AutomatedOvenRecipes = new List<RecipeData>();

		public List<RecipeData> ElectronicsPrinterRecipes = new List<RecipeData>();

		public List<RecipeData> SecurityPrinterRecipes = new List<RecipeData>();

		public List<RecipeData> RocketManufactoryRecipes = new List<RecipeData>();

		public List<RecipeData> HydraulicPipeBenderRecipes = new List<RecipeData>();

		public List<RecipeData> ToolManufactoryRecipes = new List<RecipeData>();

		public List<RecipeData> ChemistryRecipes = new List<RecipeData>();

		public List<RecipeData> IngotRecipes = new List<RecipeData>();

		public List<RecipeData> RecycleRecipes = new List<RecipeData>();

		public List<RecipeData> TerraformingManufactoryRecipes = new List<RecipeData>();

		[XmlElement("MinableVisualiser")]
		public List<MinableVisualiserData> MinableVisualisers = new List<MinableVisualiserData>();

		public List<ProcessingData> ReagentGrinderRecipes = new List<ProcessingData>();

		[XmlArray("WorldSettings")]
		[XmlArrayItem("World", Type = typeof(WorldSettingData))]
		public List<WorldSettingData> WorldSettings = new List<WorldSettingData>();

		[XmlElement("Trader")]
		public List<TraderData> TraderDatas = new List<TraderData>();

		[XmlElement("RoomTypeRule")]
		public List<RoomTypeRuleData> RoomTypeRuleData = new List<RoomTypeRuleData>();

		[XmlElement("ContactSlot")]
		public List<ContactSlotData> ContactSlots = new List<ContactSlotData>();

		[XmlElement("SpaceMap")]
		public List<SpaceMapData> SpaceMaps = new List<SpaceMapData>();

		[XmlArray("CelestialBodies")]
		[XmlArrayItem("CelestialBody")]
		public List<CelestialBodyTemplate> CelestialBodies = new List<CelestialBodyTemplate>();

		[XmlElement("Icon")]
		public List<NodeIcon> NodeIcons = new List<NodeIcon>();

		[XmlElement("Spawn")]
		public List<SpawnData> SpawnDatas = new List<SpawnData>();

		[XmlElement("StartCondition")]
		public List<StartConditionData> StartConditions = new List<StartConditionData>();

		[XmlElement("StartLocation", typeof(StartLocationData))]
		[XmlElement("RandomStartLocation", typeof(RoundRobinStartLocationData))]
		public List<StartLocationData> StartLocations = new List<StartLocationData>();

		[XmlElement("WorldObjective")]
		public List<WorldObjectiveCollection> WorldObjectiveCollections = new List<WorldObjectiveCollection>();

		[XmlElement("Objective")]
		public List<WorldObjective> WorldObjectives = new List<WorldObjective>();

		[XmlElement("Buy")]
		public List<BuyData> BuyDatas = new List<BuyData>();

		[XmlElement("Sell")]
		public List<SellData> SellDatas = new List<SellData>();

		public List<RecipeData> PaintMixRecipes = new List<RecipeData>();

		public List<ThingModData> ThingMods = new List<ThingModData>();

		public List<DifficultySetting> DifficultySettings = new List<DifficultySetting>();

		public List<RocketName> DefaultRocketNames = new List<RocketName>();

		public List<ItemReplacement> ItemReplacements = new List<ItemReplacement>();

		[XmlElement("GHGIndex")]
		public List<TerraformingGasCurveData> TerraformingGasCurveDatas = new List<TerraformingGasCurveData>();

		[XmlElement("AtmosphericScatteringBlend")]
		public List<AtmosphericScatteringBlendData> AtmosphericScatteringBlendDatas = new List<AtmosphericScatteringBlendData>();

		[XmlElement("LifeRequirements")]
		public List<LifeRequirementsData> PlantLifeRequirementsData = new List<LifeRequirementsData>();

		[XmlElement("CustomPlant", typeof(CustomPlantData))]
		[XmlElement("CustomSeed", typeof(CustomSeedData))]
		public List<CustomThingData> CustomThingData = new List<CustomThingData>();

		[XmlElement("Blueprint")]
		public List<BlueprintData> BlueprintData = new List<BlueprintData>();

		[XmlElement("WeatherEvent")]
		public List<WeatherEvent> WeatherEvents = new List<WeatherEvent>();

		[XmlElement("ServerProviderData")]
		public List<ServerProvider> ServerProviders = new List<ServerProvider>();

		[XmlElement("Minables")]
		public List<MinablesGenerationData> Minables = new List<MinablesGenerationData>();

		[XmlElement("DeepMinables")]
		public List<DeepMinablesGenerationData> DeepMinables = new List<DeepMinablesGenerationData>();

		[XmlElement("OreVein")]
		public List<VeinGenerationData> OreVeins = new List<VeinGenerationData>();

		[XmlElement("Expression")]
		public List<PlayerCosmeticsBehaviour.FacialExpressionData> Expressions = new List<PlayerCosmeticsBehaviour.FacialExpressionData>();
	}

	[XmlRoot]
	public class DynamicThingData
	{
		[XmlIgnore]
		[XmlElement("Prefab")]
		public DynamicThing SourcePrefab;

		public string CustomName;

		public string PrefabName;

		public bool UseMasterColor;

		public string CustomColor;

		[XmlArray]
		public List<InventoryData> Contents = new List<InventoryData>();

		public bool PopulatePrefab()
		{
			SourcePrefab = Prefab.Find<DynamicThing>(PrefabName);
			if (SourcePrefab == null)
			{
				return false;
			}
			foreach (InventoryData content in Contents)
			{
				content.PopulatePrefab();
			}
			return true;
		}

		public virtual void Spawn()
		{
			Vector3 safePoint = SpawnPoint.GetSafePoint(UnityEngine.Random.insideUnitSphere * 8f);
			DynamicThing dynamicThing = OnServer.Create<DynamicThing>(SourcePrefab, safePoint, Quaternion.identity);
			dynamicThing.ThingTransform.Rotate(Vector3.up, UnityEngine.Random.Range(-180, 180));
			dynamicThing.HasRunOnAtmospherics = false;
			foreach (InventoryData content in Contents)
			{
				content.Spawn(dynamicThing, null);
			}
			if (!string.IsNullOrEmpty(CustomName))
			{
				if (Localization.InterfaceExists(CustomName))
				{
					OnServer.SetCustomName(dynamicThing, Localization.GetInterface(CustomName));
				}
				else
				{
					OnServer.SetCustomName(dynamicThing, CustomName);
					ConsoleWindow.Print("No matching Key was found for " + CustomName + " in english file. It is needed for translations");
				}
			}
			if (!UseMasterColor && !string.IsNullOrEmpty(CustomColor))
			{
				ColorSwatch colorSwatch = GameManager.GetColorSwatch(CustomColor);
				if (colorSwatch != null)
				{
					OnServer.SetCustomColor(dynamicThing, colorSwatch.Index);
				}
			}
			safePoint.y += 2f;
			OnServer.Create<RoadFlare>("ItemRoadFlare", safePoint, Quaternion.identity)?.WaitThenBurn().Forget();
		}
	}

	[XmlRoot]
	public class InventoryData : DynamicThingData
	{
		public int SlotId = -1;

		public Slot.Class Type;

		public float StackSize = float.NaN;

		public SpeciesClass SpeciesClass;

		public virtual void Spawn(Thing parentEntity, ColorSwatch colorSwatch)
		{
			Slot slot = null;
			Human human = parentEntity as Human;
			if (Type != Slot.Class.None)
			{
				for (int i = 0; i < parentEntity.Slots.Count; i++)
				{
					Slot slot2 = parentEntity.Slots[i];
					if (!(slot2.Occupant != null) && slot2.Type == SourcePrefab.SlotType)
					{
						slot = slot2;
						break;
					}
				}
			}
			if (slot == null && SlotId >= 0 && SlotId < parentEntity.Slots.Count)
			{
				slot = parentEntity.Slots[SlotId];
				if ((bool)slot.Occupant || (slot.Type != Slot.Class.None && slot.Type != SourcePrefab.SlotType))
				{
					slot = null;
				}
			}
			if (slot == null)
			{
				foreach (Slot slot3 in parentEntity.Slots)
				{
					if (!slot3.Occupant && (slot3.Type == Slot.Class.None || slot3.Type == SourcePrefab.SlotType))
					{
						slot = slot3;
						break;
					}
				}
			}
			if (slot == null)
			{
				throw new NullReferenceException("Invalid starting conditions! Unable to find a free slot for " + PrefabName + " in " + parentEntity.DisplayName);
			}
			DynamicThing dynamicThing = OnServer.Create<DynamicThing>(SourcePrefab, slot);
			if (!dynamicThing)
			{
				throw new NullReferenceException("thing " + PrefabName + " created null");
			}
			if (dynamicThing is BatteryCell batteryCell)
			{
				batteryCell.PowerStored = batteryCell.PowerMaximum;
			}
			if (dynamicThing is WaterBottle waterBottle)
			{
				waterBottle.Quantity = waterBottle.MaxQuantity;
			}
			if (dynamicThing is CreditCard creditCard && !float.IsNaN(StackSize))
			{
				creditCard.Currency = StackSize;
			}
			if (dynamicThing is IQuantity quantity && !float.IsNaN(StackSize))
			{
				quantity.SetQuantity(Mathf.Clamp(StackSize, 1f, quantity.GetMaxQuantity));
			}
			if (UseMasterColor && dynamicThing.PaintableMaterial != null && colorSwatch != null)
			{
				OnServer.SetCustomColor(dynamicThing, colorSwatch.Index);
			}
			else if (!UseMasterColor && !string.IsNullOrEmpty(CustomColor))
			{
				ColorSwatch colorSwatch2 = GameManager.GetColorSwatch(CustomColor);
				if (colorSwatch2 != null)
				{
					OnServer.SetCustomColor(dynamicThing, colorSwatch2.Index);
				}
			}
			if (!string.IsNullOrEmpty(CustomName))
			{
				if (Localization.InterfaceExists(CustomName))
				{
					OnServer.SetCustomName(dynamicThing, Localization.GetInterface(CustomName));
				}
				else
				{
					OnServer.SetCustomName(dynamicThing, CustomName);
					ConsoleWindow.Print("No matching Key was found for " + CustomName + " in english file. It is needed for translations");
				}
			}
			dynamicThing.HasRunOnAtmospherics = false;
			foreach (InventoryData content in Contents)
			{
				if (!(human != null) || content.SpeciesClass == SpeciesClass.None || content.SpeciesClass == human.SpeciesClass)
				{
					content.Spawn(dynamicThing, colorSwatch);
				}
			}
		}
	}

	public enum WorldType
	{
		Undefined = 0,
		Moon = 1,
		Mars = 2,
		Space = 3,
		Loulan = 4,
		Mimas = 5,
		Europa = 6,
		Vulcan = 7,
		Custom = -1,
		Max = 8,
		Venus = 9
	}

	public delegate void Event();

	public GameObject DriverWarningScreen;

	public ParticleSystem Sparker;

	public static WorldManager Instance;

	public static int Seed = 0;

	public static Vector3 WindVector = Vector3.zero;

	public WorldLight WorldSun = new WorldLight();

	public TextMeshProUGUI FrameRate;

	public Image Brightness;

	public Canvas GameCanvas;

	public Canvas MainMenuCanvas;

	public CanvasScaler PingCanvasScaler;

	public Assets.Scripts.UI.MainMenu MenuCanvas;

	public Image CastBarImage;

	public CelestialSprite CelestialSpritePrefab;

	public Digit DigitPrefab;

	public RockyBody RockyBodyPrefab;

	public AtmosphericBody AtmosphericBodyPrefab;

	public Tooltip Tooltip;

	public GameMode GameMode;

	private float deltaTime;

	public static float ShadowCullSize = 0.3f;

	private static uint _daysPast;

	private Transform _sparkerTransform;

	private ParticleSystem.MainModule _sparkerMainModule;

	public GameObject WorldDescriptionWindow;

	public List<Thing> SourcePrefabs = new List<Thing>();

	private static Dictionary<RecipeType, RecipeComparable> _recipeComparables = new Dictionary<RecipeType, RecipeComparable>();

	public Material lavaMaterial;

	public Material ClutterMaterial;

	public static Action OnGameDataLoaded;

	private const string ReloadTerrainTexturesFailed = "Failed to reload terrain textures";

	private static List<CustomThingData> _blueprintsToGenerate = new List<CustomThingData>();

	private bool _rateCounterShown;

	private static float TerrainGenerationTimeBudgetMs = 10f;

	private static float TerrainGenerationLoadingTimeBudgetMs = 10000f;

	public static Event OnWorldStarted;

	private const string MAIN_MENU_ATMOS_SCATTERING_WORLD = "Mars2";

	public static float EarthGravityRatio;

	public static Vector3 EarthGravityOffset;

	private readonly string _sunName = "Sun";

	public GameObject SunGameObject;

	[Space]
	[Header("Damage Filters")]
	public CameraFilterPack_Blur_Radial BlurRadial;

	public CameraFilterPack_Blur_Tilt_Shift BlurTiltShift;

	public CameraFilterPack_AAA_Blood_Hit BloodHit;

	public CameraFilterPack_TV_Vignetting Vignette;

	public CameraFilterPack_Light_Water LightWater;

	public CameraFilterPack_AAA_SuperComputer EletricShock;

	public CameraFilterPack_FX_Drunk2 Drunken;

	public CameraFilterPack_TV_BrokenGlass BrokenGlass;

	public CameraFilterPack_Color_BrightContrastSaturation BlackScreen;

	public static DamageType CurrentDamageType = DamageType.Blood;

	public static string CurrentWorldId => WorldSetting.Current?.Id ?? string.Empty;

	public static string CurrentWorldName
	{
		get
		{
			LocalizedStringReference localizedStringReference = WorldSetting.Current?.Name;
			if (localizedStringReference == null)
			{
				return string.Empty;
			}
			return localizedStringReference;
		}
	}

	public static float WorldGravity => WorldSetting.Current.Gravity;

	public static bool AtmosphericScattering => WorldSetting.Current?.AtmosphericScattering ?? false;

	public static bool HasLava
	{
		get
		{
			if (WorldSetting.Current.Data.TerrainSettings.LavaData == null)
			{
				return WorldSetting.Current.Data.TerrainSettings.TerrainProps?.LavaLakeList != null;
			}
			return true;
		}
	}

	public static bool HasGravity => WorldGravity < 0f;

	public static uint DaysPast
	{
		get
		{
			return _daysPast;
		}
		set
		{
			if (_daysPast == value)
			{
				return;
			}
			_daysPast = value;
			if (GameManager.IsRunning)
			{
				WeatherManager.OnNextDay();
				PlayerStateWindow.OnNextDay();
				foreach (IOnEachDay onNewDayThing in OcclusionManager.OnNewDayThings)
				{
					onNewDayThing.OnNewDay();
				}
			}
			if (NetworkManager.IsServer)
			{
				NetworkServer.OnNextDay();
			}
			if (GameManager.RunSimulation)
			{
				Achievements.AssessBrutalLegend();
				Achievements.AssessWhyIsItStillMarkWatney();
			}
		}
	}

	public static bool IsInitialized { get; private set; }

	public static bool IsGamePaused { get; private set; }

	public static event Action<bool> OnPaused;

	public static event Event OnHudScaleUpdate;

	public static bool IsCreative()
	{
		WorldManager instance = Instance;
		if ((object)instance != null)
		{
			return instance.GameMode == GameMode.Creative;
		}
		return false;
	}

	public static bool HasGravityAtHeight(float height)
	{
		if (WorldGravity < 0f)
		{
			return height < PlanetaryAtmosphereSimulation.SpaceHeight;
		}
		return false;
	}

	public static void PublishDaysPast()
	{
		if ((bool)InventoryManager.Parent)
		{
			ConsoleWindow.Print(GameStrings.DaysPassedPlayerMessage.AsString(_daysPast.ToString(), InventoryManager.Parent.DaysLived.ToString()), ConsoleColor.White, clearLine: false, aged: false);
		}
		if (SteamClient.IsValid)
		{
			GameManager.SetSteamRichPresence("day", StringManager.Get(DaysPast));
			if ((object)InventoryManager.Parent != null)
			{
				Achievements.AssessDaysLived(InventoryManager.Parent.DaysLived);
			}
		}
		Singleton<DiscordClient>.Instance?.UpdateActivityInGame();
	}

	public static void Spark(Vector3 position, int quantity, bool isGravity)
	{
		if (!GameManager.IsBatchMode)
		{
			Instance._sparkerTransform.position = position;
			Instance._sparkerMainModule.gravityModifier = (isGravity ? 0.6f : 0.1f);
			Instance.Sparker.Emit(quantity);
		}
	}

	public override void ManagerAwake()
	{
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
		base.ManagerAwake();
		Instance = this;
		StringManager.Initialize();
		Localization.GetLanguages();
		GameStrings.Initialize();
		Settings.LoadSettings();
		UpdateFrameLimiter();
		WorkshopMenu.LoadModConfig();
		CelestialSprite.Initialize(CelestialSpritePrefab);
		Digit.Initialize(DigitPrefab);
		RockyBody.Initialize(RockyBodyPrefab);
		AtmosphericBody.Initialize(AtmosphericBodyPrefab);
		if (!GameManager.IsBatchMode)
		{
			_sparkerMainModule = Sparker.main;
			_sparkerTransform = Sparker.transform;
			if (Settings.CurrentData.FirstRun)
			{
				Settings.CurrentData.FirstRun = false;
				Settings.SaveSettings();
				if (Settings.GetIsAMDGPU())
				{
					DriverWarningScreen.SetActive(value: true);
				}
			}
		}
		foreach (WorldSetting allWorldSetting in WorldSetting.AllWorldSettings)
		{
			allWorldSetting.Initialize();
		}
		ClearWorld();
		GameManager.OnGameStateChange += UpdateFrameLimiter;
	}

	private void OnDestroy()
	{
		GridController.World?.Dispose();
		GameManager.OnGameStateChange -= UpdateFrameLimiter;
	}

	public async UniTask LoadGameDataAsync()
	{
		MainMenuCanvas.enabled = false;
		ImGuiLoadingScreen.SetActive(active: true);
		LayerMasks.Initialize();
		await Prefab.LoadAll();
		Reagent.GenerateReagentTypeLookup();
		Ingot.RecipeComparable.ClearRecipe(resetInitialHash: true);
		Centrifuge.RecipeComparable.ClearRecipe(resetInitialHash: true);
		Furnace.RecipeComparable.ClearRecipe(resetInitialHash: true);
		AdvancedFurnace.RecipeComparable.ClearRecipe(resetInitialHash: true);
		ArcFurnace.RecipeComparable.ClearRecipe(resetInitialHash: true);
		ChemistryStation.RecipeComparable.ClearRecipe(resetInitialHash: true);
		PaintMixer.RecipeComparable.ClearRecipe(resetInitialHash: true);
		Microwave.RecipeComparable.ClearRecipe(resetInitialHash: true);
		Autolathe.RecipeComparable.ClearRecipe(resetInitialHash: true);
		AutomatedOven.RecipeComparable.ClearRecipe(resetInitialHash: true);
		AdvancedPackagingMachine.RecipeComparable.ClearRecipe(resetInitialHash: true);
		ElectronicsPrinter.RecipeComparable.ClearRecipe(resetInitialHash: true);
		SecurityPrinter.RecipeComparable.ClearRecipe(resetInitialHash: true);
		RocketManufactory.RecipeComparable.ClearRecipe(resetInitialHash: true);
		TerraformingManufactory.RecipeComparable.ClearRecipe(resetInitialHash: true);
		HydraulicPipeBender.RecipeComparable.ClearRecipe(resetInitialHash: true);
		ToolManufactory.RecipeComparable.ClearRecipe(resetInitialHash: true);
		OrganicsPrinter.RecipeComparable.ClearRecipe(resetInitialHash: true);
		BasicPackagingMachine.RecipeComparable.ClearRecipe(resetInitialHash: true);
		ElectronicReader.ClearRecipe();
		Recycler.RecycleRecipes.Clear();
		ReagentProcessor.RecipeComparable.ClearRecipe(resetInitialHash: true);
		ScreenFilter.Clear();
		if (!_recipeComparables.Any())
		{
			_recipeComparables.Add(RecipeType.Ingot, Ingot.RecipeComparable);
			_recipeComparables.Add(RecipeType.Centrifuge, Centrifuge.RecipeComparable);
			_recipeComparables.Add(RecipeType.Furnace, Furnace.RecipeComparable);
			_recipeComparables.Add(RecipeType.AdvancedFurnace, AdvancedFurnace.RecipeComparable);
			_recipeComparables.Add(RecipeType.ArcFurnace, ArcFurnace.RecipeComparable);
			_recipeComparables.Add(RecipeType.ChemistryStation, ChemistryStation.RecipeComparable);
			_recipeComparables.Add(RecipeType.PaintMixer, PaintMixer.RecipeComparable);
			_recipeComparables.Add(RecipeType.Microwave, Microwave.RecipeComparable);
			_recipeComparables.Add(RecipeType.AutomatedOven, AutomatedOven.RecipeComparable);
			_recipeComparables.Add(RecipeType.AdvancedPackagingMachine, AdvancedPackagingMachine.RecipeComparable);
			_recipeComparables.Add(RecipeType.Autolathe, Autolathe.RecipeComparable);
			_recipeComparables.Add(RecipeType.ElectronicsPrinter, ElectronicsPrinter.RecipeComparable);
			_recipeComparables.Add(RecipeType.SecurityPrinter, SecurityPrinter.RecipeComparable);
			_recipeComparables.Add(RecipeType.RocketManufactory, RocketManufactory.RecipeComparable);
			_recipeComparables.Add(RecipeType.TerraformingManufactory, TerraformingManufactory.RecipeComparable);
			_recipeComparables.Add(RecipeType.HydraulicPipeBender, HydraulicPipeBender.RecipeComparable);
			_recipeComparables.Add(RecipeType.ToolManufactory, ToolManufactory.RecipeComparable);
			_recipeComparables.Add(RecipeType.OrganicsPrinter, OrganicsPrinter.RecipeComparable);
			_recipeComparables.Add(RecipeType.ReagentProcessor, ReagentProcessor.RecipeComparable);
			_recipeComparables.Add(RecipeType.PackagingMachine, BasicPackagingMachine.RecipeComparable);
		}
		BeforeLoadDataFiles();
		LoadDataFiles();
		AfterLoadDataFiles();
		NewWorldMenu.PopulateWorldHashes();
		foreach (TraderData allTraderDatum in TraderData.AllTraderData)
		{
			allTraderDatum.Validate();
		}
		ContactSlot.ContactSlots.Clear();
		foreach (ContactSlotData allContactSlotDatum in ContactSlotData.AllContactSlotData)
		{
			ContactSlot.ContactSlots.Add(new ContactSlot(allContactSlotDatum));
		}
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenGeneratingRecipes.DisplayString);
		ScreenFilter.GenerateFilters();
		ScreenConstructionJob.GenerateRecipieList();
		Ingot.RecipeComparable.GenerateRecipieList();
		Centrifuge.RecipeComparable.GenerateRecipieList();
		await ImGuiLoadingScreen.SetProgress(0.34f);
		AdvancedFurnace.RecipeComparable.GenerateRecipieList();
		Furnace.RecipeComparable.GenerateRecipieList();
		ArcFurnace.RecipeComparable.GenerateRecipieList();
		ChemistryStation.RecipeComparable.GenerateRecipieList();
		PaintMixer.RecipeComparable.GenerateRecipieList();
		Microwave.RecipeComparable.GenerateRecipieList();
		await ImGuiLoadingScreen.SetProgress(0.69f);
		AdvancedPackagingMachine.RecipeComparable.GenerateRecipieList();
		ReagentProcessor.RecipeComparable.GenerateRecipieList();
		AutomatedOven.RecipeComparable.GenerateRecipieList();
		Autolathe.RecipeComparable.GenerateRecipieList();
		ElectronicsPrinter.RecipeComparable.GenerateRecipieList();
		SecurityPrinter.RecipeComparable.GenerateRecipieList();
		RocketManufactory.RecipeComparable.GenerateRecipieList();
		TerraformingManufactory.RecipeComparable.GenerateRecipieList();
		HydraulicPipeBender.RecipeComparable.GenerateRecipieList();
		OrganicsPrinter.RecipeComparable.GenerateRecipieList();
		ToolManufactory.RecipeComparable.GenerateRecipieList();
		BasicPackagingMachine.RecipeComparable.GenerateRecipieList();
		ElectronicReader.GenerateRecipieList();
		await ImGuiLoadingScreen.SetProgress(1f);
		AtmosphericsWorker.Initialize();
		TerraForming.Initialize();
		VoxelTerrain.Initialize();
		InventoryManager.Instance.Initialize();
		GenerateCustomThingBlueprints();
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenCleaningUpPrefabs.DisplayString);
		for (int i = 0; i < Prefab.AllPrefabs.Count; i++)
		{
			if (Prefab.AllPrefabs[i] is IResourceConsumer resourceConsumer)
			{
				Thing.AddToLookup(resourceConsumer);
			}
			await ImGuiLoadingScreen.SetProgress((float)i / (float)Prefab.AllPrefabs.Count);
		}
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenLoadingStationpedia.DisplayString);
		Stationpedia.Regenerate();
		for (int j = 0; j < 21; j++)
		{
			if (_recipeComparables.ContainsKey((RecipeType)j))
			{
				_recipeComparables[(RecipeType)j].SetInitialHash();
			}
		}
		if (!GameManager.IsBatchMode)
		{
			Assets.Scripts.UI.MainMenu.Instance.PageManager.EnableMainMenuPage("MainMenu");
			MainMenuCanvas.enabled = true;
		}
		GameManager.UnloadAssetsAndCollectGarbage();
		ImGuiLoadingScreen.SetActive(active: false);
		IsInitialized = true;
		OnGameDataLoaded?.Invoke();
	}

	public static WorldType ConvertStringToWorldType(string wName)
	{
		return wName.ToLower() switch
		{
			"space" => WorldType.Space, 
			"mimas" => WorldType.Mimas, 
			"vulcan2" => WorldType.Vulcan, 
			"mars" => WorldType.Mars, 
			"moon" => WorldType.Moon, 
			"europa2" => WorldType.Europa, 
			"loulan" => WorldType.Loulan, 
			"europa" => WorldType.Europa, 
			"vulcan" => WorldType.Vulcan, 
			"venus" => WorldType.Venus, 
			_ => WorldType.Custom, 
		};
	}

	public static async UniTask<Texture2D> LoadImageFromStreamingAssets(string folderPath, string fileName)
	{
		Texture2D texture = new Texture2D(1, 1);
		string path = Application.streamingAssetsPath + "\\" + folderPath + "\\" + fileName + ".png";
		if (!File.Exists(path))
		{
			return null;
		}
		texture.LoadImage(await File.ReadAllBytesAsync(path));
		return texture;
	}

	private static void BeforeLoadDataFiles()
	{
		_blueprintsToGenerate.Clear();
		DataResolver.Clear();
	}

	private static void LoadDataFiles()
	{
		LoadDataFilesAtPath(Path.Combine(Application.streamingAssetsPath, "Worlds"));
		LoadDataFilesAtPath(Path.Combine(Application.streamingAssetsPath, "Data"));
		foreach (ModData mod in WorkshopMenu.ModsConfig.Mods)
		{
			if (mod.Enabled && !(mod is CoreModData))
			{
				LoadDataFilesAtPath(Path.Combine(mod.DirectoryPath, "GameData"));
			}
		}
	}

	private static void AfterLoadDataFiles()
	{
		DataResolver.ResolveAll();
	}

	public static bool ReloadTerrainTextures(out string message)
	{
		message = string.Empty;
		if (WorldSetting.Current == null)
		{
			message = "Failed to reload terrain textures";
			return false;
		}
		string path = Path.Combine(Application.streamingAssetsPath, "Worlds");
		if (!Directory.Exists(path))
		{
			message = "Failed to reload terrain textures";
			return false;
		}
		string[] files = Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories);
		foreach (string text in files)
		{
			if (text.ToLower().Contains("gamedata\\language"))
			{
				continue;
			}
			try
			{
				ModAbout modAbout = ModAbout.Load(text);
				if (modAbout != null)
				{
					ConsoleWindow.PrintAction("loading the '" + modAbout.Name + "' mod");
				}
				GameData obj = XmlSerialization.Deserialize(Serializers.GameData, text) as GameData;
				if (obj == null)
				{
					ConsoleWindow.PrintError("error deserializing GameData for " + text + " ");
					message = "Failed to reload terrain textures";
					throw new XmlException("Failed to Deserialize GameData");
				}
				foreach (WorldSettingData worldSetting in obj.WorldSettings)
				{
					if (!(WorldSetting.Current.Id != worldSetting.Id))
					{
						StreamingAssetLoader.UnloadOnDemandTextures();
						worldSetting.TerrainSettings.Initialize(modAbout);
						WorldSetting.Current.Data.TerrainSettings = worldSetting.TerrainSettings;
						TerrainShaderScript.ApplyMaterialSettings(VoxelTerrain.Instance.TerrainMaterial, WorldSetting.Current.Data.TerrainSettings);
						TerrainShaderScript.SetTerrainDetail(Settings.CurrentData.TerrainDetail);
						VoxelTerrain.CreateLavaMesh(worldSetting.TerrainSettings.LavaData);
						message = "Applied Terrain Settings for " + WorldSetting.Current.Id + " from " + text;
						break;
					}
				}
			}
			catch (Exception exception)
			{
				ConsoleWindow.PrintError(GameStrings.FailedToLoadGameData.AsString(text));
				ConsoleWindow.PrintError(exception).Forget();
				message = "Failed to reload terrain textures";
			}
		}
		return true;
	}

	private static void LoadDataFilesAtPath(string path)
	{
		if (!Directory.Exists(path))
		{
			return;
		}
		string[] files = Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories);
		string[] array = files;
		foreach (string text in array)
		{
			if (!text.ToLower().Contains("gamedata\\language"))
			{
				try
				{
					LoadXmlFileDataFirstPass(Serializers.GameData, text);
				}
				catch (Exception exception)
				{
					ConsoleWindow.PrintError(GameStrings.FailedToLoadGameData.AsString(text));
					ConsoleWindow.PrintError(exception).Forget();
				}
			}
		}
		array = files;
		foreach (string text2 in array)
		{
			if (!text2.ToLower().Contains("gamedata\\language"))
			{
				try
				{
					LoadXmlFileData(Serializers.GameData, text2);
				}
				catch (Exception exception2)
				{
					ConsoleWindow.PrintError(GameStrings.FailedToLoadGameData.AsString(text2));
					ConsoleWindow.PrintError(exception2).Forget();
				}
			}
		}
		if (Directory.Exists(path + "\\Language"))
		{
			Localization.GetLanguages(path);
			Localization.ProcessNewPages(Settings.CurrentData.LanguageCode);
		}
	}

	public static void LoadXmlFileDataFirstPass(XmlSerializer serializer, string xmlFile)
	{
		if (!(XmlSerialization.Deserialize(serializer, xmlFile) is GameData gameData))
		{
			Debug.LogError("Failed to load " + xmlFile);
		}
		else
		{
			LoadCustomThingData(gameData);
		}
	}

	public static void LoadXmlFileData(XmlSerializer serializer, string xmlFile)
	{
		ModAbout modAbout = ModAbout.Load(xmlFile);
		if (modAbout != null)
		{
			ConsoleWindow.PrintAction("loading the '" + modAbout.Name + "' mod");
		}
		if (!(XmlSerialization.Deserialize(serializer, xmlFile) is GameData gameData))
		{
			ConsoleWindow.PrintError("error deserializing GameData for " + xmlFile + " ");
			throw new XmlException("Failed to Deserialize GameData");
		}
		foreach (DifficultySetting difficultySetting in gameData.DifficultySettings)
		{
			DifficultySetting.Register(difficultySetting, modAbout);
		}
		foreach (RecipeData ingotRecipe in gameData.IngotRecipes)
		{
			ingotRecipe.Recipe.Check();
			Ingot.RecipeComparable.AddRecipe(ingotRecipe, modAbout);
		}
		foreach (RecipeData centrifugeRecipe in gameData.CentrifugeRecipes)
		{
			centrifugeRecipe.Recipe.Check();
			Centrifuge.RecipeComparable.AddRecipe(centrifugeRecipe, modAbout);
		}
		foreach (RecipeData furnaceRecipe in gameData.FurnaceRecipes)
		{
			furnaceRecipe.Recipe.Check();
			if (Furnace.RecipeComparable.AddRecipe(furnaceRecipe, modAbout))
			{
				AdvancedFurnace.RecipeComparable.AddRecipe(furnaceRecipe, modAbout);
				Recycler.AddRecycleRecipe(Animator.StringToHash(furnaceRecipe.PrefabName), new ReagentMixture(furnaceRecipe.Recipe), authored: false, 1f);
			}
		}
		foreach (RecipeData advancedFurnaceRecipe in gameData.AdvancedFurnaceRecipes)
		{
			advancedFurnaceRecipe.Recipe.Check();
			if (AdvancedFurnace.RecipeComparable.AddRecipe(advancedFurnaceRecipe, modAbout))
			{
				Recycler.AddRecycleRecipe(Animator.StringToHash(advancedFurnaceRecipe.PrefabName), new ReagentMixture(advancedFurnaceRecipe.Recipe), authored: false, 1f);
			}
		}
		foreach (RecipeData microwaveRecipe in gameData.MicrowaveRecipes)
		{
			microwaveRecipe.Recipe.Check();
			Microwave.RecipeComparable.AddRecipe(microwaveRecipe, modAbout);
		}
		foreach (RecipeData packagingMachineRecipe in gameData.PackagingMachineRecipes)
		{
			packagingMachineRecipe.Recipe.Check();
			BasicPackagingMachine.RecipeComparable.AddRecipe(packagingMachineRecipe, modAbout);
		}
		foreach (RecipeData arcFurnaceRecipe in gameData.ArcFurnaceRecipes)
		{
			arcFurnaceRecipe.Recipe.Check();
			ArcFurnace.RecipeComparable.AddRecipe(arcFurnaceRecipe, modAbout);
		}
		foreach (RecipeData autolatheRecipe in gameData.AutolatheRecipes)
		{
			autolatheRecipe.Recipe.Check();
			Autolathe.RecipeComparable.AddRecipe(autolatheRecipe, modAbout);
		}
		foreach (RecipeData automatedOvenRecipe in gameData.AutomatedOvenRecipes)
		{
			automatedOvenRecipe.Recipe.Check();
			AutomatedOven.RecipeComparable.AddRecipe(automatedOvenRecipe, modAbout);
		}
		foreach (RecipeData packagingMachineRecipe2 in gameData.PackagingMachineRecipes)
		{
			packagingMachineRecipe2.Recipe.Check();
			AdvancedPackagingMachine.RecipeComparable.AddRecipe(packagingMachineRecipe2, modAbout);
		}
		foreach (RecipeData electronicsPrinterRecipe in gameData.ElectronicsPrinterRecipes)
		{
			electronicsPrinterRecipe.Recipe.Check();
			ElectronicsPrinter.RecipeComparable.AddRecipe(electronicsPrinterRecipe, modAbout);
		}
		foreach (RecipeData securityPrinterRecipe in gameData.SecurityPrinterRecipes)
		{
			securityPrinterRecipe.Recipe.Check();
			SecurityPrinter.RecipeComparable.AddRecipe(securityPrinterRecipe, modAbout);
		}
		foreach (RecipeData rocketManufactoryRecipe in gameData.RocketManufactoryRecipes)
		{
			rocketManufactoryRecipe.Recipe.Check();
			RocketManufactory.RecipeComparable.AddRecipe(rocketManufactoryRecipe, modAbout);
		}
		foreach (RecipeData terraformingManufactoryRecipe in gameData.TerraformingManufactoryRecipes)
		{
			terraformingManufactoryRecipe.Recipe.Check();
			TerraformingManufactory.RecipeComparable.AddRecipe(terraformingManufactoryRecipe, modAbout);
		}
		foreach (RecipeData chemistryRecipe in gameData.ChemistryRecipes)
		{
			chemistryRecipe.Recipe.Check();
			ChemistryStation.RecipeComparable.AddRecipe(chemistryRecipe, modAbout);
		}
		foreach (RecipeData paintMixRecipe in gameData.PaintMixRecipes)
		{
			paintMixRecipe.Recipe.Check();
			PaintMixer.RecipeComparable.AddRecipe(paintMixRecipe, modAbout);
		}
		foreach (RecipeData hydraulicPipeBenderRecipe in gameData.HydraulicPipeBenderRecipes)
		{
			hydraulicPipeBenderRecipe.Recipe.Check();
			HydraulicPipeBender.RecipeComparable.AddRecipe(hydraulicPipeBenderRecipe, modAbout);
		}
		foreach (RecipeData toolManufactoryRecipe in gameData.ToolManufactoryRecipes)
		{
			toolManufactoryRecipe.Recipe.Check();
			ToolManufactory.RecipeComparable.AddRecipe(toolManufactoryRecipe, modAbout);
		}
		foreach (RecipeData recycleRecipe in gameData.RecycleRecipes)
		{
			Recycler.AddRecycleRecipe(Animator.StringToHash(recycleRecipe.PrefabName), new ReagentMixture(recycleRecipe.Recipe), authored: true);
		}
		foreach (ProcessingData reagentGrinderRecipe in gameData.ReagentGrinderRecipes)
		{
			ReagentProcessor.RecipeComparable.AddRecipe(reagentGrinderRecipe, modAbout);
		}
		foreach (MinableVisualiserData minableVisualiser in gameData.MinableVisualisers)
		{
			minableVisualiser.Initialize(modAbout);
		}
		foreach (NodeIcon nodeIcon in gameData.NodeIcons)
		{
			nodeIcon.Initialise();
		}
		foreach (SpaceMapData spaceMap in gameData.SpaceMaps)
		{
			spaceMap.Initialize(modAbout);
		}
		Instance.SetWorldPresetSettings(gameData.WorldSettings, xmlFile, modAbout);
		foreach (BuyData buyData in gameData.BuyDatas)
		{
			buyData.Initialize(modAbout);
		}
		foreach (SellData sellData in gameData.SellDatas)
		{
			sellData.Initialize(modAbout);
		}
		foreach (SpawnData spawnData in gameData.SpawnDatas)
		{
			spawnData.Initialize(modAbout);
		}
		foreach (StartConditionData startCondition in gameData.StartConditions)
		{
			startCondition.Initialize(modAbout);
		}
		foreach (StartLocationData startLocation in gameData.StartLocations)
		{
			startLocation.Initialize(modAbout);
		}
		foreach (WorldObjective worldObjective in gameData.WorldObjectives)
		{
			worldObjective.Initialize(modAbout);
		}
		foreach (WorldObjectiveCollection worldObjectiveCollection in gameData.WorldObjectiveCollections)
		{
			worldObjectiveCollection.Initialize(modAbout);
		}
		foreach (CelestialBodyTemplate celestialBody in gameData.CelestialBodies)
		{
			celestialBody.Register();
		}
		foreach (TraderData traderData in gameData.TraderDatas)
		{
			traderData.Initialize(modAbout);
		}
		foreach (ContactSlotData contactSlot in gameData.ContactSlots)
		{
			contactSlot.Initialise();
		}
		foreach (RoomTypeRuleData roomTypeRuleDatum in gameData.RoomTypeRuleData)
		{
			RoomManager.AddRoomTypeRule(roomTypeRuleDatum);
		}
		foreach (ThingModData thingMod in gameData.ThingMods)
		{
			Thing thing = Prefab.Find(thingMod.PrefabName);
			if ((bool)thing)
			{
				thing.DeserializModData(thingMod);
			}
		}
		foreach (RocketName defaultRocketName in gameData.DefaultRocketNames)
		{
			if (!string.IsNullOrWhiteSpace(defaultRocketName.Name))
			{
				Rocket.DefaultRocketNames.Add(defaultRocketName.Name);
			}
		}
		foreach (ItemReplacement itemReplacement in gameData.ItemReplacements)
		{
			Item.RegisterItemReplacement(itemReplacement, modAbout);
		}
		foreach (TerraformingGasCurveData terraformingGasCurveData in gameData.TerraformingGasCurveDatas)
		{
			terraformingGasCurveData.Init();
		}
		foreach (AtmosphericScatteringBlendData atmosphericScatteringBlendData in gameData.AtmosphericScatteringBlendDatas)
		{
			atmosphericScatteringBlendData.Initialize(modAbout);
		}
		foreach (LifeRequirementsData plantLifeRequirementsDatum in gameData.PlantLifeRequirementsData)
		{
			plantLifeRequirementsDatum.Initialize(modAbout);
		}
		foreach (BlueprintData blueprintDatum in gameData.BlueprintData)
		{
			blueprintDatum.Initialize(modAbout);
		}
		foreach (WeatherEvent weatherEvent in gameData.WeatherEvents)
		{
			weatherEvent.Initialize(modAbout);
		}
		foreach (ServerProvider serverProvider in gameData.ServerProviders)
		{
			ServerProviderPanel.Add(serverProvider);
		}
		foreach (VeinGenerationData oreVein in gameData.OreVeins)
		{
			oreVein.Initialize(modAbout);
		}
		foreach (PlayerCosmeticsBehaviour.FacialExpressionData expression in gameData.Expressions)
		{
			expression.Initialize(modAbout);
		}
		foreach (MinablesGenerationData minable in gameData.Minables)
		{
			minable.Initialize(modAbout);
		}
		foreach (DeepMinablesGenerationData deepMinable in gameData.DeepMinables)
		{
			deepMinable.Initialize(modAbout);
		}
		if (modAbout != null)
		{
			ConsoleWindow.Print("successfully loaded the '" + modAbout.Name + "' mod");
		}
	}

	private static void LoadCustomThingData(GameData gameData)
	{
		foreach (CustomThingData customThingDatum in gameData.CustomThingData)
		{
			if (Prefab.TryFind(customThingDatum.Name, out var _))
			{
				ConsoleWindow.PrintError("Loading custom thing failed. Thing " + customThingDatum.Name + " already exists.");
				continue;
			}
			_blueprintsToGenerate.Add(customThingDatum);
			customThingDatum.Initialize();
			customThingDatum.RegisterPrefab();
		}
	}

	private static void GenerateCustomThingBlueprints()
	{
		foreach (CustomThingData item in _blueprintsToGenerate)
		{
			ThingImporter.Instance.CreateBlueprint(item);
		}
		_blueprintsToGenerate.Clear();
	}

	private void SetWorldPresetSettings(List<WorldSettingData> worldSettingData, string xmlFile, ModAbout mod)
	{
		if (worldSettingData == null)
		{
			return;
		}
		foreach (WorldSettingData worldSettingDatum in worldSettingData)
		{
			if (!worldSettingDatum.IsValid())
			{
				ConsoleWindow.PrintError("error Loading World Data " + worldSettingDatum.Id + " in " + xmlFile + ".");
				continue;
			}
			worldSettingDatum.Mod = mod;
			DataCollection.Register(worldSettingDatum, mod);
			worldSettingDatum.Initialize(mod);
			DataResolver.AddResolutionTask(new WorldSettingResolutionTask(worldSettingDatum));
			WorldSetting worldSetting = ImportWorldSettingData(worldSettingDatum);
			foreach (SpawnData spawnData in worldSettingDatum.SpawnDatas)
			{
				spawnData.Initialize(mod);
			}
			worldSettingDatum.SpaceMapData?.Initialize(mod);
			worldSettingDatum.CelestialBodies.LoadData();
			worldSetting.CelestialBodies.LoadData();
			foreach (RegionSet regionSet in worldSettingDatum.RegionSets)
			{
				regionSet.Initialize(mod);
			}
			foreach (PointOfInterest item in worldSettingDatum.PointsOfInterest)
			{
				item.Initialize(mod);
			}
			foreach (AchievementChainData achievementChain in worldSettingDatum.AchievementChains)
			{
				achievementChain.Initialize(mod);
			}
			if (WorldSetting.Find(worldSettingDatum.Id) != null)
			{
				WorldSetting.Reregister(worldSetting);
			}
			else
			{
				WorldSetting.Register(worldSetting);
			}
		}
	}

	public override void ManagerStart()
	{
		base.ManagerStart();
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		if (!GameManager.IsBatchMode)
		{
			UpdateFrameRate();
			UpdateBrightness();
			UpdateFoV();
			UpdateColorBlind();
			UpdateCameraSensitivity();
			UpdateHUDScale();
			UpdateTooltipOpacity();
		}
		else
		{
			AudioListener.volume = 0f;
			AudioListener.pause = true;
		}
		Singleton<MonoBehaviourListener>.Instance.SetType(MonoBehaviourType.WorldManager);
		DifficultySetting.OnGameDataLoad();
	}

	public static WorldSetting ImportWorldSettingData(WorldSettingData input)
	{
		WorldSetting worldSetting = new WorldSetting
		{
			Id = input.Id,
			Data = input,
			SkyBox = Resources.Load<Material>("WorldEnvironment/SkyBoxes/" + input.SkyBoxMaterialName),
			Sun = Resources.Load<GameObject>("WorldEnvironment/Suns/" + input.SunPrefabName),
			PreviewScene = new PreviewScene(input.PreviewScene)
		};
		worldSetting.PreviewScene.Sun = Resources.Load<GameObject>("WorldEnvironment/Suns/" + input.PreviewScene.SunPrefab);
		if (worldSetting.PreviewScene.Sun == null)
		{
			worldSetting.PreviewScene.Sun = Resources.Load<GameObject>("WorldEnvironment/Suns/" + input.SunPrefabName);
		}
		else
		{
			worldSetting.PreviewScene.Sun.name = input.PreviewScene.SunPrefab;
		}
		foreach (PlanetPrefab item in input.Skybox)
		{
			item.GameObject = Resources.Load<GameObject>("WorldEnvironment/SkyBoxes/" + item.Name);
			if (item.GameObject == null)
			{
				Debug.Log("Error: Missing Planetary Prefab <b>" + item.Name + "</b> for " + input.Id);
				continue;
			}
			item.GameObject.transform.position = item.Position;
			item.GameObject.transform.rotation = Quaternion.Euler(item.Rotation);
			item.GameObject.transform.localScale = item.Scale;
			worldSetting.PlanetPrefabs.Add(item);
		}
		foreach (PlanetPrefab prefab in worldSetting.PreviewScene.Prefabs)
		{
			prefab.GameObject = Resources.Load<GameObject>("WorldEnvironment/SkyBoxes/" + prefab.Name);
			if (prefab.GameObject == null)
			{
				prefab.GameObject = Resources.Load<GameObject>("WorldEnvironment/SkyBoxes/Light/" + prefab.Name);
			}
			if (prefab.GameObject == null)
			{
				Debug.Log("Error: Missing Planetary Prefab <b>" + prefab.Name + "</b> for " + input.Id);
			}
		}
		return worldSetting;
	}

	public void UpdateBrightness()
	{
		Brightness.color = new Color(0f, 0f, 0f, 1f - (float)Settings.CurrentData.Brightness / 100f);
		Brightness.gameObject.SetActive(Settings.CurrentData.Brightness != 100);
	}

	public void UpdateFoV()
	{
		CameraController.SetFieldOfView((GameManager.GameState == GameState.None) ? 39f : ((float)Settings.CurrentData.FieldOfView));
		if ((bool)FirstPersonHelmetOverlay.Instance)
		{
			FirstPersonHelmetOverlay.Instance.UpdateHelmetScale();
		}
	}

	public void UpdateColorBlind()
	{
		ColorBlindFilter[] array = UnityEngine.Object.FindObjectsOfType<ColorBlindFilter>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].colorBlindMode = (ColorBlindMode)Enum.Parse(typeof(ColorBlindMode), Settings.CurrentData.ColorBlind);
		}
	}

	public void UpdateFrameLimiter()
	{
		if (GameManager.IsBatchMode)
		{
			Application.targetFrameRate = 25;
		}
		else if (GameManager.GameState != GameState.Running)
		{
			Application.targetFrameRate = 60;
		}
		else if (!string.Equals(Settings.CurrentData.FrameLock, "Off"))
		{
			int num = int.Parse(Settings.CurrentData.FrameLock);
			Mathf.Clamp(num, 60, 250);
			Application.targetFrameRate = num;
		}
		else
		{
			Application.targetFrameRate = 250;
		}
	}

	public void UpdateParticleQuality()
	{
	}

	public void UpdateEnvironmentElements()
	{
	}

	public void UpdateVolumeLight()
	{
		CameraController.SetBloom(Settings.CurrentData.VolumeLight != "None");
		bool num = Settings.CurrentData.VolumeLight != "None";
		CameraController.SetVolumetricLights(num);
		if (num)
		{
			CameraController.SetVolumetricLightResolution((VolumetricLightRenderer.VolumtericResolution)Enum.Parse(typeof(VolumetricLightRenderer.VolumtericResolution), Settings.CurrentData.VolumeLight));
		}
	}

	public void UpdateFrameRate()
	{
		FrameRate.transform.parent.gameObject.SetActive(Settings.CurrentData.ShowFps);
		if (Settings.CurrentData.ShowFps && !_rateCounterShown)
		{
			FrameRateCounter().Forget();
		}
	}

	public void UpdateCameraSensitivity()
	{
		CameraController.CameraSensitivity = RocketMath.MapToScale(0f, 100f, 0.2f, 4f, Settings.CurrentData.CameraSensitivity);
	}

	private async UniTaskVoid FrameRateCounter()
	{
		_rateCounterShown = true;
		while (Settings.CurrentData.ShowFps)
		{
			deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
			int num = (int)(deltaTime * 100000f);
			int value = num / 100;
			int value2 = num % 100;
			int value3 = (int)(1f / deltaTime);
			FrameRate.text = StringManager.Get(value) + "." + StringManager.Get(value2) + " ms (" + StringManager.Get(value3) + " fps)";
			await UniTask.NextFrame();
		}
		_rateCounterShown = false;
	}

	public void UpdateHUDScale()
	{
		CameraController.SetHudScale(PingCanvasScaler);
		CameraController.SetHudScale(GameCanvas.GetComponent<CanvasScaler>());
		if (WorldManager.OnHudScaleUpdate != null)
		{
			WorldManager.OnHudScaleUpdate();
		}
	}

	public void UpdateTooltipOpacity()
	{
		CastBarImage.color = CastBarImage.color.SetAlpha(Settings.CurrentData.TooltipOpacity);
		Tooltip.UpdateOpacity();
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		if (GameManager.IsRunning)
		{
			OrbitalSimulation.UpdateEachFrame();
		}
	}

	public static void StartWorld()
	{
		NewWorldMenu.ClearPreviewScenes();
		RoomManager.Instance.StartManager();
		ElectricityManager.Instance.StartManager();
		AtmosphericsManager.Instance.StartManager();
		OcclusionManager.Instance.StartManager();
		LightManager.Instance.StartManager();
		RocketParkSlot.Initialise();
		GameManager.SetTickSpeed();
		GameManager.StartGameTick();
		WorldSetting.StartWorld();
		_ = GameManager.IsBatchMode;
	}

	public void ClearWorld()
	{
		if (!GameManager.IsBatchMode)
		{
			SetMainMenuWorldSettings("Mars2");
			Settings.ApplyVolumeSettings();
			CameraController.ClearCameraShake();
			CameraController.SetUnderLava(show: false);
		}
		GameManager.GameState = GameState.None;
		XmlSaveLoad.WorldIsReadOnly = false;
		GameManager.IsTutorial = false;
		GameManager.IsNewTutorial = false;
		GameManager.IsScenario = false;
		LeanTween.cancel(Instance.gameObject);
		UpdateFoV();
		GameManager.UnloadAssetsAndCollectGarbage();
		DaysPast = 0u;
		Plant.CustomPlantGrowthSpeed = false;
		if (!GameManager.IsBatchMode)
		{
			if (WorldSetting.Current != null && HasLava && CursorManager.Instance != null)
			{
				CursorManager.Instance.LavaAudio.Stop();
				CursorManager.Instance.LavaLight.enabled = false;
				CursorManager.Instance.LavaPlane.SetActive(value: false);
				CursorManager.Instance.LavaParticles.Stop();
			}
			BlackScreen.Saturation = 1f;
			BlackScreen.Brightness = 1f;
			if ((bool)FirstPersonHelmetOverlay.Instance)
			{
				FirstPersonHelmetOverlay.Instance.OnWorldExit();
			}
			BlurRadial.enabled = false;
			BlurTiltShift.enabled = false;
			BloodHit.enabled = false;
			Vignette.enabled = false;
			LightWater.enabled = false;
			EletricShock.enabled = false;
			Drunken.enabled = false;
			BrokenGlass.enabled = false;
		}
		GameObject gameObject = GameObject.Find("WorldGround");
		if ((object)gameObject != null)
		{
			UnityEngine.Object.Destroy(gameObject);
		}
		GameObject gameObject2 = GameObject.Find("Sun");
		if ((object)gameObject2 != null)
		{
			UnityEngine.Object.Destroy(gameObject2);
		}
		GameObject gameObject3 = GameObject.Find("Moon");
		if ((object)gameObject3 != null)
		{
			UnityEngine.Object.Destroy(gameObject3);
		}
		WorldSun.TargetLight = null;
		LeanTween.cancel(base.gameObject);
		WeatherStation.AllWeatherStations.Clear();
		WirelessBattery.AllWirelessBatteries.Clear();
		if (!GameManager.IsBatchMode)
		{
			InventoryManager.ShowMenu = true;
			MenuCanvas.SetVisible(isVisble: true);
			GameCanvas.enabled = false;
			if (StatusUpdates.Instance != null)
			{
				StatusUpdates.Instance.DisableStatus();
			}
			HeatHaze.Clear();
		}
	}

	private void SetSun()
	{
		OrbitalSimulation.AssignSun(WorldSun.TargetLight);
	}

	public void InitializeWorldEnvironment()
	{
		if (!GameManager.IsBatchMode)
		{
			SkyBoxController.ClearPlanets();
			EffectManager.SetEffectsMaterial(0);
			RenderSettings.skybox = UnityEngine.Object.Instantiate(WorldSetting.Current.SkyBox);
			RenderSettings.ambientMode = AmbientMode.Skybox;
			if (WorldSetting.Current.Sun != null)
			{
				UnityEngine.Object.Destroy(SunGameObject);
				SunGameObject = UnityEngine.Object.Instantiate(WorldSetting.Current.Sun);
				SunGameObject.name = "Sun";
				WorldSun.TargetLight = SunGameObject.GetComponent<Light>();
				WorldSun.Color = WorldSun.TargetLight.color;
				WorldSun.Intensity = WorldSun.TargetLight.intensity;
				WorldSun.TargetLight.shadowBias = 0.2f;
				WorldSun.TargetLight.shadowNormalBias = 0f;
				RenderSettings.sun = WorldSun.TargetLight;
				SkyBoxController.SetStars(WorldSetting.Current);
				OrbitalSimulation.Load(WorldSetting.Current);
				SetSun();
				if (!GameManager.IsBatchMode)
				{
					EasyFlares component = RenderSettings.sun.GetComponent<EasyFlares>();
					if ((bool)component && component.enabled)
					{
						CursorManager.DefaultEasyFlareEnabled = component.enabled;
						component.enabled = Settings.CurrentData.LensFlares;
					}
					else
					{
						CursorManager.DefaultEasyFlareEnabled = false;
					}
					foreach (PlanetPrefab planetPrefab in WorldSetting.Current.PlanetPrefabs)
					{
						GameObject obj = UnityEngine.Object.Instantiate(planetPrefab.GameObject);
						obj.name = planetPrefab.Name;
						SkyBoxController.RegisterPlanet(obj.transform);
					}
				}
			}
			else
			{
				SkyBoxController.SetStars(WorldSetting.Current);
				OrbitalSimulation.Load(WorldSetting.Current);
			}
		}
		else
		{
			SkyBoxController.SetStars(WorldSetting.Current);
			OrbitalSimulation.Load(WorldSetting.Current);
		}
		if (!GameManager.IsBatchMode && AtmosphericScattering)
		{
			CursorManager.Instance.AtmosphericScattering.Import();
			CursorManager.UpdateAtmosphericScattering();
		}
		PlanetaryAtmosphereSimulation.CreateGlobalAtmosphere(WorldSetting.Current.Data.GlobalAtmosphereData);
		CursorManager.UpdateAtmosphericScattering();
		Physics.reuseCollisionCallbacks = true;
		Physics.gravity = new Vector3(0f, WorldGravity, 0f);
		EarthGravityOffset = new Vector3(0f, -9.8f, 0f) - Physics.gravity;
		EarthGravityRatio = (HasGravity ? (Physics.gravity.y / -9.8f) : 0f);
		if (WorldSetting.Current.HasCustomLavaColor)
		{
			Instance.lavaMaterial.SetColor(Thing.EMISSION_COLOR, WorldSetting.Current.LavaColor);
		}
	}

	public static void DamageEffect(DamageType type, float duringTime = 0.8f, float shakeAmount = 1.5f, bool blurEffect = false, bool brokenGlass = false, float delayTime = 0f)
	{
		LeanTween.cancel(Instance.gameObject);
		switch (type)
		{
		case DamageType.Blood:
			LeanTween.value(Instance.gameObject, 1f, 0f, duringTime).setEase(LeanTweenType.easeInCirc).setOnUpdate(delegate(float value)
			{
				Instance.BloodHit.Blood_Hit_Full_1 = value;
				Instance.BloodHit.Blood_Hit_Full_2 = value;
				Instance.BloodHit.Blood_Hit_Full_3 = value;
			})
				.setDelay(delayTime);
			LeanTween.value(Instance.gameObject, 0.2f, 0f, duringTime).setEase(LeanTweenType.easeInCirc).setOnStart(delegate
			{
				Instance.BloodHit.enabled = true;
			})
				.setOnUpdate(delegate(float value)
				{
					Instance.BloodHit.LightReflect = value;
				})
				.setOnComplete((Action)delegate
				{
					Instance.BloodHit.enabled = false;
				})
				.setDelay(delayTime);
			LeanTween.value(Instance.gameObject, 0.156f, 0f, duringTime).setEase(LeanTweenType.easeInCirc).setOnStart(delegate
			{
				Instance.Vignette.enabled = true;
			})
				.setOnUpdate(delegate(float value)
				{
					Color vignettingColor = Instance.Vignette.VignettingColor;
					vignettingColor.a = value;
					Instance.Vignette.VignettingColor = vignettingColor;
				})
				.setOnComplete((Action)delegate
				{
					Instance.Vignette.enabled = false;
				})
				.setDelay(delayTime);
			break;
		case DamageType.Confused:
			LeanTween.value(Instance.gameObject, 0f, 0.627f, duringTime / 2f).setEase(LeanTweenType.easeInOutCirc).setOnStart(delegate
			{
				Instance.LightWater.enabled = true;
			})
				.setOnUpdate(delegate(float value)
				{
					Instance.LightWater.Alpha = value;
				})
				.setOnComplete((Action)delegate
				{
					Instance.LightWater.enabled = false;
				})
				.setLoopPingPong(1)
				.setDelay(delayTime);
			LeanTween.value(Instance.gameObject, 0f, 0.358f, duringTime / 2f).setEase(LeanTweenType.easeInOutCirc).setOnUpdate(delegate(float value)
			{
				Instance.LightWater.Size = value;
			})
				.setLoopPingPong(1)
				.setDelay(delayTime);
			break;
		case DamageType.EletricShock:
			LeanTween.value(Instance.gameObject, 1f, 0f, duringTime).setEase(LeanTweenType.easeInCirc).setOnStart(delegate
			{
				Instance.EletricShock.enabled = true;
			})
				.setOnUpdate(delegate(float value)
				{
					Instance.EletricShock._AlphaHexa = value;
				})
				.setOnComplete((Action)delegate
				{
					Instance.EletricShock.enabled = false;
				})
				.setDelay(delayTime);
			LeanTween.value(Instance.gameObject, 0f, 1f, duringTime).setEase(LeanTweenType.easeInCirc).setOnUpdate(delegate(float value)
			{
				Instance.EletricShock.Radius = value;
			})
				.setDelay(delayTime);
			break;
		case DamageType.Drunk:
			LeanTween.value(Instance.gameObject, 0f, 1f, duringTime).setOnStart(delegate
			{
				Instance.Drunken.enabled = true;
			}).setOnComplete((Action)delegate
			{
				Instance.Drunken.enabled = false;
			})
				.setDelay(delayTime);
			break;
		}
		if (blurEffect)
		{
			LeanTween.value(Instance.gameObject, 0f, -0.15f, duringTime / 2f).setEase(LeanTweenType.easeInOutCirc).setOnStart(delegate
			{
				Instance.BlurRadial.enabled = true;
			})
				.setOnUpdate(delegate(float value)
				{
					Instance.BlurRadial.Intensity = value;
				})
				.setOnComplete((Action)delegate
				{
					Instance.BlurRadial.enabled = false;
				})
				.setLoopPingPong(1)
				.setDelay(delayTime);
			LeanTween.value(Instance.gameObject, 0f, 1f, duringTime).setEase(LeanTweenType.easeInCirc).setOnStart(delegate
			{
				Instance.BlurTiltShift.enabled = true;
			})
				.setOnUpdate(delegate(float value)
				{
					Instance.BlurTiltShift.Size = value;
				})
				.setOnComplete((Action)delegate
				{
					Instance.BlurTiltShift.enabled = false;
				})
				.setDelay(delayTime);
		}
		if (brokenGlass)
		{
			LeanTween.value(Instance.gameObject, 40f, 0f, duringTime).setEase(LeanTweenType.easeInCirc).setOnStart(delegate
			{
				Instance.BrokenGlass.enabled = true;
			})
				.setOnUpdate(delegate(float value)
				{
					Instance.BrokenGlass.Broken_Big = value;
				})
				.setOnComplete((Action)delegate
				{
					Instance.BrokenGlass.enabled = false;
				})
				.setDelay(delayTime);
		}
		if (shakeAmount >= 0f)
		{
			float num = 0.2f;
			Vector3 originPos = CameraController.Instance.CameraPivotOffset;
			LeanTween.value(Instance.gameObject, shakeAmount, 0f, num).setEase(LeanTweenType.linear).setLoopCount((int)(duringTime / num))
				.setOnUpdate(delegate(float val)
				{
					Vector3 b = originPos + new Vector3(UnityEngine.Random.Range(0f - val, val), UnityEngine.Random.Range(0f - val, val), 0f);
					CameraController.Instance.CameraPivotOffset = Vector3.Lerp(CameraController.Instance.CameraPivotOffset, b, 2f * Time.deltaTime);
				})
				.setOnComplete((Action)delegate
				{
					CameraController.Instance.CameraPivotOffset = originPos;
				})
				.setDelay(delayTime);
		}
	}

	public static async UniTask SpawnOnNewWorld()
	{
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenSpawningThings.DisplayString);
		await WorldSetting.Current.SpawnOnNewWorld();
	}

	public void ResumePlay()
	{
		SetGamePause(pauseGame: false);
		if (!GameManager.IsBatchMode)
		{
			if (PromptPanel.Instance.PromptWindow.activeSelf)
			{
				PromptPanel.Instance.DisablePromptPanel();
			}
			if (AlertPanel.Instance.AlertWindow.activeSelf)
			{
				AlertPanel.Instance.DisableAlertPanel();
			}
		}
	}

	public static void OnPanelClose()
	{
		if (!Stationpedia.Instance.isActiveAndEnabled && !InputSourceCode.Instance.isActiveAndEnabled && (!InGameMenu.Instance || !InGameMenu.Instance.isActiveAndEnabled))
		{
			Instance.ResumePlay();
		}
	}

	public void EnablePause(bool showPrompt = true)
	{
		if (GameManager.RunSimulation)
		{
			if (!GameManager.IsBatchMode && showPrompt)
			{
				PromptPanel.Instance.ShowPrompt(PromptPauseStrings.Title, PromptPauseStrings.PauseBody, PromptPauseStrings.ResumeButton, ResumePlay, isEscapable: false, hideCancelButton: true);
			}
			SetGamePause(pauseGame: true);
		}
	}

	public static void SetGamePause(bool pauseGame)
	{
		if (IsGamePaused != pauseGame)
		{
			IsGamePaused = pauseGame;
			if (pauseGame)
			{
				KeyManager.SetInputState("WorldManager", KeyInputState.Paused);
			}
			else
			{
				KeyManager.RemoveInputState("WorldManager");
			}
			Time.timeScale = (pauseGame ? 0f : 1f);
			RoomManager.Instance.IsPaused = pauseGame;
			OcclusionManager.Instance.IsPaused = pauseGame;
			ElectricityManager.Instance.IsPaused = pauseGame;
			AtmosphericsManager.Instance.IsPaused = pauseGame;
			LightManager.Instance.IsPaused = pauseGame;
			if (!GameManager.IsBatchMode)
			{
				AudioManager.UpdateVolume(SettingType.SoundVolume);
			}
			WorldManager.OnPaused?.Invoke(pauseGame);
		}
	}

	private void SetMainMenuWorldSettings(string worldName)
	{
		WorldSetting worldSetting = WorldSetting.Find(worldName);
		if (worldSetting != null && AtmosphericScattering)
		{
			CursorManager.Instance.AtmosphericScattering.Import(worldSetting);
			CursorManager.Instance.AtmosphericScattering.UpdateStaticUniforms();
		}
	}

	public static void GenerateNewSeed()
	{
		Seed = UnityEngine.Random.Range(0, 10000000);
	}

	public static async UniTask Initialize()
	{
		await Instance.LoadGameDataAsync();
	}

	public static void SerializeOnJoin(RocketBinaryWriter writer)
	{
		writer.WriteString(World.CurrentId);
		writer.WriteInt32(WorldSetting.Current.GetHash());
		writer.WriteInt32(DifficultySetting.Current.GetHash());
		writer.WriteBoolean(IsCreative());
	}

	public static void DeserializeOnJoin(RocketBinaryReader reader)
	{
		World.CurrentId = reader.ReadString();
		int current = reader.ReadInt32();
		int current2 = reader.ReadInt32();
		bool num = reader.ReadBoolean();
		WorldSetting.SetCurrent(current);
		DifficultySetting.SetCurrent(current2);
		if (num)
		{
			SetCreativeMode(isCreative: true);
		}
		Instance.InitializeWorldEnvironment();
	}

	public static void SetCreativeMode(bool isCreative)
	{
		if (isCreative && Instance.GameMode != GameMode.Creative)
		{
			ConsoleWindow.PrintAction("Enabling Creative Mode");
		}
		Instance.GameMode = (isCreative ? GameMode.Creative : GameMode.Survival);
	}
}
