using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Networks;
using Objects.Rockets;
using Steamworks;
using TerrainSystem;
using UnityEngine;

public class WorldSetting : SettingBase
{
	public static List<WorldSetting> AllWorldSettings = new List<WorldSetting>();

	public Material SkyBox;

	public GameObject Sun;

	public StartConditionData StartConditionData;

	public StartLocationData StartLocationData;

	public WorldSettingData Data;

	public List<PlanetPrefab> PlanetPrefabs = new List<PlanetPrefab>();

	public PreviewScene PreviewScene = new PreviewScene();

	private static readonly Dictionary<int, WorldSetting> _worldSettingLookup = new Dictionary<int, WorldSetting>();

	[XmlIgnore]
	private int _hash;

	private IEnumerable _spawnDatas;

	private const string DEFAULT_SPAWN = "Default";

	private const string DEFAULT_NEW_PLAYER_KIT = "DefaultNewPlayer";

	private const string DEFAULT_RESPAWN_PLAYER_KIT = "DefaultRespawnPlayer";

	public Texture2D PreviewButton => Data.PreviewButton.Texture;

	public CelestialConstantsReference CelestialConstants => Data.CelestialConstants;

	public PlayableBodyReference PlayableBody => Data.PlayableBody;

	public PrimaryBodyReference PrimaryBody => Data.PrimaryBody;

	public AmbientLightingReference AmbientLighting => Data.AmbientLighting;

	public CelestialCollection CelestialBodies => Data.CelestialBodies;

	public LocalizedStringReference Name => Data.Name;

	public LocalizedStringReference Description => Data.Description;

	public LocalizedStringReference Rating => Data.Rating;

	public LocalizedStringReference SummaryText => Data.SummaryText;

	public float Gravity => Data.Gravity;

	public List<WeatherEvent> WeatherEvents => Data.WeatherEvents;

	public bool IsHidden => Data.Hidden;

	public bool IsTutorial => Data.IsTutorial?.Value ?? false;

	public bool IsDeprecated => Data.Deprecated;

	public bool SetSunInSkybox => Data.SetSunInSkybox;

	public float MaxSunIntensity => Data.MaxSunIntensity;

	public bool HasLava => Data.TerrainSettings?.LavaData != null;

	public string AmbienceSound => Data.AmbienceSound;

	public int Seed => Data.Seed;

	public bool AtmosphericScattering => Data.AtmosphericScattering;

	public AtmosphericScatteringData AtmosphericScatteringData => Data.AtmosphericScatteringData;

	public float AmbientLightMin => Data.AmbientLightMin;

	public float AmbientLightMax => Data.AmbientLightMax;

	public Color LavaColor => Data.LavaColor;

	public bool HasCustomLavaColor => Data.HasCustomLavaColor;

	public SpaceMapData SpaceMapData => Data.SpaceMapData ?? DataCollection.Get<SpaceMapData>("FallBack");

	public AnimationCurveData SolarAngleTemperatureCurve => Data.GlobalAtmosphereData.SolarAngleTemperatureCurveData;

	public GlobalGasMixData GlobalGasMixData => Data.GlobalAtmosphereData.GlobalGasMixData;

	public List<StarData> Stars => Data.Stars;

	public static WorldSetting Current { get; private set; }

	public bool IsUnderLava(Vector3 worldPosition)
	{
		if (Data.TerrainSettings.LavaData != null)
		{
			return Data.TerrainSettings.LavaData.IsUnderLava(worldPosition);
		}
		return false;
	}

	public float GetLavaHeight(Vector3 worldPosition)
	{
		return Data.TerrainSettings.LavaData?.GetLavaHeight(worldPosition) ?? 0f;
	}

	public static void SetCurrent(int worldHash)
	{
		SetCurrent(Find(worldHash));
	}

	public static void SetCurrent(WorldSetting setting, StartConditionData startCondition = null, StartLocationData startLocationData = null)
	{
		Current = setting;
		if (startCondition != null)
		{
			Current.StartConditionData = startCondition;
		}
		if (startLocationData != null)
		{
			Current.StartLocationData = startLocationData;
		}
		WorldSetting current = Current;
		if (current.StartLocationData == null)
		{
			current.StartLocationData = setting.Data.SelectStartLocation();
		}
		ConsoleWindow.PrintAction("WorldSetting: " + setting.Id + " StartCondition: " + (startCondition?.Id ?? "default") + " StartLocation: " + (startLocationData?.Id ?? "default"));
		Current.Initialize();
		if (SteamClient.IsValid)
		{
			GameManager.SetSteamRichPresence("world", WorldManager.CurrentWorldName);
			GameManager.SetSteamRichPresence("day", StringManager.Get(WorldManager.DaysPast));
		}
		SkyBoxController.ApplyNewWorldSetting();
	}

	public static void Register(WorldSetting worldSetting)
	{
		int key = Animator.StringToHash(worldSetting.Id);
		if (_worldSettingLookup.ContainsKey(key))
		{
			ConsoleWindow.PrintError("error duplicate world setting found for '" + worldSetting.Id + "'");
			return;
		}
		_worldSettingLookup.Add(key, worldSetting);
		AllWorldSettings.Add(worldSetting);
	}

