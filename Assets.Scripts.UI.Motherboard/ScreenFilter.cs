using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Motherboard;

public class ScreenFilter : MonoBehaviour
{
	[NonSerialized]
	[ReadOnly]
	public Sorter AssignedSorter;

	[NonSerialized]
	[ReadOnly]
	public FilterReference FilterReference;

	public Button ButtonDelete;

	public Dropdown TypeList;

	public int LastValue;

	public static List<Dropdown.OptionData> OptionData = new List<Dropdown.OptionData>();

	public static Dictionary<int, FilterReference> OptionLookup = new Dictionary<int, FilterReference>();

	public static Dictionary<Slot.Class, int> SlotTypeLookup = new Dictionary<Slot.Class, int>();

	public static Dictionary<string, int> PrefabTypeLookup = new Dictionary<string, int>();

	public static List<DynamicThing> DynamicThings = new List<DynamicThing>();

	public static void Clear()
	{
		OptionData.Clear();
		OptionLookup.Clear();
		SlotTypeLookup.Clear();
		PrefabTypeLookup.Clear();
		DynamicThings.Clear();
	}

	public static FilterReference GetFilterReference(int index)
	{
		OptionLookup.TryGetValue(index, out var value);
		return value;
	}

	public static int GetIndex(Slot.Class value)
	{
		SlotTypeLookup.TryGetValue(value, out var value2);
		return value2;
	}

	public static int GetIndex(string value)
	{
		PrefabTypeLookup.TryGetValue(value, out var value2);
		return value2;
	}

	public static void GenerateFilters()
	{
		foreach (Thing sourcePrefab in WorldManager.Instance.SourcePrefabs)
		{
			DynamicThing dynamicThing = sourcePrefab as DynamicThing;
			if (!(dynamicThing == null) && !dynamicThing.CompareTag("NotSpawnable") && dynamicThing.SlotType != Slot.Class.Wreckage)
			{
				DynamicThings.Add(dynamicThing);
			}
		}
		int num = 0;
		List<string> list = Enum.GetNames(typeof(Slot.Class)).ToList();
		list.Sort();
		foreach (string item in list)
		{
			Slot.Class obj = (Slot.Class)Enum.Parse(typeof(Slot.Class), item);
			OptionData.Add(new Dropdown.OptionData($"*{item.ToProper()}*"));
			OptionLookup.Add(num, new FilterReference(obj));
			SlotTypeLookup.Add(obj, num);
			num++;
		}
		DynamicThings.Sort((DynamicThing x, DynamicThing y) => x.CompareTo(y));
		foreach (DynamicThing dynamicThing2 in DynamicThings)
		{
			OptionData.Add(new Dropdown.OptionData($"{dynamicThing2.DisplayName}", dynamicThing2.Thumbnail));
			OptionLookup.Add(num, new FilterReference(dynamicThing2.name));
			PrefabTypeLookup.Add(dynamicThing2.name, num);
			num++;
		}
	}

	public void Awake()
	{
		TypeList.options = OptionData;
		TypeList.ReplaceRaycasters();
	}
}
