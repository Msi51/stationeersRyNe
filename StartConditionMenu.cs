using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartConditionMenu : ManagerBase
{
	[SerializeField]
	private StartConditionButtonItem startConditionButtonItemPrefab;

	[SerializeField]
	private Transform _startConditionItemParent;

	[SerializeField]
	private Button _startGameButton;

	[SerializeField]
	public TMP_InputField _worldNameInput;

	public static StartConditionButtonItem SelectedStartCondition;

	public static List<StartConditionButtonItem> AllStartConditionButtons = new List<StartConditionButtonItem>();

	public NewWorldStartCondition StartConditionDescription;

	public static StartConditionMenu Instance;

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		_startGameButton.onClick.AddListener(MainMenu.StartGame);
		Instance = this;
	}

	private void OnEnable()
	{
		_worldNameInput.text = NewWorldMenu.SelectedWorld.WorldSetting.Id;
		StartConditionDescription.RefreshPanelAsync().Forget();
	}

	public static void ClearButtons()
	{
		for (int num = AllStartConditionButtons.Count - 1; num >= 0; num--)
		{
			UnityEngine.Object.Destroy(AllStartConditionButtons[num].gameObject);
		}
		AllStartConditionButtons.Clear();
	}

	public void PopulateStartConditionsList(WorldSetting worldSetting)
	{
		ClearButtons();
		foreach (StartConditionData startConditionData2 in worldSetting.Data.StartConditionDatas)
		{
			StartConditionButtonItem startConditionButtonItem = UnityEngine.Object.Instantiate(startConditionButtonItemPrefab, _startConditionItemParent);
			startConditionButtonItem.Initialize(startConditionData2);
			startConditionButtonItem.OnClick = (Action<StartConditionButtonItem>)Delegate.Combine(startConditionButtonItem.OnClick, new Action<StartConditionButtonItem>(OnStartConditionButtonClicked));
			AllStartConditionButtons.Add(startConditionButtonItem);
		}
		if (AllStartConditionButtons.Count == 0)
		{
			StartConditionData startConditionData = DataCollection.Get<StartConditionData>("Default");
			StartConditionButtonItem startConditionButtonItem2 = UnityEngine.Object.Instantiate(startConditionButtonItemPrefab, _startConditionItemParent);
			startConditionButtonItem2.Initialize(startConditionData);
			startConditionButtonItem2.OnClick = (Action<StartConditionButtonItem>)Delegate.Combine(startConditionButtonItem2.OnClick, new Action<StartConditionButtonItem>(OnStartConditionButtonClicked));
			AllStartConditionButtons.Add(startConditionButtonItem2);
		}
		foreach (StartConditionData allStartCondition in StartConditionData.AllStartConditions)
		{
			if (allStartCondition.WorldInjection != null && allStartCondition.WorldInjection.Evaluate(worldSetting) && !HasStartConditionData(allStartCondition))
			{
				StartConditionButtonItem startConditionButtonItem3 = UnityEngine.Object.Instantiate(startConditionButtonItemPrefab, _startConditionItemParent);
				startConditionButtonItem3.Initialize(allStartCondition);
				startConditionButtonItem3.OnClick = (Action<StartConditionButtonItem>)Delegate.Combine(startConditionButtonItem3.OnClick, new Action<StartConditionButtonItem>(OnStartConditionButtonClicked));
				AllStartConditionButtons.Add(startConditionButtonItem3);
			}
		}
		SelectStartCondition(AllStartConditionButtons[0]);
	}

	private bool HasStartConditionData(StartConditionData startConditionData)
	{
		foreach (StartConditionButtonItem allStartConditionButton in AllStartConditionButtons)
		{
			if (allStartConditionButton.StartCondition.IdHash == startConditionData.IdHash)
			{
				return true;
			}
		}
		return false;
	}

	public void SetWorld(WorldSetting worldSetting)
	{
		PopulateStartConditionsList(worldSetting);
	}

	private void OnStartConditionButtonClicked(StartConditionButtonItem item)
	{
		SelectStartCondition(item);
		StartConditionDescription.RefreshPanelAsync().Forget();
	}

	private void SelectStartCondition(StartConditionButtonItem item)
	{
		_startGameButton.interactable = true;
		if (SelectedStartCondition == null)
		{
			SelectedStartCondition = item;
			SelectedStartCondition.ShowHighlight(show: true);
		}
		else
		{
			SelectedStartCondition.ShowHighlight(show: false);
			SelectedStartCondition = item;
			SelectedStartCondition.ShowHighlight(show: true);
		}
		StartConditionDescription.SetStartCondition(SelectedStartCondition.StartCondition.IdHash);
	}
}
