using System.Collections.Generic;
using Reagents;

namespace Assets.Scripts.Objects.Appliances;

public static class MicrowaveIngredients
{
	public static readonly List<IMicrowaveIngredient> AllIngredients = new List<IMicrowaveIngredient>();

	public static bool CanProcess(Recipe recipe)
	{
		foreach (IMicrowaveIngredient allIngredient in AllIngredients)
		{
			if (allIngredient is Item item && item.CreatedReagentMixture.ContainsSome(recipe))
			{
				return true;
			}
		}
		return false;
	}

	public static bool CanProcess(Reagent reagentType)
	{
		foreach (IMicrowaveIngredient allIngredient in AllIngredients)
		{
			if (allIngredient is Item item && item.CreatedReagentMixture.Contains(reagentType))
			{
				return true;
			}
		}
		return false;
	}

	public static List<Item> GetResourcesUsed()
	{
		List<Item> list = new List<Item>(AllIngredients.Count);
		foreach (IMicrowaveIngredient allIngredient in AllIngredients)
		{
			Item item = Prefab.Find(allIngredient.GetPrefabHash()) as Item;
			if (item == null || list.Contains(item))
			{
				continue;
			}
			foreach (KeyValuePair<Recipe, Item> allRecipe in Microwave.RecipeComparable.AllRecipes)
			{
				if (item.CreatedReagentMixture.ContainsSome(allRecipe.Key))
				{
					list.Add(item);
					break;
				}
			}
		}
		return list;
	}
}
