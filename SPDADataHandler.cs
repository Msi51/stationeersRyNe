using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;

public class SPDADataHandler
{
	public Dictionary<string, Dictionary<string, List<StationCategoryInsert>>> _listDictionary = new Dictionary<string, Dictionary<string, List<StationCategoryInsert>>>();

	public List<string> ListOfAllListOfObjects = new List<string>();

	public Dictionary<string, List<SPDAThingOverideData>> ThingOverrideData = new Dictionary<string, List<SPDAThingOverideData>>();

	public Dictionary<string, bool> HiddenInPedia = new Dictionary<string, bool>();

	public Dictionary<string, List<StationCategoryInsert>> GetList(string reference)
	{
		return _listDictionary[reference];
	}

	public void HandleThingPageOverrides()
	{
		foreach (List<SPDAThingOverideData> value in ThingOverrideData.Values)
		{
			foreach (SPDAThingOverideData item in value)
			{
				Thing thing = Prefab.Find(item.ThingName);
				if ((bool)thing)
				{
					HiddenInPedia[thing.PrefabName] = item.HideInSPDA;
					thing.HideInStationpedia = item.HideInSPDA;
				}
			}
		}
	}

	public void ClearAll()
	{
		_listDictionary.Clear();
		ListOfAllListOfObjects.Clear();
	}

	public void AddToAllLists(string key)
	{
		if (!ListOfAllListOfObjects.Contains(key))
		{
			ListOfAllListOfObjects.Add(key);
		}
	}

	public bool AddNewListItem(string pageKey, string category, StationCategoryInsert dat)
	{
		if (!_listDictionary.ContainsKey(pageKey))
		{
			_listDictionary[pageKey] = new Dictionary<string, List<StationCategoryInsert>>();
			if (!ListOfAllListOfObjects.Contains(pageKey))
			{
				ListOfAllListOfObjects.Add(pageKey);
			}
		}
		if (_listDictionary[pageKey].ContainsKey(category) && _listDictionary[pageKey][category] != null)
		{
			_listDictionary[pageKey][category].Add(dat);
		}
		else
		{
			_listDictionary[pageKey][category] = new List<StationCategoryInsert>();
			_listDictionary[pageKey][category].Add(dat);
		}
		return true;
	}
}
