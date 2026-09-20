using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;

namespace ThingImport;

public class PlantSeedResolutionTask : DataResolutionTask
{
	private Plant _plant;

	private PrefabReference _seedRef;

	public PlantSeedResolutionTask(Plant plant, PrefabReference seedRef)
	{
		_plant = plant;
		_seedRef = seedRef;
	}

	public override void Resolve()
	{
		if (_seedRef != null)
		{
			if (!Prefab.TryFind(_seedRef.Id, out Seed thing))
			{
				ConsoleWindow.PrintError("Can't resolve relationship for plant " + _plant.DisplayName + ", seed " + _seedRef.Id);
			}
			else
			{
				_plant.SeedObject = thing;
			}
		}
	}
}
