using System.Collections.Generic;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Reagents;

namespace Assets.Scripts.Util;

public class OreRecipeComparable : RecipeDataComparable
{
	public Dictionary<Recipe, Ore> AllRecipes = new Dictionary<Recipe, Ore>();

	private Dictionary<Recipe, Ore> _initialAllRecipes = new Dictionary<Recipe, Ore>();

	public virtual Dictionary<Recipe, Ore> Recipes => AllRecipes;

	public OreRecipeComparable()
	{
	}

	public OreRecipeComparable(string recipeName)
	{
		RecipeName = recipeName;
	}

	public override bool AddRecipe(WorldManager.RecipeData recipe, ModAbout mod)
	{
		if (AllRecipes.ContainsKey(recipe.Recipe))
		{
			ConsoleWindow.PrintAction(GameStrings.PatchingRecipe.AsString(mod?.Name ?? ((string)GameStrings.UnknownMod), recipe.PrefabName));
			RemoveRecipe(recipe);
		}
		Ore ore = Prefab.Find(recipe.PrefabName) as Ore;
		if (!ore)
		{
			return false;
		}
		AllRecipes.Add(recipe.Recipe, ore);
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
