using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;

namespace ThingImport;

public class SeedPlantResolutionTask : DataResolutionTask
{
	private Seed _seed;

	private PrefabReference _plantRef;

	public SeedPlantResolutionTask(Seed seed, PrefabReference plantRef)
	{
		_seed = seed;
		_plantRef = plantRef;
	}

	public override void Resolve()
	{
		if (!Prefab.TryFind(_plantRef.Id, out Plant thing))
		{
			ConsoleWindow.PrintError("Can't resolve relationship for seed " + _seed.DisplayName + ", plant " + _plantRef.Id);
		}
		else
		{
			_seed.PlantType = thing;
		}
	}
}
