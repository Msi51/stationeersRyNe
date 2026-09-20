using System.Collections.Generic;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Reagents;

namespace Assets.Scripts.Util;

public class IngotRecipeComparable : RecipeDataComparable
{
	public Dictionary<Recipe, Ingot> AllRecipes = new Dictionary<Recipe, Ingot>();

	private Dictionary<Recipe, Ingot> _initialAllRecipes = new Dictionary<Recipe, Ingot>();

	public virtual Dictionary<Recipe, Ingot> Recipes => AllRecipes;

	public IngotRecipeComparable(string recipeName)
	{
		RecipeName = recipeName;
	}

	public override bool AddRecipe(WorldManager.RecipeData recipe, ModAbout mod)
	{
		if (AllRecipes.ContainsKey(recipe.Recipe))
		{
			ConsoleWindow.PrintAction(GameStrings.PatchingRecipe.AsString(mod?.Name, recipe.PrefabName));
			RemoveRecipe(recipe);
		}
		Ingot ingot = Prefab.Find(recipe.PrefabName) as Ingot;
		if (!ingot)
		{
			return false;
		}
		AllRecipes.Add(recipe.Recipe, ingot);
		base.AddRecipe(recipe, mod);
		return true;
	}

	public override void RemoveRecipe(WorldManager.RecipeData recipe)
	{
		base.RemoveRecipe(recipe);
		AllRecipes.Remove(recipe.Recipe);
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
	}
}
