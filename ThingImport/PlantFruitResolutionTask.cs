using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;

namespace ThingImport;

public class PlantFruitResolutionTask : DataResolutionTask
{
	private Plant _plant;

	private PrefabReference _fruitRef;

	public PlantFruitResolutionTask(Plant plant, PrefabReference fruitRef)
	{
		_plant = plant;
		_fruitRef = fruitRef;
	}

	public override void Resolve()
	{
		Thing thing;
		if (_fruitRef == null)
		{
			_plant.FruitObject = _plant.SourcePrefab;
		}
		else if (!Prefab.TryFind<Thing>(_fruitRef.Id, out thing))
		{
			ConsoleWindow.PrintError("Can't resolve relationship for plant " + _plant.DisplayName + ", fruit " + _fruitRef.Id);
		}
		else
		{
			_plant.FruitObject = thing;
		}
	}
}
