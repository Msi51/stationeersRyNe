using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class WorldConfigurationMenu : ManagerBase
{
	[SerializeField]
	private DifficultyButtonItem _difficultyButtonItemPrefab;

	[SerializeField]
	private Transform _difficultyItemParent;

	[FormerlySerializedAs("_startGameButton")]
	[SerializeField]
	private Button _startConditionsButton;

	public static DifficultyButtonItem SelectedDifficulty;

	public WorldDescription WorldDescription;

	public DifficultySettingWindow DifficultySettingWindow;

	public NewWorldDifficulty DifficultyInfo;

	public Slider DayLengthSlider;

	public TextMeshProUGUI DayLengthText;

	private static int _dayLengthIndex;

	private static List<int> _dayLengths = new List<int>
	{
		300, 600, 900, 1200, 1500, 1800, 2400, 3000, 3600, 7200,
		14400, 21600, 43200, 86400
	};

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		_startConditionsButton.onClick.AddListener(SelectStartConditions);
		PopulateDayLengthSlider();
		WorldManager.OnGameDataLoaded = (Action)Delegate.Combine(WorldManager.OnGameDataLoaded, new Action(PopulateDifficultySettingsList));
	}

	private void OnEnable()
	{
		_startConditionsButton.interactable = true;
	}

	public void PopulateDifficultySettingsList()
	{
		DifficultySetting.ClearButtons();
		if ((object)DifficultySettingWindow == null)
		{
			ConsoleWindow.PrintError("error difficulty setting window is not defined");
		}
		foreach (DifficultySetting allSetting in DifficultySetting.AllSettings)
		{
			if (!allSetting.Hidden)
			{
				DifficultyButtonItem difficultyButtonItem = UnityEngine.Object.Instantiate(_difficultyButtonItemPrefab, _difficultyItemParent);
				difficultyButtonItem.Initialize(allSetting);
				difficultyButtonItem.OnClick = (Action<DifficultyButtonItem>)Delegate.Combine(difficultyButtonItem.OnClick, new Action<DifficultyButtonItem>(SelectDifficulty));
				DifficultySetting.Register(difficultyButtonItem);
				if ((object)DifficultySettingWindow != null)
				{
					DifficultyButtonItem difficultyButtonItem2 = UnityEngine.Object.Instantiate(_difficultyButtonItemPrefab, DifficultySettingWindow.ContentParent);
					difficultyButtonItem2.Initialize(allSetting);
					difficultyButtonItem2.OnClick = (Action<DifficultyButtonItem>)Delegate.Combine(difficultyButtonItem2.OnClick, new Action<DifficultyButtonItem>(SelectDifficulty));
					DifficultySettingWindow.Register(difficultyButtonItem2);
				}
			}
		}
		if (DifficultySetting.AllSettingButtons.Count <= 0)
		{
			return;
		}
		foreach (DifficultyButtonItem allSettingButton in DifficultySetting.AllSettingButtons)
		{
			if (allSettingButton.DifficultySetting.IsDefault)
			{
				SelectDifficulty(allSettingButton);
				return;
			}
		}
		SelectDifficulty(DifficultySetting.AllSettingButtons[0]);
	}

	public static void SelectStartConditions()
	{
		if (!(SelectedDifficulty == null))
		{
			MainMenu.Instance.PageManager.EnableMainMenuPage("StartingConditions");
		}
	}

	public static int GetDayLengthSeconds()
	{
		return _dayLengths[_dayLengthIndex];
	}

	private void PopulateDayLengthSlider()
	{
		DayLengthSlider.minValue = 0f;
		DayLengthSlider.maxValue = _dayLengths.Count - 1;
		_dayLengthIndex = _dayLengths.FindIndex((int x) => x == 1200);
		DayLengthSlider.value = _dayLengthIndex;
		SetDayLength();
	}

	public void SetDayLength()
	{
		_dayLengthIndex = (int)DayLengthSlider.value;
		DayLengthText.text = TimeLength.FromSeconds(_dayLengths[_dayLengthIndex]).ToNearestString();
		WorldDescription.SummaryInfo.Refresh();
	}

	private void SelectDifficulty(DifficultyButtonItem item)
	{
		_startConditionsButton.interactable = true;
		if (SelectedDifficulty == null)
		{
			SelectedDifficulty = item;
			SelectedDifficulty.ShowHighlight(show: true);
		}
		else
		{
			SelectedDifficulty.ShowHighlight(show: false);
			SelectedDifficulty = item;
			SelectedDifficulty.ShowHighlight(show: true);
		}
		DifficultyInfo.SetDifficulty(SelectedDifficulty);
	}
}
