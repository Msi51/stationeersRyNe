using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Localization2;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.LoadGame;

public class WorldItem : UserInterfaceBase, IPointerClickHandler, IEventSystemHandler
{
	[Header("World Item")]
	[SerializeField]
	private Image _backgroundImage;

	[SerializeField]
	private SaveItem _saveItemPrefab;

	[SerializeField]
	private Transform _saveItemParent;

	[SerializeField]
	private TextMeshProUGUI _titleText;

	[SerializeField]
	private TextMeshProUGUI _infoText;

	[SerializeField]
	private Button _loadLatestButton;

	[Header("Detail")]
	[SerializeField]
	private GameObject _extraButtonsPanelGameObject;

	[SerializeField]
	private Button _renameButton;

	[SerializeField]
	private Button _deleteButton;

	[SerializeField]
	private GameObject _saveItemsGameObject;

	[SerializeField]
	private RectTransform _saveItemsRectTransform;

	[SerializeField]
	private VerticalLayoutGroup _saveItemsVerticalLayoutGroup;

	private DirectoryInfo _stationSaveDirectory;

	public bool Expanded { get; private set; }

	public string StationName { get; private set; }

	public DateTime LastModifiedDate { get; private set; }

	public List<SaveItem> SaveItems { get; } = new List<SaveItem>();

	public void Initialise(SaveInfo saveInfo)
	{
		_titleText.text = saveInfo.StationName;
		_infoText.text = GetInfoText(saveInfo);
		_stationSaveDirectory = saveInfo.StationSaveDirectory;
		_extraButtonsPanelGameObject.SetActive(value: false);
		_backgroundImage.color = _backgroundImage.color.SetAlpha(0.6f);
		foreach (Transform item in _saveItemParent)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
		SaveItems.Clear();
		StationName = saveInfo.StationName;
		SetLastModifiedDate(saveInfo.StationSaveDirectory);
		foreach (SaveFileInfo safe in saveInfo.Saves)
		{
			SaveItem saveItem = UnityEngine.Object.Instantiate(_saveItemPrefab, _saveItemParent);
			saveItem.Initialise(this, safe.FileInfo, safe.SaveType);
			SaveItems.Add(saveItem);
		}
		_loadLatestButton.onClick.AddListener(LoadLatestButtonClicked);
		_renameButton.onClick.AddListener(RenameButtonClicked);
		_deleteButton.onClick.AddListener(DeleteButtonClicked);
	}

	private void OnDestroy()
	{
		_loadLatestButton.onClick.RemoveAllListeners();
		_renameButton.onClick.RemoveAllListeners();
		_deleteButton.onClick.RemoveAllListeners();
	}

	private void SetLastModifiedDate(DirectoryInfo stationDirectory)
	{
		LastModifiedDate = stationDirectory.LastWriteTime;
		DirectoryInfo[] directories = stationDirectory.GetDirectories();
		foreach (DirectoryInfo directoryInfo in directories)
		{
			if (directoryInfo.LastWriteTime > LastModifiedDate)
			{
				LastModifiedDate = directoryInfo.LastWriteTime;
			}
		}
		FileInfo[] files = stationDirectory.GetFiles();
		foreach (FileInfo fileInfo in files)
		{
			if (fileInfo.LastWriteTime > LastModifiedDate)
			{
				LastModifiedDate = fileInfo.LastWriteTime;
			}
		}
	}

	private string GetInfoText(SaveInfo saveInfo)
	{
		List<string> list = new List<string>();
		string text = SaveCountText(saveInfo.ManualSaveCount, "manual");
		if (text != string.Empty)
		{
			list.Add(text);
		}
		string text2 = SaveCountText(saveInfo.QuickSaveCount, "quick");
		if (text2 != string.Empty)
		{
			list.Add(text2);
		}
		string text3 = SaveCountText(saveInfo.AutoSaveCount, "auto");
		if (text3 != string.Empty)
		{
			list.Add(text3);
		}
		return string.Join(", ", list);
		static string SaveCountText(int length, string type)
		{
			return length switch
			{
				0 => string.Empty, 
				1 => "1 " + type + " save", 
				_ => $"{length} {type} saves", 
			};
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		LoadGamePage.Instance.WorldItemClicked(this);
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		_backgroundImage.color = _backgroundImage.color.SetAlpha(1f);
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		_backgroundImage.color = _backgroundImage.color.SetAlpha(0.6f);
	}

	private void LoadLatestButtonClicked()
	{
		foreach (SaveItem saveItem in SaveItems)
		{
			saveItem.LoadSaveDateTimeOnly();
		}
		SaveItems.Sort((SaveItem a, SaveItem b) => b.SaveDateTime.CompareTo(a.SaveDateTime));
		SaveItems[0].LoadSave();
	}

	private void RenameButtonClicked()
	{
		InputWindow.ShowInputPanel("Rename Station", StationName, delegate(string s1, string s2)
		{
			if (SaveHelper.RenameStation(StationName, s1))
			{
				LoadGamePage.Instance.RefreshAllSaves();
			}
		});
	}

	private void DeleteButtonClicked()
	{
		PromptPanel.Instance.ShowPrompt(PromptDeleteStrings.Title, GameStrings.DeleteSavePrompt.AsString(StationName), PromptDeleteStrings.Button, delegate
		{
			if (_stationSaveDirectory.Exists)
			{
				_stationSaveDirectory.Delete(recursive: true);
				LoadGamePage.Instance.RefreshAllSaves();
			}
		});
	}

	public void Expand(bool expand)
	{
		if (Expanded == expand)
		{
			return;
		}
		Expanded = expand;
		_saveItemsGameObject.SetActive(expand);
		_extraButtonsPanelGameObject.SetActive(expand);
		if (expand)
		{
			foreach (SaveItem saveItem in SaveItems)
			{
				saveItem.OnWorldItemExpanded();
			}
			SaveItems.Sort((SaveItem a, SaveItem b) => b.SaveDateTime.CompareTo(a.SaveDateTime));
			for (int num = 0; num < SaveItems.Count; num++)
			{
				SaveItems[num].Transform.SetSiblingIndex(num);
			}
			return;
		}
		foreach (SaveItem saveItem2 in SaveItems)
		{
			saveItem2.Expand(expand: false);
		}
	}

	public float CalculateSize()
	{
		float num = 100f;
		float num2 = 0f;
		if (Expanded)
		{
			foreach (SaveItem saveItem in SaveItems)
			{
				num2 += saveItem.CalculateSize();
			}
			num2 += LoadGamePage.CalculateVerticalExtraSize(_saveItemsVerticalLayoutGroup, SaveItems.Count);
		}
		float num3 = num2 + num;
		_saveItemsRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num2);
		RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num3);
		LayoutRebuilder.MarkLayoutForRebuild(_saveItemsRectTransform);
		return num3;
	}
}
