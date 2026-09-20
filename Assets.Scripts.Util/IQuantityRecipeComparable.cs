using System.Collections.Generic;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Reagents;

namespace Assets.Scripts.Util;

public class IQuantityRecipeComparable : RecipeDataComparable
{
	public Dictionary<Recipe, IQuantity> AllRecipes = new Dictionary<Recipe, IQuantity>();

	private Dictionary<Recipe, IQuantity> _initialAllRecipes = new Dictionary<Recipe, IQuantity>();

	private Dictionary<Recipe, float> _recipeQuantities = new Dictionary<Recipe, float>();

	private Dictionary<Recipe, Recipe> _recipeReverseLookup = new Dictionary<Recipe, Recipe>();

	public virtual Dictionary<Recipe, IQuantity> Recipes => AllRecipes;

	public IQuantityRecipeComparable(string recipeName)
	{
		RecipeName = recipeName;
	}

	public float GetOutputScale(Recipe recipe)
	{
		_recipeQuantities.TryGetValue(recipe, out var value);
		return value;
	}

	public Recipe GetCleanRecipe(Recipe recipe)
	{
		_recipeReverseLookup.TryGetValue(recipe, out var value);
		return value;
	}

	public override bool AddRecipe(WorldManager.RecipeData recipe, ModAbout mod)
	{
		if (AllRecipes.ContainsKey(recipe.Recipe))
		{
			ConsoleWindow.PrintAction(GameStrings.PatchingRecipe.AsString(mod?.Name ?? ((string)GameStrings.UnknownMod), recipe.PrefabName));
			RemoveRecipe(recipe);
		}
		if (!(Prefab.Find(recipe.PrefabName) is IQuantity value))
		{
			return false;
		}
		AllRecipes.Add(recipe.Recipe, value);
		_recipeQuantities.Add(recipe.Recipe, recipe.Output);
		_recipeReverseLookup.Add(recipe.Recipe, recipe.Recipe);
		base.AddRecipe(recipe, mod);
		return true;
	}

	public override void RemoveRecipe(WorldManager.RecipeData recipe)
	{
		base.RemoveRecipe(recipe);
		AllRecipes.Remove(recipe.Recipe);
		_recipeQuantities.Remove(recipe.Recipe);
		_recipeReverseLookup.Remove(recipe.Recipe);
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
		_recipeQuantities.Clear();
		_recipeReverseLookup.Clear();
	}
}
