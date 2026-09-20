using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects.Items;

namespace ThingImport;

public class CustomPlantData : CustomItemData
{
	[XmlAttribute("StackQuantityMax")]
	public int StackQuantityMax;

	[XmlAttribute("HarvestQuantityMax")]
	public int HarvestQuantityMax;

	[XmlAttribute("Nutrition")]
	public float Nutrition;

	[XmlAttribute("MoodBonus")]
	public float MoodBonus;

	[XmlAttribute("AlcoholPerUnit")]
	public float AlcoholPerUnit;

	[XmlAttribute("ThermalEnergy")]
	public float ThermalEnergy;

	[XmlAttribute("AddsToWater")]
	public bool AddsToWater;

	[XmlAttribute("LifeRequirementsId")]
	public string LifeRequirementsId;

	[XmlAttribute("MicrowaveIngredient")]
	public bool MicrowaveIngredient;

	[XmlAttribute("ChemistryIngredient")]
	public bool ChemistryIngredient;

	[XmlAttribute("AnimalFood")]
	public bool AnimalFood;

	[XmlElement("Mesh")]
	public MeshReference MeshRef;

	[XmlElement("Texture")]
	public TextureReference TextureRef;

	[XmlElement("Thumbnail")]
	public SpriteReference ThumbnailRef;

	[XmlElement("Seed")]
	public PrefabReference SeedRef;

	[XmlElement("Fruit")]
	public PrefabReference FruitRef;

	[XmlElement("Perennial")]
	public PerennialData PerennialData;

	[XmlElement("Stage")]
	public List<PlantStageData> Stages = new List<PlantStageData>();

	public override void Initialize()
	{
		base.Initialize();
		MeshRef?.Load();
		TextureRef?.Load();
		ThumbnailRef?.Load();
		foreach (PlantStageData stage in Stages)
		{
			stage.Initialize();
		}
	}

	public override void RegisterPrefab()
	{
		Plant plant = ThingImporter.Instance.CreatePlant(this);
		DataResolver.AddResolutionTask(new PlantFruitResolutionTask(plant, FruitRef));
		DataResolver.AddResolutionTask(new PlantSeedResolutionTask(plant, SeedRef));
	}
}
