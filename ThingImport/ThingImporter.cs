using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using UnityEngine;

namespace ThingImport;

public class ThingImporter : ManagerBase
{
	[Header("Plant Templates")]
	[SerializeField]
	private PlantTemplate _plantTemplateBasic;

	[SerializeField]
	private PlantTemplate _plantTemplateAnimalFood;

	[SerializeField]
	private PlantTemplate _plantTemplateMicrowaveIngredient;

	[SerializeField]
	private PlantTemplate _plantTemplateChemistryIngredient;

	[SerializeField]
	private PlantTemplate _plantTemplateAnimalFoodMicrowaveIngredient;

	[Header("Other Templates")]
	[SerializeField]
	private PlantStageTemplate _plantStageTemplate;

	[SerializeField]
	private SeedTemplate _seedTemplate;

	[SerializeField]
	private BlueprintTemplate _blueprintTemplate;

	[Header("Materials")]
	[SerializeField]
	private Material _plantTemplateMaterial;

	[SerializeField]
	private Material _seedTemplateMaterial;

	[SerializeField]
	private Material _plantStageTemplateMaterial;

	public static ThingImporter Instance;

	public Material PlantStageTemplateMaterial => _plantStageTemplateMaterial;

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		Instance = this;
	}

	public void CreateBlueprint(CustomThingData data)
	{
		BlueprintTemplate blueprintTemplate = Object.Instantiate(_blueprintTemplate, Vector3.zero, Quaternion.identity, InventoryManager.Instance.DynamicCursorParentTransform);
		if (!DataCollection.TryGet<BlueprintData>(data.BlueprintData.Id, out var data2))
		{
			ConsoleWindow.PrintError("Blueprint " + data.BlueprintData.Id + " not found. Will use default blueprint.");
		}
		data.BlueprintData = data2;
		blueprintTemplate.Initialize(data.BlueprintData);
		blueprintTemplate.name = data.Name + "_cursor";
		blueprintTemplate.tag = "Cursor";
		blueprintTemplate.gameObject.SetActive(value: false);
		InventoryManager.Instance.AddDynamicThingCursor(data.Name, blueprintTemplate.gameObject);
		Object.Destroy(blueprintTemplate);
	}

	public Plant CreatePlant(CustomPlantData plantData)
	{
		PlantTemplate plantTemplate = Object.Instantiate((plantData.AnimalFood && plantData.MicrowaveIngredient) ? _plantTemplateAnimalFoodMicrowaveIngredient : (plantData.AnimalFood ? _plantTemplateAnimalFood : (plantData.MicrowaveIngredient ? _plantTemplateMicrowaveIngredient : (plantData.ChemistryIngredient ? _plantTemplateChemistryIngredient : _plantTemplateBasic))), Vector3.zero, Quaternion.identity, Prefab.PrefabsGameObject.transform);
		Material material = Object.Instantiate(_plantTemplateMaterial);
		plantTemplate.Initialize(plantData, material);
		Plant plant = plantTemplate.Plant;
		plant.name = plantData.Name;
		plant.PrefabName = plantData.Name;
		plant.PrefabHash = Animator.StringToHash(plantData.Name);
		plant.Thumbnail = plantData.ThumbnailRef.Sprite;
		plant.SetIsPerennial(plantData.PerennialData != null);
		plant.MoodBonusValue = plantData.MoodBonus;
		plant.HarvestQuantityMax = plantData.HarvestQuantityMax;
		plant.GenerateSpawnGases(plantData.AlcoholPerUnit);
		plant.NutritionValue = plantData.Nutrition;
		plant.MaxQuantity = plantData.StackQuantityMax;
		plant.ThermalPlantEnergy = plantData.ThermalEnergy;
		plant.AddsToWater = plantData.AddsToWater;
		if (plantData.ReagentData == null)
		{
			plant.CreatedReagentMixture.Initialize();
		}
		else
		{
			plant.CreatedReagentMixture = plantData.ReagentData.ToReagentMixture();
		}
		plant.LifeRequirementsId = plantData.LifeRequirementsId;
		plant.GrowthStates.Clear();
		Material material2 = Object.Instantiate(_plantStageTemplateMaterial);
		PlantStage plantStage = new PlantStage();
		plantStage.Visualizer = plantTemplate.MeshRenderer;
		plantStage.Length = 1f;
		plant.GrowthStates.Add(plantStage);
		foreach (PlantStageData stage in plantData.Stages)
		{
			PlantStageTemplate plantStageTemplate = Object.Instantiate(_plantStageTemplate, Vector3.zero, Quaternion.identity, plant.Transform);
			plantStageTemplate.Initialize(stage, plantData, material2);
			plantStageTemplate.gameObject.SetActive(value: false);
			PlantStage plantStage2 = new PlantStage();
			plantStage2.Visualizer = plantStageTemplate.Renderer;
			plantStage2.Length = stage.Length;
			plantStage2.Mature = stage.Mature;
			plantStage2.Seed = stage.Seeding;
			plantStage2.Dead = stage.Dead;
			plant.GrowthStates.Add(plantStage2);
			Object.Destroy(plantStageTemplate);
		}
		plant.IsCustomThing = true;
		RegisterThing(plant);
		Object.Destroy(plantTemplate);
		return plant;
	}

	public Seed CreateSeed(CustomSeedData data)
	{
		SeedTemplate seedTemplate = Object.Instantiate(_seedTemplate, Vector3.zero, Quaternion.identity, Prefab.PrefabsGameObject.transform);
		Material material = Object.Instantiate(_seedTemplateMaterial);
		seedTemplate.Initialize(data, material);
		Seed seed = seedTemplate.Seed;
		Object.Destroy(seedTemplate);
		seed.name = data.Name;
		seed.GenerateSpawnGases(data.AlcoholPerUnit);
		seed.PrefabName = data.Name;
		seed.PrefabHash = Animator.StringToHash(data.Name);
		seed.Thumbnail = data.ThumbnailRef.Sprite;
		seed.IsCustomThing = true;
		RegisterThing(seed);
		return seed;
	}

	private void RegisterThing(Thing thing)
	{
		Prefab.RegisterExisting(thing);
		WorldManager.Instance.SourcePrefabs.Add(thing);
		InventoryManager.DynamicThingPrefabs.Add(thing.PrefabName);
		if (thing is DynamicThing spawnable)
		{
			ImguiCreativeSpawnMenu.AddDynamicItem(spawnable);
		}
	}
}
