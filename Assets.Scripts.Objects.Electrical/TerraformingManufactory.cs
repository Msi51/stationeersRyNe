using System.Collections.Generic;
using Assets.Scripts.Util;
using Reagents;

namespace Assets.Scripts.Objects.Electrical;

public class TerraformingManufactory : AnimComponentManufactory
{
	public static readonly DynamicThingRecipeComparable RecipeComparable = new DynamicThingRecipeComparable("TerraformingManufactory");

	private bool _isDone;

	private Dictionary<MachineTier, List<DynamicThing>> _dynamicThings = new Dictionary<MachineTier, List<DynamicThing>>();

	private List<DynamicThing> _validDynamicThings = new List<DynamicThing>();

	public override Dictionary<DynamicThing, Recipe> Recipes => RecipeComparable.AllRecipes;

	public override Dictionary<MachineTier, List<DynamicThing>> DynamicThings
	{
		get
		{
			if (!_isDone)
			{
				PopulateDynamicThingsDict(ref _dynamicThings);
				_isDone = true;
			}
			return _dynamicThings;
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

	public override void RefreshDynamicThings()
	{
		_validDynamicThings.Clear();
		GetValidDynamicThings(ref _validDynamicThings);
		base.NeedsValidate = false;
	}

	public override void GetValidDynamicThings(ref List<DynamicThing> list)
	{
		for (int i = 0; i < 4 && i <= (int)base.CurrentTier; i++)
		{
			if (DynamicThings.ContainsKey((MachineTier)i))
			{
				list.AddRange(DynamicThings[(MachineTier)i]);
			}
		}
		if ((bool)IconMaterial)
		{
			IconMaterial.transform.gameObject.SetActive(value: true);
		}
	}

	private static void PopulateDynamicThingsDict(ref Dictionary<MachineTier, List<DynamicThing>> dynamicThings)
	{
		foreach (DynamicThing dynamicThing in RecipeComparable.DynamicThings)
		{
			if (!dynamicThings.ContainsKey(dynamicThing.RecipeTier))
			{
				dynamicThings[dynamicThing.RecipeTier] = new List<DynamicThing> { dynamicThing };
			}
			else
			{
				dynamicThings[dynamicThing.RecipeTier].Add(dynamicThing);
			}
		}
	}

	protected override void SetIcon()
	{
		if ((bool)IconMaterial && !base.BeingDestroyed && base.CurrentBuildStateIndex >= 0 && ValidDynamicThings != null && base.CurrentIndex < ValidDynamicThings.Count && base.CurrentIndex >= 0)
		{
			IconMaterial.material.mainTexture = ValidDynamicThings[base.CurrentIndex].Thumbnail.texture;
			IconMaterial.material.SetTexture(SimpleFabricatorBase.EmissionMap, IconMaterial.material.mainTexture);
		}
	}
}
