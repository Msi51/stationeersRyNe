using Assets.Scripts.Genetics;

namespace Assets.Scripts.Atmospherics;

public class GlobalPlantSaveData
{
	public string PrefabId;

	public GeneCollectionWrapper GeneCollectionWrapper;

	public int Count;

	public GlobalPlantSaveData()
	{
	}

	public GlobalPlantSaveData(GlobalPlant plant)
	{
		PrefabId = plant.PlantPrefab.PrefabName;
		Count = plant.Count;
		GeneCollectionWrapper = new GeneCollectionWrapper
		{
			PlantCustomName = plant.GeneCollection.PlantCustomName,
			PlanterCustomName = plant.GeneCollection.PlanterCustomName
		};
		foreach (GeneWrapper value in plant.GeneCollection.Lookup.Values)
		{
			GeneCollectionWrapper.GeneWrappers.Add(new GeneWrapper(value));
		}
	}
}
