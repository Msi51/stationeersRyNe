using System.Collections.Generic;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Reagents;

namespace Assets.Scripts.Util;

public class ItemRecipeComparable : RecipeDataComparable
{
	public Dictionary<Recipe, Item> AllRecipes = new Dictionary<Recipe, Item>();

	public virtual Dictionary<Recipe, Item> Recipes => AllRecipes;

	public ItemRecipeComparable()
	{
	}

	public ItemRecipeComparable(string recipeName)
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
		Item item = Prefab.Find(recipe.PrefabName) as Item;
		if (!item)
		{
			return false;
		}
		AllRecipes.Add(recipe.Recipe, item);
		base.AddRecipe(recipe, mod);
		return true;
	}

	public override void RemoveRecipe(WorldManager.RecipeData recipe)
	{
		base.RemoveRecipe(recipe);
		AllRecipes.Remove(recipe.Recipe);
	}

	public override void ClearRecipe(bool resetInitialHash = false)
	{
		base.ClearRecipe(resetInitialHash);
		AllRecipes.Clear();
	}
}
