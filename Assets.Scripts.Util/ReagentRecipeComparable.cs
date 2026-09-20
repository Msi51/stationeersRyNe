using System.Collections.Generic;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Util;

public class ReagentRecipeComparable : RecipeProcessingDataComparable
{
	public static Dictionary<int, Item> AllRecipes = new Dictionary<int, Item>();

	private static Dictionary<int, Item> _initialAllRecipes = new Dictionary<int, Item>();

	public static Dictionary<int, WorldManager.ProcessingData> RecipeData = new Dictionary<int, WorldManager.ProcessingData>();

	public virtual Dictionary<int, Item> Recipes => AllRecipes;

	public ReagentRecipeComparable()
	{
	}

	public ReagentRecipeComparable(string recipeName)
	{
		RecipeName = recipeName;
	}

	public override bool AddRecipe(WorldManager.ProcessingData recipe, ModAbout mod)
	{
		int key = Animator.StringToHash(recipe.InputPrefab);
		if (AllRecipes.ContainsKey(key))
		{
			ConsoleWindow.PrintAction(GameStrings.PatchingRecipe.AsString(mod?.Name ?? ((string)GameStrings.UnknownMod), recipe.InputPrefab));
			RemoveRecipe(recipe);
		}
		Item item = Prefab.Find(recipe.OutputPrefab) as Item;
		if (!item)
		{
			return false;
		}
		AllRecipes.Add(key, item);
		RecipeData.Add(key, recipe);
		base.AddRecipe(recipe, mod);
		return true;
	}

	public override void RemoveRecipe(WorldManager.ProcessingData recipe)
	{
		base.RemoveRecipe(recipe);
		int key = Animator.StringToHash(recipe.InputPrefab);
		AllRecipes.Remove(key);
		RecipeData.Remove(key);
	}

	public override void RecoverRecipe()
	{
		ClearRecipe();
		AllRecipes.AddRange(_initialAllRecipes);
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
	}

	public override void ClearRecipe(bool resetInitialHash = false)
	{
		base.ClearRecipe(resetInitialHash);
		AllRecipes.Clear();
		RecipeData.Clear();
	}
}
