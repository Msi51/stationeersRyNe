using System;
using System.IO;
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

public class WorkshopSaveItem : UserInterfaceBase, IPointerClickHandler, IEventSystemHandler
{
	[Header("Workshop Save Item")]
	[SerializeField]
	private Image _backgroundImage;

	[SerializeField]
	private TextMeshProUGUI _nameText;

	[SerializeField]
	private TextMeshProUGUI _infoText;

	[SerializeField]
	private Button _loadButton;

	[SerializeField]
	private GameObject _detailPanelGameObject;

	[SerializeField]
	private RectTransform _detailPanelRectTransform;

	[SerializeField]
	private Button _unsubscribeButton;

	[SerializeField]
	private TextMeshProUGUI _versionText;

	[SerializeField]
	private RawImage _previewImage;

	private FileInfo _fileInfo;

	private ulong _workshopId;

	public bool Expanded { get; private set; }

	public string SaveName { get; private set; }

	public DateTime SaveDateTime { get; private set; }

	public void Initialise(SaveFileInfo saveFileInfo, ulong workshopId)
	{
		_workshopId = workshopId;
		_fileInfo = saveFileInfo.FileInfo;
		SaveName = Path.GetFileNameWithoutExtension(saveFileInfo.FileInfo.Name);
		_detailPanelGameObject.SetActive(value: false);
		_backgroundImage.color = _backgroundImage.color.SetAlpha(0.6f);
		_loadButton.onClick.AddListener(LoadButtonClicked);
		_unsubscribeButton.onClick.AddListener(UnsubscribeButtonClicked);
		UnzipFilesAndSetValues();
	}

	private void UnzipFilesAndSetValues()
	{
		if (LoadHelper.UnzipMetaData(_fileInfo, out var metaData) && LoadHelper.UnzipPreviewImage(_fileInfo, out var texture))
		{
			_nameText.text = SaveName;
			SaveDateTime = DateTime.FromFileTime(metaData.DateTime);
			string text = SaveDateTime.ToString("ddd dd-MMM-yy hh:mm:ss");
			string text2 = StringManager.Get(metaData.DaysPast);
			_infoText.text = text + "\nDays Passed: " + text2;
			_versionText.text = "Version:\n" + metaData.GameVersion;
			_previewImage.texture = texture;
		}
	}

	private void LoadButtonClicked()
	{
		string text = SaveName;
		for (int i = 2; i < 100; i++)
		{
			if (SaveHelper.CreateSaveDirectory(text, out var directoryInfo))
			{
				string text2 = directoryInfo.FullName + "/" + text + SaveLoadConstants.SaveFileExtension;
				try
				{
					_fileInfo.CopyTo(text2);
				}
				catch
				{
					break;
				}
				LoadHelper.LoadGame(text2, text);
				break;
			}
			text = $"{SaveName}_{i}";
		}
	}

	private void UnsubscribeButtonClicked()
	{
		UnsubscribeTask().Forget();
	}

	private async UniTaskVoid UnsubscribeTask()
	{
		if (await SteamTransport.Workshop_UnsubscribeAsync(_workshopId))
		{
			LoadGamePage.Instance.RefreshWorkshopSaves();
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		LoadGamePage.Instance.WorkshopSaveItemClicked(this);
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		_backgroundImage.color = _backgroundImage.color.SetAlpha(1f);
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		_backgroundImage.color = _backgroundImage.color.SetAlpha(0.6f);
	}

	private void OnDestroy()
	{
		_loadButton.onClick.RemoveAllListeners();
		_unsubscribeButton.onClick.RemoveAllListeners();
	}

	public void Expand(bool expand)
	{
		if (Expanded != expand)
		{
			Expanded = expand;
			_detailPanelGameObject.SetActive(expand);
		}
	}

	public float CalculateSize()
	{
		float num = 380f;
		float num2 = 100f;
		if (Expanded)
		{
			num2 += num;
		}
		RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num2);
		return num2;
	}
}
