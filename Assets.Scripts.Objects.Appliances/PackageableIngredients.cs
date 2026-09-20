using System.Collections.Generic;
using Reagents;

namespace Assets.Scripts.Objects.Appliances;

public class PackageableIngredients
{
	public static List<IPackageableIngredient> AllIngredients = new List<IPackageableIngredient>();

	public static bool CanProcess(Recipe recipe)
	{
		foreach (IPackageableIngredient allIngredient in AllIngredients)
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
		foreach (IPackageableIngredient allIngredient in AllIngredients)
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
		foreach (IPackageableIngredient allIngredient in AllIngredients)
		{
			if (!(allIngredient is Item item) || list.Contains(item))
			{
				continue;
			}
			foreach (KeyValuePair<Recipe, Item> allRecipe in BasicPackagingMachine.RecipeComparable.AllRecipes)
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
