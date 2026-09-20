using System.Collections.Generic;
using System.IO;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Util;

public class RecyclerRecipeComparable : RecipeComparable
{
	public Dictionary<int, ReagentMixture> AllRecipes = new Dictionary<int, ReagentMixture>();

	private static float _recycleRatio = 0.5f;

	public RecyclerRecipeComparable()
	{
	}

	public RecyclerRecipeComparable(string recipeName)
	{
		RecipeName = recipeName;
	}

	public override void GenerateRecipeHash()
	{
		int num;
		using (StringWriter stringWriter = new StringWriter())
		{
			Serializers.ReagentMixture.Serialize(stringWriter, AllRecipes.Values);
			num = Animator.StringToHash(stringWriter.ToString());
		}
		CurrentRecipeHash = Animator.StringToHash($"{CurrentRecipeHash}{num}");
	}

	public bool AddRecycleRecipe(int hash, ReagentMixture contents, bool authored = false, float recycleRatioOverride = 0f)
	{
		if (AllRecipes.ContainsKey(hash))
		{
			if (authored)
			{
				AllRecipes[hash] = contents;
				return true;
			}
			return false;
		}
		AllRecipes.Add(hash, contents * ((recycleRatioOverride > 0f) ? recycleRatioOverride : _recycleRatio));
		return true;
	}

	public override void ClearRecipe(bool resetInitialHash = false)
	{
		base.ClearRecipe(resetInitialHash);
		AllRecipes.Clear();
	}
}
