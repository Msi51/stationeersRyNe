using System;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using Reagents;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Motherboard;

public class ScreenConstructionJob : MonoBehaviour
{
	[NonSerialized]
	[ReadOnly]
	public Fabricator AssignedFabricator;

	public Button ButtonAdd;

	public Button ButtonRemove;

	public Button ButtonDelete;

	public Dropdown TypeList;

	public Text QuantityText;

	public int LastValue;

	[NonSerialized]
	[ReadOnly]
	public FabricatorJob JobReference;

	public static List<DynamicThing> DynamicThings = new List<DynamicThing>();

	public static List<Dropdown.OptionData> OptionData = new List<Dropdown.OptionData>();

	public static Dictionary<string, int> PrefabTypeLookup = new Dictionary<string, int>();

	public static Recipe GetRecipe(int index)
	{
		DynamicThing key = DynamicThings[index];
		Fabricator.RecipeComparable.AllRecipes.TryGetValue(key, out var value);
		return value;
	}

	public static Recipe GetRecipe(DynamicThing prefab)
	{
		if (prefab == null)
		{
			return default(Recipe);
		}
		Fabricator.RecipeComparable.AllRecipes.TryGetValue(prefab, out var value);
		return value;
	}

	public static int GetIndex(string value)
	{
		PrefabTypeLookup.TryGetValue(value, out var value2);
		return value2 + 1;
	}

	public static DynamicThing GetPrefab(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return null;
		}
		int value2 = -1;
		PrefabTypeLookup.TryGetValue(value, out value2);
		if (value2 < 0)
		{
			return null;
		}
		return DynamicThings[value2];
	}

	public static void GenerateRecipieList()
	{
		OptionData.Clear();
		DynamicThings.Clear();
		PrefabTypeLookup.Clear();
		foreach (KeyValuePair<DynamicThing, Recipe> allRecipe in Fabricator.RecipeComparable.AllRecipes)
		{
			DynamicThing key = allRecipe.Key;
			DynamicThings.Add(key);
		}
		OptionData.Add(new Dropdown.OptionData("None"));
		int num = 1;
		DynamicThings.Sort((DynamicThing x, DynamicThing y) => x.CompareTo(y));
		foreach (DynamicThing dynamicThing in DynamicThings)
		{
			OptionData.Add(new Dropdown.OptionData($"{dynamicThing.DisplayName}", dynamicThing.Thumbnail));
			PrefabTypeLookup.Add(dynamicThing.name, num - 1);
			num++;
		}
	}

	public void UpdateQuantity()
	{
		QuantityText.text = $"x {JobReference.Quantity}";
	}

	public void Awake()
	{
		if ((bool)TypeList)
		{
			TypeList.options = OptionData;
			TypeList.value = 0;
			TypeList.ReplaceRaycasters();
		}
	}
}