	public static void Reregister(WorldSetting worldSetting)
	{
		int key = Animator.StringToHash(worldSetting.Id);
		if (!_worldSettingLookup.ContainsKey(key))
		{
			ConsoleWindow.PrintError("error cannot update world setting reference for '" + worldSetting.Id + "' as it is not registered");
			return;
		}
		if (_worldSettingLookup.TryGetValue(key, out var value))
		{
			AllWorldSettings.Remove(value);
		}
		AllWorldSettings.Add(worldSetting);
		_worldSettingLookup[key] = worldSetting;
	}

	public static WorldSetting Find(string id)
	{
		return Find(Animator.StringToHash(id));
	}

	private static WorldSetting Find(int stringToHash)
	{
		if (_worldSettingLookup.TryGetValue(stringToHash, out var value))
		{
			return value;
		}
		return null;
	}

	public void Initialize()
	{
		_hash = Animator.StringToHash(Id);
		CelestialBodies.LoadData();
		foreach (MinablesGenerationData minablesDatum in Data.MinablesData)
		{
			minablesDatum.CalculateTotalWeight();
		}
	}

	public void DebugPrint()
	{
		ConsoleWindow.Print("Id: " + Id);
		ConsoleWindow.Print($"Name: {Name}");
		ConsoleWindow.Print($"Description: {Description}");
	}

	public int GetHash()
	{
		if (_hash == 0)
		{
			Initialize();
		}
		return _hash;
	}

	public static void StartWorld()
	{
		Current?.AmbientLighting.Apply();
		CameraController.SetAntialiasing();
	}

	public async UniTask SpawnOnNewWorld()
	{
		await ImGuiLoadingScreen.SetProgress(0f);
		if (StartConditionData == null)
		{
			StartConditionData = DataCollection.Get<StartConditionData>("Default");
		}
		int total = 0;
		int count = 0;
		foreach (SpawnData spawn in StartConditionData.Spawns)
		{
			total += spawn.GetCount();
		}
		foreach (SpawnData spawnData in Data.SpawnDatas)
		{
			total += spawnData.GetCount();
		}
		foreach (SpawnData spawn2 in StartConditionData.Spawns)
		{
			if (spawn2.EventType == SpawnEvent.NewWorld)
			{
				spawn2.Execute(null, InventoryManager.ParentHuman);
				count += spawn2.GetCount();
				await ImGuiLoadingScreen.SetProgress((float)count / (float)total);
			}
		}
		foreach (SpawnData spawnData2 in Data.SpawnDatas)
		{
			if (spawnData2.EventType == SpawnEvent.NewWorld)
			{
				spawnData2.Execute(null, InventoryManager.ParentHuman);
				count += spawnData2.GetCount();
				await ImGuiLoadingScreen.SetProgress((float)count / (float)total);
			}
		}
		for (int num = StructureNetwork.AllStructureNetworks.Count - 1; num >= 0; num--)
		{
			(StructureNetwork.AllStructureNetworks[num] as AtmosphericsNetwork)?.RefreshNetworkVolume();
		}
		await ImGuiLoadingScreen.SetProgress(1f);
	}

	private void Execute(SpawnEvent spawnEvent, DynamicThing parent, Human player)
	{
		foreach (SpawnData spawn in StartConditionData.Spawns)
		{
			if (spawn.EventType == spawnEvent)
			{
				spawn.Execute(parent, player);
			}
		}
		foreach (SpawnData spawnData in Data.SpawnDatas)
		{
			if (spawnData.EventType == spawnEvent)
			{
				spawnData.Execute(parent, player);
			}
		}
	}

	private int Count(SpawnEvent spawnEvent)
	{
		int num = 0;
		foreach (SpawnData spawn in StartConditionData.Spawns)
		{
			if (spawn.EventType == spawnEvent)
			{
				num++;
			}
		}
		foreach (SpawnData spawnData in Data.SpawnDatas)
		{
			if (spawnData.EventType == spawnEvent)
			{
				num++;
			}
		}
		return num;
	}

	public static void OnNewPlayer(Human player)
	{
		if (Current == null)
		{
			throw new NullReferenceException("OnNewPlayer Current world setting is null");
		}
		if (Current.Count(SpawnEvent.NewPlayer) != 0)
		{
			Current.Execute(SpawnEvent.NewPlayer, null, player);
		}
	}

	public static void OnNewPlayerKit(Human player)
	{
		if (Current == null)
		{
			throw new NullReferenceException("OnNewPlayerKit Current world setting is null");
		}
		if (Current.Count(SpawnEvent.NewPlayerKit) == 0)
		{
			DataCollection.Get<SpawnData>("DefaultNewPlayer").Execute(player, null);
		}
		else
		{
			Current.Execute(SpawnEvent.NewPlayerKit, player, player);
		}
	}

	public static void OnRespawnPlayerKit(Human player)
	{
		if (Current == null)
		{
			throw new NullReferenceException("OnRespawnPlayerKit Current world setting is null");
		}
		if (Current.Count(SpawnEvent.RespawnPlayerKit) == 0)
		{
			DataCollection.Get<SpawnData>("DefaultRespawnPlayer").Execute(player, null);
		}
		else
		{
			Current.Execute(SpawnEvent.RespawnPlayerKit, player, player);
		}
	}

	public static void OnRespawnPlayer(Human player)
	{
		if (Current == null)
		{
			throw new NullReferenceException("OnRespawnPlayer Current world setting is null");
		}
		if (Current.Count(SpawnEvent.RespawnPlayer) != 0)
		{
			Current.Execute(SpawnEvent.RespawnPlayer, null, player);
		}
	}

	public bool HasWeather()
	{
		return WeatherEvents.Count > 0;
	}
}
