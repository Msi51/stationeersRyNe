using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Networks;
using Objects.Rockets;
using TerrainSystem;
using ThingImport;
using Trading;
using UnityEngine;

[XmlInclude(typeof(PreviewScene))]
[XmlInclude(typeof(SpawnGas))]
[XmlInclude(typeof(GameMode))]
[XmlRoot]
public class WorldSettingData : DataCollection
{
	[XmlAttribute]
	public bool Hidden;

	[XmlAttribute("Priority")]
	public int Priority;

	[XmlAttribute("Deprecated")]
	public bool Deprecated;

	[XmlElement("IsTutorial")]
	public BoolReference IsTutorial;

	[XmlElement(IsNullable = true)]
	public bool? DisplayBadge;

	[XmlElement]
	public LocalizedStringReference Description;

	[XmlElement("ShortDescription")]
	public LocalizedStringReference ShortDescription;

	[XmlElement]
	public LocalizedStringReference Rating;

	[XmlElement]
	public LocalizedStringReference SummaryText;

	[XmlElement]
	public string SkyBoxMaterialName;

	[XmlElement]
	public string SunPrefabName;

	public float MaxSunIntensity = 1f;

	[XmlIgnore]
	public ModAbout Mod;

	[XmlElement("Spawn")]
	public List<SpawnData> SpawnDatas = new List<SpawnData>();

	[XmlElement("StartCondition")]
	public List<StartConditionData> StartConditionDatas = new List<StartConditionData>();

	[XmlElement("StartLocation", typeof(StartLocationData))]
	[XmlElement("RandomStartLocation", typeof(RoundRobinStartLocationData))]
	public List<StartLocationData> StartLocationDatas = new List<StartLocationData>();

	[XmlElement("PlayableArea")]
	public List<PlayableAreaData> PlayableAreaDatas = new List<PlayableAreaData>();

	[XmlArray("Skybox")]
	[XmlArrayItem("PlanetPrefab", Type = typeof(PlanetPrefab))]
	public List<PlanetPrefab> Skybox = new List<PlanetPrefab>();

	[XmlElement("CelestialBodies")]
	public CelestialCollection CelestialBodies = new CelestialCollection();

	public CelestialConstantsReference CelestialConstants = CelestialConstantsReference.Default;

	public PlayableBodyReference PlayableBody;

	public PrimaryBodyReference PrimaryBody;

	public AmbientLightingReference AmbientLighting = AmbientLightingReference.Default;

	[XmlElement("GlobalAtmosphere")]
	public GlobalAtmosphereData GlobalAtmosphereData;

	[XmlElement]
	public PreviewScene PreviewScene = new PreviewScene();

	[XmlElement]
	public float Gravity;

	[XmlElement]
	public float LavaLevel;

	[XmlElement]
	public bool HasLava;

	[XmlElement]
	public int Seed;

	[XmlElement]
	public bool AtmosphericScattering;

	[XmlElement]
	public AtmosphericScatteringData AtmosphericScatteringData = new AtmosphericScatteringData();

	[XmlElement]
	public OrbitalViewData OrbitalView;

	[XmlElement]
	public float AmbientLightMin;

	[XmlElement]
	public float AmbientLightMax = 1f;

	[XmlElement]
	public Color AmbientSkyColor = Color.black;

	[XmlElement]
	public Color AmbientEquatorColor = Color.black;

	[XmlElement]
	public Color AmbientGroundColor = Color.black;

	[XmlElement]
	public bool SetSunInSkybox;

	[XmlArray("Stars")]
	[XmlArrayItem("Star")]
	public List<StarData> Stars = new List<StarData>();

	public TextureReference PreviewButton;

	[XmlElement]
	public Color LavaColor = new Color(69f / 106f, 0.27908587f, 0.11974899f);

	[XmlElement]
	public bool HasCustomLavaColor;

	[XmlElement]
	public string AmbienceSound;

	[XmlElement("WeatherEvent")]
	public List<WeatherEvent> WeatherEvents = new List<WeatherEvent>();

	[XmlElement("SpaceMap")]
	public SpaceMapData SpaceMapData;

	[XmlElement("WorldObjective")]
	public List<WorldObjectiveCollection> WorldObjectiveCollections = new List<WorldObjectiveCollection>();

	[XmlElement("TerrainSettings")]
	public TerrainSettings TerrainSettings;

