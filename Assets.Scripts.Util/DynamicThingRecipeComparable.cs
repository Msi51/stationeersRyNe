using System.Collections.Generic;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Util;

public class DynamicThingRecipeComparable : RecipeDataComparable
{
	private Dictionary<int, int> _prefabTypeLookup = new Dictionary<int, int>();

	private List<DynamicThing> _dynamicThings = new List<DynamicThing>();

	public Dictionary<DynamicThing, Recipe> AllRecipes = new Dictionary<DynamicThing, Recipe>();

	private Dictionary<DynamicThing, Recipe> _initialAllRecipes = new Dictionary<DynamicThing, Recipe>();

	public virtual Dictionary<DynamicThing, Recipe> Recipes => AllRecipes;

	public virtual List<DynamicThing> DynamicThings => _dynamicThings;

	public virtual Dictionary<int, int> PrefabTypeLookup => _prefabTypeLookup;

	public DynamicThingRecipeComparable(string recipeName)
	{
		RecipeName = recipeName;
	}

	public override bool AddRecipe(WorldManager.RecipeData recipe, ModAbout mod)
	{
		DynamicThing dynamicThing = Prefab.Find(recipe.PrefabName) as DynamicThing;
		if (!dynamicThing)
		{
			dynamicThing = Resources.Load<DynamicThing>("Objects/" + recipe.PrefabName);
			if (!dynamicThing)
			{
				return false;
			}
		}
		if (recipe.RecipeTier != MachineTier.Undefined)
		{
			dynamicThing.RecipeTier = recipe.RecipeTier;
		}
		if (AllRecipes.ContainsKey(dynamicThing))
		{
			ConsoleWindow.PrintAction(GameStrings.PatchingRecipe.AsString(mod?.Name ?? ((string)GameStrings.UnknownMod), recipe.PrefabName));
			RemoveRecipe(recipe);
		}
		AllRecipes.Add(dynamicThing, recipe.Recipe);
		Recycler.AddRecycleRecipe(Animator.StringToHash(recipe.PrefabName), new ReagentMixture(recipe.Recipe));
		base.AddRecipe(recipe, mod);
		return true;
	}

	public override void RemoveRecipe(WorldManager.RecipeData recipe)
	{
		base.RemoveRecipe(recipe);
		DynamicThing dynamicThing = Prefab.Find(recipe.PrefabName) as DynamicThing;
		if ((bool)dynamicThing)
		{
			AllRecipes.Remove(dynamicThing);
		}
		Recycler.RemoveRecipe(Animator.StringToHash(recipe.PrefabName));
	}

	public override void RemoveRecipesFromRecycle()
	{
		foreach (int key in _prefabTypeLookup.Keys)
		{
			Recycler.RemoveRecipe(key);
		}
	}

	public override void ClearRecipe(bool resetInitialHash = false)
	{
		base.ClearRecipe(resetInitialHash);
		AllRecipes.Clear();
		if (resetInitialHash)
		{
			_initialAllRecipes.Clear();
		}
	}

	public override void RecoverRecipe()
	{
		RemoveRecipesFromRecycle();
		ClearRecipe();
		AllRecipes.AddRange(_initialAllRecipes);
		foreach (KeyValuePair<DynamicThing, Recipe> allRecipe in AllRecipes)
		{
			Recycler.AddRecycleRecipe(allRecipe.Key.PrefabHash, new ReagentMixture(allRecipe.Value));
		}
		GenerateRecipieList();
		base.RecoverRecipe();
	}

	public override void GenerateRecipieList()
	{
		if (InitialRecipeHash == 0)
		{
			_initialAllRecipes.Clear();
			_initialAllRecipes.AddRange(AllRecipes);
		}
		base.GenerateRecipieList();
		_dynamicThings.Clear();
		_prefabTypeLookup.Clear();
		foreach (KeyValuePair<DynamicThing, Recipe> allRecipe in AllRecipes)
		{
			DynamicThing key = allRecipe.Key;
			_dynamicThings.Add(key);
		}
		int num = 1;
		_dynamicThings.Sort((DynamicThing x, DynamicThing y) => x.CompareTo(y));
		foreach (DynamicThing dynamicThing in _dynamicThings)
		{
			_prefabTypeLookup.Add(dynamicThing.PrefabHash, num - 1);
			num++;
		}
	}
}
