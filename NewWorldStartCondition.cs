using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class NewWorldStartCondition : UserInterfaceBase
{
	public TMP_Text StartConditionTitle;

	public TMP_Text SubTitlePrefab;

	public SpawnReference PrefabReferenceElement;

	public Transform ContentParent;

	public AdaptablePreferred AdaptablePreferred;

	private readonly List<SpawnReference> _createdSpawnReferences = new List<SpawnReference>(32);

	private readonly List<GameObject> _createdTitles = new List<GameObject>(32);

	public void SetStartCondition(int startConditionDataIdHash)
	{
		ClearElements();
		StartConditionData startConditionData = DataCollection.Get<StartConditionData>(startConditionDataIdHash);
		StartConditionTitle.text = startConditionData.Name;
		CreateSubHeading(GameStrings.StartScreenHeaderInWorldSpawns);
		foreach (SpawnData spawn in startConditionData.Spawns)
		{
			if (spawn.EventType == SpawnEvent.NewWorld)
			{
				GenerateUiElements(spawn, addChildren: true);
			}
		}
		foreach (SpawnData spawn2 in startConditionData.Spawns)
		{
			if (spawn2.EventType == SpawnEvent.NewPlayer)
			{
				GenerateUiElements(spawn2, addChildren: true);
			}
		}
		RefreshPanelAsync().Forget();
	}

	public async UniTaskVoid RefreshPanelAsync()
	{
		await UniTask.WaitForEndOfFrame();
		foreach (SpawnReference createdSpawnReference in _createdSpawnReferences)
		{
			createdSpawnReference.UpdateContentScale();
		}
		await UniTask.WaitForEndOfFrame();
		AdaptablePreferred.CalculateLayoutInputVertical();
	}

	private void CreateSubHeading(string text)
	{
		TMP_Text tMP_Text = Object.Instantiate(SubTitlePrefab, ContentParent);
		tMP_Text.text = text;
		_createdTitles.Add(tMP_Text.gameObject);
	}

	private void GenerateUiElements(SpawnData spawnData, bool addChildren)
	{
		if (!spawnData.IsValid())
		{
			spawnData = DataCollection.Get<SpawnData>(spawnData.IdHash);
		}
		if (spawnData.HideInStartScreen)
		{
			return;
		}
		if (!string.IsNullOrWhiteSpace(spawnData.StartScreenHeader))
		{
			CreateSubHeading(spawnData.StartScreenHeader);
		}
		foreach (DynamicSpawnData dynamicThing in spawnData.DynamicThings)
		{
			CreateElement(dynamicThing, dynamicThing.ExpandInStartScreen, addChildren);
		}
		foreach (DynamicSpawnData item in spawnData.Items)
		{
			CreateElement(item, item.ExpandInStartScreen, addChildren);
		}
		foreach (SpawnData spawn in spawnData.Spawns)
		{
			GenerateUiElements(spawn, addChildren);
		}
	}

	private void CreateElement(DynamicSpawnData dynamicSpawnData, bool expand, bool addChildren)
	{
		if (!dynamicSpawnData.HideInStartScreen)
		{
			SpawnReference item = SpawnReference.CreateInstance(dynamicSpawnData, PrefabReferenceElement, ContentParent, null, expand, addChildren);
			_createdSpawnReferences.Add(item);
		}
	}

	private void ClearElements()
	{
		for (int num = _createdSpawnReferences.Count - 1; num >= 0; num--)
		{
			Object.Destroy(_createdSpawnReferences[num].gameObject);
		}
		_createdSpawnReferences.Clear();
		for (int num2 = _createdTitles.Count - 1; num2 >= 0; num2--)
		{
			Object.Destroy(_createdTitles[num2]);
		}
		_createdTitles.Clear();
	}
}