	[XmlElement("RegionSet")]
	public List<RegionSet> RegionSets = new List<RegionSet>();

	[XmlElement("GeographicRegionData")]
	public GeographicRegionData GeographicRegionData;

	[XmlElement("DeepMinablesRegionData")]
	public DeepMinablesRegionData DeepMinablesRegionData;

	[XmlElement("Minables")]
	public List<MinablesGenerationData> MinablesData = new List<MinablesGenerationData>();

	[XmlElement("DeepMinables")]
	public List<DeepMinablesGenerationData> DeepMinablesData = new List<DeepMinablesGenerationData>();

	[XmlElement("PointOfInterest")]
	public List<PointOfInterest> PointsOfInterest = new List<PointOfInterest>();

	[XmlElement("AchievementChain")]
	public List<AchievementChainData> AchievementChains = new List<AchievementChainData>();

	[XmlElement("AchievementSurvival")]
	public List<AchievementSurvivalData> SurvivalAchievements = new List<AchievementSurvivalData>();

	private static SpawnData _atmosphereSpawns;

	private static readonly Action<Atmosphere> SaveAtmosphereAction = delegate(Atmosphere atmosphere)
	{
		if (atmosphere.IsValidWorld() && atmosphere.Room != null)
		{
			_atmosphereSpawns.WorldAtmospheres.Add(new WorldAtmosphereSpawnData(atmosphere));
		}
	};

	private static SpawnData _thingSpawns;

	private static HashSet<Atmosphere> _savedNetworkAtmopsheres = new HashSet<Atmosphere>();

	private static readonly Action<Thing> SaveThingAction = delegate(Thing thing)
	{
		if ((bool)thing && !thing.BeingDestroyed && !(thing is Human))
		{
			if (!(thing is INetworkedAtmospherics networkedAtmospherics))
			{
				if (!(thing is Structure structure))
				{
					if (!(thing is Item item))
					{
						if (thing is DynamicThing { ParentSlot: null } dynamicThing)
						{
							_thingSpawns.DynamicThings.Add(new DynamicSpawnData(dynamicThing));
						}
					}
					else if (item.ParentSlot == null)
					{
						_thingSpawns.Items.Add(new DynamicSpawnData(item));
					}
				}
				else
				{
					_thingSpawns.Structures.Add(new StructureSpawnData(structure));
				}
			}
			else
			{
				StructureSpawnData structureSpawnData = new StructureSpawnData(networkedAtmospherics as Structure);
				Atmosphere atmosphere = ((AtmosphericsNetwork)networkedAtmospherics.StructureNetwork)?.Atmosphere;
				if (atmosphere != null && !_savedNetworkAtmopsheres.Contains(atmosphere))
				{
					GasMixture.CreateGasActions(ref structureSpawnData.Actions, atmosphere.GasMixture);
					_savedNetworkAtmopsheres.Add(atmosphere);
				}
				_thingSpawns.Structures.Add(structureSpawnData);
			}
		}
	};

	private const string DEFAULT_SCENARIO_START_CONDITION_ID = "DefaultScenario";

	public int Hash => base.IdHash;

	public WorldSettingData DeepCopy()
	{
		WorldSettingData result = (WorldSettingData)MemberwiseClone();
		Initialize(Mod);
		return result;
	}

	public override bool IsValid()
	{
		if (GlobalAtmosphereData == null)
		{
			return false;
		}
		return true;
	}

	public override void Initialize(ModAbout mod)
	{
		Deprecated |= Hidden;
		PreviewButton?.Load();
		foreach (WeatherEvent weatherEvent in WeatherEvents)
		{
			weatherEvent.Initialize(mod);
		}
		GlobalAtmosphereData?.Init();
		TerrainSettings?.Initialize(mod);
		foreach (MinablesGenerationData minablesDatum in MinablesData)
		{
			minablesDatum.Initialize(mod);
		}
		foreach (DeepMinablesGenerationData deepMinablesDatum in DeepMinablesData)
		{
			deepMinablesDatum.Initialize(mod);
		}
		foreach (PlayableAreaData playableAreaData in PlayableAreaDatas)
		{
			playableAreaData.Initialize(mod);
		}
		foreach (StartLocationData startLocationData in StartLocationDatas)
		{
			startLocationData.Initialize(mod);
		}
	}

	public static WorldSettingData Find(string id)
	{
		return DataCollection.Get<WorldSettingData>(id);
	}

