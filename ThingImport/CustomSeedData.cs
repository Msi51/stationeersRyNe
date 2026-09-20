using System.Xml.Serialization;

namespace ThingImport;

public class CustomSeedData : CustomThingData
{
	[XmlAttribute("AlcoholPerUnit")]
	public float AlcoholPerUnit;

	[XmlElement("Mesh")]
	public MeshReference MeshRef;

	[XmlElement("Texture")]
	public TextureReference TextureRef;

	[XmlElement("Thumbnail")]
	public SpriteReference ThumbnailRef;

	[XmlElement("Plant")]
	public PrefabReference PlantRef;

	public override void Initialize()
	{
		base.Initialize();
		MeshRef?.Load();
		TextureRef?.Load();
		ThumbnailRef?.Load();
	}

	public override void RegisterPrefab()
	{
		DataResolver.AddResolutionTask(new SeedPlantResolutionTask(ThingImporter.Instance.CreateSeed(this), PlantRef));
	}
}
