using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts.Util;

public class RecipeProcessingDataComparable : RecipeComparable
{
	private List<WorldManager.ProcessingData> _recipeDataList = new List<WorldManager.ProcessingData>();

	private List<WorldManager.ProcessingData> _initailRecipeDataList = new List<WorldManager.ProcessingData>();

	public override void GenerateRecipeHash()
	{
		int num;
		using (StringWriter stringWriter = new StringWriter())
		{
			Serializers.ProcessingData.Serialize(stringWriter, _recipeDataList);
			num = Animator.StringToHash(stringWriter.ToString());
		}
		CurrentRecipeHash = Animator.StringToHash($"{CurrentRecipeHash}{num}");
	}

	public virtual bool AddRecipe(WorldManager.ProcessingData recipe, ModAbout mod)
	{
		_recipeDataList.Add(recipe);
		return true;
	}

	public virtual void RemoveRecipe(WorldManager.ProcessingData recipe)
	{
		_recipeDataList.Remove(recipe);
	}

	public override bool UpdatedRecipe(string compressedRecipe)
	{
		string path = StringCompressor.DecompressString(compressedRecipe);
		if (!(XmlSerialization.Deserialize(Serializers.ProcessingData, path) is List<WorldManager.ProcessingData> list))
		{
			return false;
		}
		if (list == null || list.Count <= 0)
		{
			return false;
		}
		ClearRecipe();
		foreach (WorldManager.ProcessingData item in list)
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
			Serializers.ProcessingData.Serialize(stringWriter, _recipeDataList);
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