	public static WorldSettingData Find(WorldSettingData loadedSettings)
	{
		WorldSettingData worldSettingData = Find(loadedSettings.Id);
		if (worldSettingData != null)
		{
			return worldSettingData;
		}
		return Find("Moon");
	}

	public static void SaveWorldSetting(string id, WorldSettingData data)
	{
		WorldManager.GameData gameData = new WorldManager.GameData();
		data.Id = id;
		gameData.WorldSettings.Add(data);
		string text = Application.streamingAssetsPath + "\\Data\\" + id + ".xml";
		gameData.SaveXml(text);
		ConsoleWindow.PrintAction("Saved " + text + " successfully");
	}

	public StartLocationData SelectStartLocation()
	{
		if (StartLocationDatas == null || StartLocationDatas.Count == 0)
		{
			return DataCollection.Get<StartLocationData>("DefaultStartLocation");
		}
		StartLocationData startLocationData = StartLocationDatas.Pick();
		while (startLocationData is RoundRobinStartLocationData)
		{
			startLocationData = StartLocationDatas.Pick();
		}
		return DataCollection.Get<StartLocationData>(startLocationData.Id);
	}

	public static async UniTaskVoid SaveNewScenarioWorld(string id, bool isTutorial = false)
	{
		GameManager.PauseGameTick();
		while (!GameManager.GameTickPaused)
		{
			ConsoleWindow.Print("...Waiting for game tick to complete...");
			await UniTask.Delay(30);
			if (GameManager.GameState == GameState.None || Singleton<GameManager>.IsQuitting)
			{
				ConsoleWindow.Print("Process Halted");
				return;
			}
		}
		ConsoleWindow.Print($"Saving {OcclusionManager.AllThings.ActiveCount} things");
		WorldManager.GameData gameData = new WorldManager.GameData();
		WorldSettingData worldSettingData = WorldSetting.Current.Data.DeepCopy();
		if (isTutorial)
		{
			worldSettingData.IsTutorial = new BoolReference
			{
				Value = true
			};
			worldSettingData.WorldObjectiveCollections = new List<WorldObjectiveCollection>();
		}
		worldSettingData.Id = id;
		_savedNetworkAtmopsheres.Clear();
		_thingSpawns = new SpawnData
		{
			Id = id + "_Things"
		};
		SpawnData item = new SpawnData
		{
			Id = id + "_Things",
			EventType = SpawnEvent.NewWorld,
			HideInStartScreen = true
		};
		OcclusionManager.AllThings.ForEach(SaveThingAction);
		ConsoleWindow.Print($"Saving {AtmosphericsManager.AllAtmospheres.ActiveCount} World Atmospheres");
		_atmosphereSpawns = new SpawnData
		{
			Id = id + "_WorldAtmospheres"
		};
		SpawnData item2 = new SpawnData
		{
			Id = id + "_WorldAtmospheres",
			EventType = SpawnEvent.NewWorld,
			HideInStartScreen = true
		};
		AtmosphericsManager.AllAtmospheres.ForEach(SaveAtmosphereAction);
		worldSettingData.StartConditionDatas.Clear();
		worldSettingData.StartConditionDatas.Add(new StartConditionData
		{
			Id = "DefaultScenario",
			IsDefault = true
		});
		worldSettingData.SpawnDatas.Clear();
		worldSettingData.SpawnDatas.Add(item);
		worldSettingData.SpawnDatas.Add(item2);
		gameData.WorldSettings.Add(worldSettingData);
		string text = (isTutorial ? "Tutorial" : "Scenario");
		string text2 = Application.streamingAssetsPath + "\\Data\\" + text + "_" + id + ".xml";
		string text3 = Application.streamingAssetsPath + "\\Data\\" + text + "_" + id + "_Spawns.xml";
		gameData.SaveXml(text2);
		XDocument xDocument = new XDocument(new XDeclaration("1.0", "utf-8", null));
		XElement parentElement = new XElement("GameData");
		xDocument.Add(parentElement);
		_thingSpawns.Add(ref parentElement, "Spawn");
		_atmosphereSpawns.Add(ref parentElement, "Spawn");
		xDocument.Save(text3);
		GameManager.UnpauseGameTick();
		ConsoleWindow.PrintAction("Exported " + text + " successfully " + text2);
		ConsoleWindow.PrintAction("Exported " + text + " successfully " + text3);
	}

	public static void ResolveAll()
	{
		throw new NotImplementedException();
	}
}
