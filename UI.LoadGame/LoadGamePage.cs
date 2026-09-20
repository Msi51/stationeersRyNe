using System;
using System.Collections.Generic;
using Assets.Scripts.Serialization;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.LoadGame;

public class LoadGamePage : MainMenuPage
{
	[Header("Load Game Page")]
	[SerializeField]
	private Transform _scrollViewContentTransform;

	[SerializeField]
	private RectTransform _contentRectTransform;

	[SerializeField]
	private VerticalLayoutGroup _contentVerticalLayoutGroup;

	[SerializeField]
	private BorderButton _localButton;

	[SerializeField]
	private BorderButton _workshopButton;

	[SerializeField]
	private TMP_InputField _searchField;

	[SerializeField]
	private Button _clearSearchButton;

	[Header("Local saves")]
	[SerializeField]
	private WorldItem _worldItemPrefab;

	[Header("Workshop saves")]
	[SerializeField]
	private WorkshopSaveItem _workshopSaveItemPrefab;

	[Header("Save Type Sprites")]
	public Sprite AutoSaveSprite;

	public Sprite QuickSaveSprite;

	public Sprite ManualSaveSprite;

	[Header("Colors")]
	public Color HeadSaveSpriteTint;

	public const float RowItemHoveredAlpha = 1f;

	public const float RowItemDefaultAlpha = 0.6f;

	private List<WorldItem> _worldItems = new List<WorldItem>();

	private List<WorkshopSaveItem> _workshopSaveItems = new List<WorkshopSaveItem>();

	public static LoadGamePage Instance;

	private void Awake()
	{
		Instance = this;
	}

	private void OnEnable()
	{
		_localButton.Button.onClick.AddListener(LocalButtonClicked);
		_workshopButton.Button.onClick.AddListener(WorkshopButtonClicked);
		_clearSearchButton.onClick.AddListener(ClearSearchClicked);
		_searchField.onValueChanged.AddListener(SearchFieldChanged);
		LocalButtonClicked();
	}

	private void OnDisable()
	{
		_localButton.Button.onClick.RemoveAllListeners();
		_workshopButton.Button.onClick.RemoveAllListeners();
		_clearSearchButton.onClick.RemoveAllListeners();
		_searchField.onValueChanged.RemoveAllListeners();
		ClearContentPanel();
	}

	private void LocalButtonClicked()
	{
		_localButton.ShowBorder(show: true);
		_workshopButton.ShowBorder(show: false);
		GetLocalSaves();
	}

	private void WorkshopButtonClicked()
	{
		_localButton.ShowBorder(show: false);
		_workshopButton.ShowBorder(show: true);
		GetWorkshopSaves();
	}

	private void ClearSearchClicked()
	{
		_searchField.text = string.Empty;
		SearchFieldChanged(string.Empty);
	}

	private void SearchFieldChanged(string value)
	{
		foreach (WorldItem worldItem in _worldItems)
		{
			bool active = string.IsNullOrWhiteSpace(value) || worldItem.StationName.IndexOf(value, StringComparison.InvariantCultureIgnoreCase) >= 0;
			worldItem.GameObject.SetActive(active);
		}
		foreach (WorkshopSaveItem workshopSaveItem in _workshopSaveItems)
		{
			bool active2 = string.IsNullOrWhiteSpace(value) || workshopSaveItem.SaveName.IndexOf(value, StringComparison.InvariantCultureIgnoreCase) >= 0;
			workshopSaveItem.GameObject.SetActive(active2);
		}
	}

	private void RefreshSearch()
	{
		SearchFieldChanged(_searchField.text);
	}

	private void SortWorldItems()
	{
		_worldItems.Sort((WorldItem a, WorldItem b) => b.LastModifiedDate.CompareTo(a.LastModifiedDate));
		for (int num = 0; num < _worldItems.Count; num++)
		{
			_worldItems[num].Transform.SetSiblingIndex(num);
		}
	}

	private void ClearContentPanel()
	{
		foreach (Transform item in _scrollViewContentTransform)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
		_worldItems.Clear();
		_workshopSaveItems.Clear();
	}

	public void RefreshWorkshopSaves()
	{
		GetWorkshopSaves();
	}

	private void GetWorkshopSaves()
	{
		ClearContentPanel();
		LoadWorkshopSavesTask().Forget();
	}

	private async UniTaskVoid LoadWorkshopSavesTask()
	{
		foreach (var item in await LoadHelper.GetWorkshopSaves())
		{
			WorkshopSaveItem workshopSaveItem = UnityEngine.Object.Instantiate(_workshopSaveItemPrefab, _scrollViewContentTransform);
			workshopSaveItem.Initialise(item.Item1, item.Item2);
			_workshopSaveItems.Add(workshopSaveItem);
		}
		RefreshSearch();
		CalculateSize();
	}

	private void GetLocalSaves()
	{
		ClearContentPanel();
		foreach (SaveInfo localSafe in LoadHelper.GetLocalSaves())
		{
			WorldItem worldItem = UnityEngine.Object.Instantiate(_worldItemPrefab, _scrollViewContentTransform);
			worldItem.Initialise(localSafe);
			_worldItems.Add(worldItem);
		}
		SortWorldItems();
		RefreshSearch();
		CalculateSize();
	}

	public void RefreshAllSaves()
	{
		string text = null;
		foreach (WorldItem worldItem in _worldItems)
		{
			if (worldItem.Expanded)
			{
				text = worldItem.StationName;
				break;
			}
		}
		GetLocalSaves();
		foreach (WorldItem worldItem2 in _worldItems)
		{
			if (worldItem2.StationName == text)
			{
				WorldItemClicked(worldItem2);
				break;
			}
		}
	}

	public void WorldItemClicked(WorldItem worldItem)
	{
		if (worldItem.Expanded)
		{
			worldItem.Expand(expand: false);
		}
		else
		{
			foreach (WorldItem worldItem2 in _worldItems)
			{
				worldItem2.Expand(worldItem == worldItem2);
			}
		}
		CalculateSize();
	}

	public void SaveItemClicked(SaveItem saveItem)
	{
		if (saveItem.Expanded)
		{
			saveItem.Expand(expand: false);
		}
		else
		{
			foreach (SaveItem saveItem2 in saveItem.WorldItem.SaveItems)
			{
				saveItem2.Expand(saveItem == saveItem2);
			}
		}
		CalculateSize();
	}

	public void WorkshopSaveItemClicked(WorkshopSaveItem saveItem)
	{
		if (saveItem.Expanded)
		{
			saveItem.Expand(expand: false);
		}
		else
		{
			foreach (WorkshopSaveItem workshopSaveItem in _workshopSaveItems)
			{
				workshopSaveItem.Expand(saveItem == workshopSaveItem);
			}
		}
		CalculateSize();
	}

	public static float CalculateVerticalExtraSize(VerticalLayoutGroup layoutGroup, int itemCount)
	{
		float num = (float)(itemCount - 1) * layoutGroup.spacing;
		int num2 = ((itemCount > 0) ? layoutGroup.padding.vertical : 0);
		return num + (float)num2;
	}

	private void CalculateSize()
	{
		float num = 0f;
		foreach (WorldItem worldItem in _worldItems)
		{
			num += worldItem.CalculateSize();
		}
		foreach (WorkshopSaveItem workshopSaveItem in _workshopSaveItems)
		{
			num += workshopSaveItem.CalculateSize();
		}
		num += CalculateVerticalExtraSize(_contentVerticalLayoutGroup, _worldItems.Count);
		_contentRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
		LayoutRebuilder.MarkLayoutForRebuild(_contentRectTransform);
	}
}
