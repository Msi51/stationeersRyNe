using System;
using System.Collections.Generic;
using Assets.Scripts.Genetics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.Objects;

public static class Prefab
{
	public static class Organ
	{
		public static Brain Brain;

		public static Stomach Stomach;

		public static Lungs LungsHuman;

		public static Lungs LungsZirilian;

		public static Lungs LungsChicken;
	}

	public const string REAGENT_MIX_NAME = "ItemReagentMix";

	public static readonly Dictionary<int, Thing> _allPrefabs = new Dictionary<int, Thing>();

	public static readonly List<Thing> AllPrefabs = new List<Thing>();

	private const string EFFECT_LIGHT_TAG = "EffectLight";

	public static Human Character { get; private set; }

	public static GameObject PrefabsGameObject { get; private set; }

	public static event Action OnPrefabsLoaded;

	public static async UniTask LoadAll()
	{
		if (PrefabsGameObject != null)
		{
			return;
		}
		Clear();
		Thing._resourceLookup.Clear();
		DynamicThing.DynamicThingPrefabs.Clear();
		Item.ClearReplacements();
		Device.AllDevicePrefabs.Clear();
		Ore.AllOrePrefabs.Clear();
		Ice.AllIcePrefabs.Clear();
		Ingot.AllIngotPrefabs.Clear();
		FabricatorBase.AllFabricatorPrefabs.Clear();
		Cartridge.AllCartridgePrefabs.Clear();
		AirlockControlBase.AllAirlockEnabledPrefabs.Clear();
		LogicUnitBase.AllLogicPrefabs.Clear();
		Structure.AllStructurePrefabs.Clear();
		MicrowaveIngredients.AllIngredients.Clear();
		PackageableIngredients.AllIngredients.Clear();
		ChemistryStation.AllIngredients.Clear();
		PaintMixer.AllIngredients.Clear();
		Plant.AllPlantPrefabs.Clear();
		PrefabsGameObject = new GameObject("~Prefabs");
		UnityEngine.Object.DontDestroyOnLoad(PrefabsGameObject.gameObject);
		PrefabsGameObject.SetActive(value: false);
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenRegisteringPrefabs.DisplayString);
		int tot = WorldManager.Instance.SourcePrefabs.Count;
		int count = 0;
		foreach (Thing sourcePrefab in WorldManager.Instance.SourcePrefabs)
		{
			if (!(sourcePrefab == null))
			{
				try
				{
					Register(sourcePrefab);
					int num = count + 1;
					count = num;
					await ImGuiLoadingScreen.SetProgress((float)num / (float)tot);
				}
				catch (Exception ex)
				{
					Debug.LogError("ERROR: " + sourcePrefab.name + " | WITH: " + ex.Message, sourcePrefab.gameObject);
				}
			}
		}
		DynamicThing.DynamicThingPrefabs.Sort((DynamicThing x, DynamicThing y) => x.CompareTo(y));
		Prefab.OnPrefabsLoaded?.Invoke();
		foreach (Thing allPrefab in AllPrefabs)
		{
			allPrefab.OnAllPrefabsLoaded();
		}
		IRobotRepairer.Tooltip = "<color=yellow>Repair</color> using " + IRobotRepairer.Tooltip;
		ISolarRepairer.Tooltip = "<color=yellow>Repair</color> using " + ISolarRepairer.Tooltip;
		ISuitReparier.Tooltip = "<color=yellow>Repair</color> using " + ISuitReparier.Tooltip;
		LoadCorePrefabs();
	}

	public static void LoadCorePrefabs()
	{
		Character = Find<Human>("Character");
		Organ.Brain = Find<Brain>("OrganBrain");
		Organ.Stomach = Find<Stomach>("OrganStomach");
		Organ.LungsHuman = Find<Lungs>("OrganLungs");
		Organ.LungsZirilian = Find<Lungs>("OrganLungsZrilian");
		Organ.LungsChicken = Find<Lungs>("OrganLungsChicken");
	}

	public static void Clear()
	{
		_allPrefabs.Clear();
		AllPrefabs.Clear();
	}

	private static void Register(Thing sourcePrefab)
	{
		Thing thing = UnityEngine.Object.Instantiate(sourcePrefab, PrefabsGameObject.transform);
		thing.name = sourcePrefab.name;
		thing.ThingTransformPosition = Vector3.zero;
		thing.ThingTransform.rotation = Quaternion.identity;
		RegisterExisting(thing);
	}

	public static void RegisterExisting(Thing prefab)
	{
		if (prefab.PrefabHash != Animator.StringToHash(prefab.PrefabName))
		{
			Debug.LogError(prefab.PrefabName + " Has incorrect prefab hash!");
		}
		_allPrefabs.Add(prefab.PrefabHash, prefab);
		prefab.CacheStates();
		AllPrefabs.Add(prefab);
		prefab.Lights.Clear();
		Light[] componentsInChildren = prefab.GetComponentsInChildren<Light>(includeInactive: true);
		foreach (Light obj in componentsInChildren)
		{
			ThingLight thingLight = new ThingLight(obj, prefab);
			obj.shadowBias = 0.1f;
			if (!obj.CompareTag("EffectLight"))
			{
				prefab.Lights.Add(thingLight);
				thingLight.LayerMask = thingLight.Light.cullingMask;
			}
		}
		if (prefab.CompareTag("NotSpawnable"))
		{
			return;
		}
		if (prefab is DynamicThing dynamicThing)
		{
			DynamicThing.DynamicThingPrefabs.Add(dynamicThing);
			Item item = dynamicThing as Item;
			if (item != null)
			{
				ElectronicReader.AddToLookup(item);
				Ore ore = prefab as Ore;
				if ((bool)ore)
				{
					ElectronicReader.AddToLookup(ore);
					Ore.AllOrePrefabs.Add(ore);
					if (ore is Ice ice && !(ice is PureIce))
					{
						Ice.AllIcePrefabs.Add(ice);
					}
				}
				Ingot ingot = prefab as Ingot;
				if ((bool)ingot)
				{
					Ingot.AllIngotPrefabs.Add(ingot);
				}
				if (item is Cartridge item2)
				{
					Cartridge.AllCartridgePrefabs.Add(item2);
				}
				if (item is ISolarRepairer solarRepairer)
				{
					ISolarRepairer.Prefabs.Add(solarRepairer);
					if (!string.IsNullOrEmpty(ISolarRepairer.Tooltip))
					{
						ISolarRepairer.Tooltip += ", ";
					}
					ISolarRepairer.Tooltip += solarRepairer.ToTooltip();
				}
				if (item is IRobotRepairer robotRepairer)
				{
					IRobotRepairer.Prefabs.Add(robotRepairer);
					if (!string.IsNullOrEmpty(IRobotRepairer.Tooltip))
					{
						IRobotRepairer.Tooltip += ", ";
					}
					IRobotRepairer.Tooltip += robotRepairer.ToTooltip();
				}
				if (item is ISuitReparier suitReparier)
				{
					ISuitReparier.Prefabs.Add(suitReparier);
					if (!string.IsNullOrEmpty(ISuitReparier.Tooltip))
					{
						ISuitReparier.Tooltip += ", ";
					}
					ISuitReparier.Tooltip += suitReparier.ToTooltip();
				}
				if (item is Plant plant && !(plant is Seed))
				{
					Plant.AllPlantPrefabs.Add(plant);
				}
			}
		}
		if (prefab is Structure item3)
		{
			Structure.AllStructurePrefabs.Add(item3);
			Device device = prefab as Device;
			if ((bool)device)
			{
				for (int j = 0; j < device.OpenEnds.Count; j++)
				{
					Connection connection = device.OpenEnds[j];
					if ((object)connection.Parent == null)
					{
						connection.Parent = device;
					}
				}
				Device.AllDevicePrefabs.Add(device);
				if ((bool)(prefab as LogicUnitBase))
				{
					LogicUnitBase.AllLogicPrefabs.Add(device);
				}
			}
			if (prefab is IRocketComponent rocketComponent && !(rocketComponent is IRocketInternals { InternalCellType: RocketInternalCellType.None }))
			{
				IRocketComponent.AllRocketPrefabs.Add(rocketComponent);
			}
			if (prefab is FabricatorBase item4)
			{
				FabricatorBase.AllFabricatorPrefabs.Add(item4);
			}
			if (prefab is IAirlockDevice)
			{
				AirlockControlBase.AllAirlockEnabledPrefabs.Add(device);
			}
		}
		if (prefab is ISanitation item5)
		{
			ISanitation.AllSanitationPrefabs.Add(item5);
		}
		if (prefab is IConstructionKit creator)
		{
			ElectronicReader.AddToLookup(creator);
		}
		if (prefab is IMicrowaveIngredient item6)
		{
			MicrowaveIngredients.AllIngredients.Add(item6);
		}
		if (prefab is IPackageableIngredient item7)
		{
			PackageableIngredients.AllIngredients.Add(item7);
		}
		if (prefab is IChemistryIngredient item8)
		{
			ChemistryStation.AllIngredients.Add(item8);
		}
		if (prefab is IPaintMixerIngredient item9)
		{
			PaintMixer.AllIngredients.Add(item9);
		}
		if (prefab is IGenetics item10)
		{
			IGenetics.AllGeneticsList.Add(item10);
		}
		if (prefab is ITrading item11)
		{
			ITrading.AllTradingList.Add(item11);
		}
		prefab.OnPrefabLoad();
	}

	public static Thing Find(string prefabName)
	{
		TryFind(prefabName, out var thing);
		return thing;
	}

	public static Thing Find(int prefabHash)
	{
		TryFind(prefabHash, out var thing);
		return thing;
	}

	public static T Find<T>(int prefabHash) where T : Thing
	{
		TryFind(prefabHash, out T thing);
		return thing;
	}

	public static T Find<T>(string prefabName) where T : Thing
	{
		return Find<T>(Animator.StringToHash(prefabName));
	}

	public static bool TryFind(string prefabName, out Thing thing)
	{
		return TryFind(Animator.StringToHash(prefabName), out thing);
	}

	public static bool TryFind(int prefabHash, out Thing thing)
	{
		return _allPrefabs.TryGetValue(prefabHash, out thing);
	}

	public static bool TryFind<T>(string prefabName, out T thing) where T : Thing
	{
		return TryFind(Animator.StringToHash(prefabName), out thing);
	}

	public static bool TryFind<T>(int prefabHash, out T thing) where T : Thing
	{
		try
		{
			if (!TryFind(prefabHash, out var thing2))
			{
				throw new ArgumentException(string.Format("{0} not found. hash: {1}", "prefabHash", prefabHash));
			}
			if (!(thing2 is T val))
			{
				throw new InvalidCastException($"<b>{thing2.GetType()}</b> could not cast to <b>{typeof(T)}</b>");
			}
			thing = val;
			return thing;
		}
		catch (Exception)
		{
			thing = null;
			return false;
		}
	}
}
