using System;
using System.IO;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking.Transports;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.LoadGame;

public class SaveItem : UserInterfaceBase, IPointerClickHandler, IEventSystemHandler
{
	[Header("Save Item")]
	[SerializeField]
	private Image _backgroundImage;

	[SerializeField]
	private Image _typeImage;

	[SerializeField]
	private TextMeshProUGUI _nameText;

	[SerializeField]
	private TextMeshProUGUI _infoText;

	[SerializeField]
	private Button _loadButton;

	[Header("Detail Panel")]
	[SerializeField]
	private GameObject _detailPanelGameObject;

	[SerializeField]
	private RectTransform _detailPanelRectTransform;

	[SerializeField]
	private Button _publishButton;

	[SerializeField]
	private GameObject _publishButtonGameObject;

	[SerializeField]
	private SteamPublishedIcon _publishedIcon;

	[SerializeField]
	private Button _deleteButton;

	[SerializeField]
	private GameObject _deleteButtonGameObject;

	[SerializeField]
	private TextMeshProUGUI _versionText;

	[SerializeField]
	private RawImage _previewImage;

	private FileInfo _fileInfo;

	private SaveType _saveType;

	private bool _metaDataHasBeenLoaded;

	public WorldItem WorldItem { get; private set; }

	public bool Expanded { get; private set; }

	public string SaveName { get; private set; }

	public DateTime SaveDateTime { get; private set; }

	public ulong WorkshopId { get; private set; }

	public void Initialise(WorldItem worldItem, FileInfo fileInfo, SaveType saveType)
	{
		WorldItem = worldItem;
		SaveName = Path.GetFileNameWithoutExtension(fileInfo.Name);
		_fileInfo = fileInfo;
		_saveType = saveType;
		Expand(expand: false, force: true);
		_backgroundImage.color = _backgroundImage.color.SetAlpha(0.6f);
		_loadButton.onClick.AddListener(LoadButtonClicked);
		_publishButton.onClick.AddListener(PublishButtonClicked);
		_deleteButton.onClick.AddListener(DeleteButtonClicked);
	}

	public void OnWorldItemExpanded()
	{
		if (!_metaDataHasBeenLoaded && LoadHelper.UnzipMetaData(_fileInfo, out var metaData) && LoadHelper.UnzipPreviewImage(_fileInfo, out var texture))
		{
			switch (_saveType)
			{
			case SaveType.Auto:
				_typeImage.sprite = LoadGamePage.Instance.AutoSaveSprite;
				_nameText.text = "Autosave";
				break;
			case SaveType.Quick:
				_typeImage.sprite = LoadGamePage.Instance.QuickSaveSprite;
				_nameText.text = "Quicksave";
				break;
			case SaveType.Head:
				_typeImage.sprite = LoadGamePage.Instance.ManualSaveSprite;
				_typeImage.color = LoadGamePage.Instance.HeadSaveSpriteTint;
				_nameText.text = SaveName;
				break;
			case SaveType.Manual:
				_typeImage.sprite = LoadGamePage.Instance.ManualSaveSprite;
				_nameText.text = SaveName;
				break;
			}
			WorkshopId = metaData.WorkShopFileHandle;
			CheckWorkshopId().Forget();
			SaveDateTime = DateTime.FromFileTime(metaData.DateTime);
			string text = SaveDateTime.ToString("ddd dd-MMM-yy hh:mm:ss");
			string text2 = StringManager.Get(metaData.DaysPast);
			_infoText.text = text + "\nDays Passed: " + text2;
			_versionText.text = "Version:\n" + metaData.GameVersion;
			_previewImage.texture = texture;
			_deleteButtonGameObject.SetActive(_saveType != SaveType.Head);
			_publishButtonGameObject.SetActive(_saveType == SaveType.Manual);
			_metaDataHasBeenLoaded = true;
		}
	}

	public void LoadSaveDateTimeOnly()
	{
		if (!_metaDataHasBeenLoaded && LoadHelper.UnzipMetaData(_fileInfo, out var metaData))
		{
			SaveDateTime = DateTime.FromFileTime(metaData.DateTime);
		}
	}

	public void LoadSave()
	{
		LoadHelper.LoadGame(_fileInfo.FullName, WorldItem.StationName);
	}

	private async UniTaskVoid CheckWorkshopId()
	{
		bool flag = WorkshopId != 0;
		_publishedIcon.SetActive(flag);
		if (flag)
		{
			bool matchFound = await SteamTransport.Workshop_ItemExists(WorkshopId);
			_publishedIcon.SetMatchFound(matchFound);
		}
	}

	private void OnDestroy()
	{
		_loadButton.onClick.RemoveAllListeners();
		_publishButton.onClick.RemoveAllListeners();
		_deleteButton.onClick.RemoveAllListeners();
	}

	private void LoadButtonClicked()
	{
		LoadSave();
	}

	private async UniTaskVoid PublishToWorkshopTask(string title, string path, ulong workshopId)
	{
		await XmlSaveLoad.Instance.PublishSaveToWorkshopTask(title, path, workshopId);
		CheckWorkshopId();
	}

	private void PublishButtonClicked()
	{
		PromptPanel.Instance.ShowPrompt(PromptPublishStrings.Title, PromptPublishStrings.Body, PromptPublishStrings.Button, delegate
		{
			string title = WorldItem.StationName + "_" + SaveName;
			PublishToWorkshopTask(title, _fileInfo.FullName, WorkshopId).Forget();
		});
	}

	private void DeleteButtonClicked()
	{
		PromptPanel.Instance.ShowPrompt(PromptDeleteStrings.Title, GameStrings.DeleteSavePrompt.AsString(SaveName), PromptDeleteStrings.Button, delegate
		{
			if (_fileInfo.Exists)
			{
				_fileInfo.Delete();
				LoadGamePage.Instance.RefreshAllSaves();
			}
		});
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		LoadGamePage.Instance.SaveItemClicked(this);
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		_backgroundImage.color = _backgroundImage.color.SetAlpha(1f);
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		_backgroundImage.color = _backgroundImage.color.SetAlpha(0.6f);
	}

	public void Expand(bool expand, bool force = false)
	{
		if (force || Expanded != expand)
		{
			Expanded = expand;
			_detailPanelGameObject.SetActive(expand);
		}
	}

	public float CalculateSize()
	{
		float num = 100f;
		float num2 = 380f;
		if (Expanded)
		{
			num += num2;
		}
		RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
		return num;
	}
}
