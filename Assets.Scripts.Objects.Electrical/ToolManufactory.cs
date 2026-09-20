using System.Collections.Generic;
using Assets.Scripts.Util;
using Reagents;

namespace Assets.Scripts.Objects.Electrical;

public class ToolManufactory : SimpleFabricatorBase
{
	private bool isDone;

	private Dictionary<MachineTier, List<DynamicThing>> _DynamicThings = new Dictionary<MachineTier, List<DynamicThing>>();

	private List<DynamicThing> _validDynamicThings = new List<DynamicThing>();

	public static DynamicThingRecipeComparable RecipeComparable = new DynamicThingRecipeComparable("ToolManufactory");

	public override Dictionary<DynamicThing, Recipe> Recipes => RecipeComparable.AllRecipes;

	public override Dictionary<MachineTier, List<DynamicThing>> DynamicThings
	{
		get
		{
			if (!isDone)
			{
				PopulateDynamicThingsDict(ref _DynamicThings);
				isDone = true;
			}
			return _DynamicThings;
		}
	}

	public override List<DynamicThing> ValidDynamicThings
	{
		get
		{
			if (base.NeedsValidate)
			{
				RefreshDynamicThings();
			}
			return _validDynamicThings;
		}
	}

	public override Dictionary<int, int> PrefabTypeLookup => RecipeComparable.PrefabTypeLookup;

	public override void Start()
	{
		base.Start();
	}

	public override void RefreshDynamicThings()
	{
		if (_validDynamicThings != null)
		{
			_validDynamicThings.Clear();
		}
		GetValidDynamicThings(ref _validDynamicThings);
		base.NeedsValidate = false;
	}

	public void PopulateDynamicThingsDict(ref Dictionary<MachineTier, List<DynamicThing>> _DynamicThings)
	{
		foreach (DynamicThing dynamicThing in RecipeComparable.DynamicThings)
		{
			if (!_DynamicThings.ContainsKey(dynamicThing.RecipeTier))
			{
				_DynamicThings[dynamicThing.RecipeTier] = new List<DynamicThing>();
				_DynamicThings[dynamicThing.RecipeTier].Add(dynamicThing);
			}
			else
			{
				_DynamicThings[dynamicThing.RecipeTier].Add(dynamicThing);
			}
		}
	}
}
