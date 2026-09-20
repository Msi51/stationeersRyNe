using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts.Util;

public class RecipeDataComparable : RecipeComparable
{
	private List<WorldManager.RecipeData> _recipeDataList = new List<WorldManager.RecipeData>();

	private List<WorldManager.RecipeData> _initailRecipeDataList = new List<WorldManager.RecipeData>();

	public override void GenerateRecipeHash()
	{
		int num;
		using (StringWriter stringWriter = new StringWriter())
		{
			Serializers.RecipeData.Serialize(stringWriter, _recipeDataList);
			num = Animator.StringToHash(stringWriter.ToString());
		}
		CurrentRecipeHash = Animator.StringToHash($"{CurrentRecipeHash}{num}");
	}

	public virtual bool AddRecipe(WorldManager.RecipeData recipe, ModAbout mod)
	{
		_recipeDataList.Add(recipe);
		return true;
	}

	public virtual void RemoveRecipe(WorldManager.RecipeData recipe)
	{
		_recipeDataList.Remove(recipe);
	}

	public virtual void RemoveRecipesFromRecycle()
	{
	}

	public override bool UpdatedRecipe(string compressedRecipe)
	{
		List<WorldManager.RecipeData> list;
		using (StringReader textReader = new StringReader(StringCompressor.DecompressString(compressedRecipe)))
		{
			list = Serializers.RecipeData.Deserialize(textReader) as List<WorldManager.RecipeData>;
		}
		if (list == null || list.Count <= 0)
		{
			return false;
		}
		RemoveRecipesFromRecycle();
		ClearRecipe();
		foreach (WorldManager.RecipeData item in list)
		{
			AddRecipe(item, null);
		}
		GenerateRecipieList();
		return true;
	}

	public override string GenerateCompressedRecipe()
	{
		if (CompressedRecipe != null)
		{
			return CompressedRecipe;
		}
		using (StringWriter stringWriter = new StringWriter())
		{
			Serializers.RecipeData.Serialize(stringWriter, _recipeDataList);
			CompressedRecipe = StringCompressor.CompressString(stringWriter.ToString());
		}
		return CompressedRecipe;
	}

	public override void ClearRecipe(bool resetInitialHash = false)
	{
		base.ClearRecipe(resetInitialHash);
		_recipeDataList.Clear();
		if (InitialRecipeHash == 0)
		{
			_initailRecipeDataList.Clear();
		}
	}

	public override void RecoverRecipe()
	{
		_recipeDataList.Clear();
		_recipeDataList.AddRange(_initailRecipeDataList);
	}

	public override void GenerateRecipieList()
	{
		if (InitialRecipeHash == 0)
		{
			_initailRecipeDataList.Clear();
			_initailRecipeDataList.AddRange(_recipeDataList);
		}
		base.GenerateRecipieList();
	}
}
