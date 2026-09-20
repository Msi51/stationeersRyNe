using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Networking.Transports;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorkshopMenu : ManagerBase
{
	private const string MOD_CONFIG_XML = "modconfig.xml";

	public static WorkshopMenu Instance;

	public RectTransform ModListContainer;

	public WorkshopModListItem WorkshopItemPrefab;

	public TextMeshProUGUI TitleText;

	public RawImage PreviewImage;

	public TextMeshProUGUI AuthorText;

	public TextMeshProUGUI VersionText;

	public TextMeshProUGUI DescriptionText;

	public GameObject SelectedModButtonLeft;

	public GameObject SelectedModButtonRight;

	[SerializeField]
	private Button _refreshButton;

	public Sprite SteamImage;

	public Sprite LocalImage;

	public Sprite CoreImage;

	private readonly List<WorkshopModListItem> _listItems = new List<WorkshopModListItem>();

	private WorkshopModListItem _selectedModItem;

	public static ModConfig ModsConfig { get; private set; }

	public static string ConfigPath => Path.Combine(StationSaveUtils.DefaultPath, "modconfig.xml");

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		Instance = this;
		WorkshopModListItem.Enabled += EnableMod;
		WorkshopModListItem.Selected += SelectMod;
		WorkshopModListItem.MoveUp += MoveUp;
		WorkshopModListItem.MoveDown += MoveDown;
		_refreshButton.onClick.AddListener(RefreshButtonPressed);
	}

	private void OnEnable()
	{
		Init().Forget();
	}

	private void OnDisable()
	{
		SaveModConfig();
	}

	private void OnDestroy()
	{
		WorkshopModListItem.Enabled -= EnableMod;
		WorkshopModListItem.Selected -= SelectMod;
		WorkshopModListItem.MoveUp -= MoveUp;
		WorkshopModListItem.MoveDown -= MoveDown;
	}

	private async UniTask Init()
	{
		await GetModFolders();
		DataCleanup();
		RefreshList();
		SelectMod(_selectedModItem ? _selectedModItem : _listItems[0]);
		SaveModConfig();
	}

	private async void RefreshButtonPressed()
	{
		_refreshButton.interactable = false;
		UniTask uniTask = Init();
		UniTask uniTask2 = _refreshButton.transform.GetChild(0).DOLocalRotate(new Vector3(0f, 0f, -360f), 0.5f, RotateMode.LocalAxisAdd).SetEase(Ease.OutExpo)
			.AsyncWaitForCompletion()
			.AsUniTask();
		await UniTask.WhenAll(uniTask2, uniTask);
		_refreshButton.transform.GetChild(0).localRotation = Quaternion.Euler(Vector3.zero);
		_refreshButton.interactable = true;
	}

	public static void LoadModConfig()
	{
		if (File.Exists(ConfigPath))
		{
			ModsConfig = XmlSerialization.Deserialize<ModConfig>(ConfigPath);
			ModsConfig.CreateCoreMod();
		}
		else
		{
			ModsConfig = new ModConfig();
			ModsConfig.CreateCoreMod();
		}
		Instance.Init().Forget();
	}

	private static void DataCleanup()
	{
		ModsConfig.Cleanup();
		ModsConfig.Mods.RemoveAll((ModData mod) => !mod.GetAboutData().IsValid);
	}

	private static void SaveModConfig()
	{
		if (ModsConfig != null && !ModsConfig.SaveXml(ConfigPath))
		{
			Debug.LogError("Error saving modconfig.xml");
		}
	}

	private static async UniTask GetModFolders()
	{
		foreach (SteamTransport.ItemWrapper item in await NetworkManager.GetLocalAndWorkshopItems(SteamTransport.WorkshopType.Mod))
		{
			try
			{
				if (ModsConfig.Mods.All((ModData x) => x.DirectoryPath != item.DirectoryPath))
				{
					ModsConfig.Mods.Add(ModData.CreateFrom(item));
				}
			}
			catch (Exception)
			{
				ConsoleWindow.PrintError($"Error loading mod with id {item.Id}");
			}
		}
		DataCleanup();
	}

	private void RefreshList()
	{
		foreach (WorkshopModListItem listItem in _listItems)
		{
			UnityEngine.Object.Destroy(listItem.gameObject);
		}
		_listItems.Clear();
		foreach (ModData mod in ModsConfig.Mods)
		{
			WorkshopModListItem workshopModListItem = UnityEngine.Object.Instantiate(WorkshopItemPrefab, ModListContainer);
			workshopModListItem.SetData(mod);
			_listItems.Add(workshopModListItem);
		}
	}

	public void OpenWorkshopPage()
	{
		string url = $"https://steamcommunity.com/app/{544550u}/workshop/";
		if (Application.isEditor)
		{
			Application.OpenURL(url);
		}
		else
		{
			NetworkManager.CurrentTransport.OpenWebPageOverlay(url);
		}
	}

	public void CloseWorkshopMenu()
	{
		SaveModConfig();
		base.gameObject.SetActive(value: false);
	}

	private static void EnableMod(WorkshopModListItem item)
	{
		if (!item.Data.Enabled)
		{
			item.transform.SetAsLastSibling();
		}
		ModsConfig.MoveToBottom(item.Data);
		SaveModConfig();
	}

	private void MoveUp(WorkshopModListItem item)
	{
		int siblingIndex = item.transform.GetSiblingIndex();
		if (siblingIndex != 0)
		{
			item.transform.SetSiblingIndex(siblingIndex - 1);
			ModsConfig.MoveModUp(item.Data);
			SaveModConfig();
		}
	}

	private void MoveDown(WorkshopModListItem item)
	{
		int siblingIndex = item.transform.GetSiblingIndex();
		int num = ModsConfig.Mods.Count((ModData x) => x.Enabled);
		if (siblingIndex < num - 1)
		{
			item.transform.SetSiblingIndex(siblingIndex + 1);
			ModsConfig.MoveModDown(item.Data);
			SaveModConfig();
		}
	}

	private void SelectMod(WorkshopModListItem modItem)
	{
		_selectedModItem = modItem;
		ModAbout aboutData = modItem.Data.GetAboutData();
		RefreshButtons();
		TitleText.SetText(aboutData.Name);
		AuthorText.SetText(aboutData.Author);
		VersionText.SetText(aboutData.Version);
		DescriptionText.SetText(aboutData.Description);
		Texture2D previewImage = modItem.Data.GetPreviewImage();
		PreviewImage.GetComponent<RawImage>().texture = previewImage;
	}

	private void RefreshButtons()
	{
		if ((bool)_selectedModItem)
		{
			if (_selectedModItem.Data is CoreModData)
			{
				SelectedModButtonLeft.SetActive(value: false);
				SelectedModButtonRight.SetActive(value: false);
			}
			else if (_selectedModItem.Data is LocalModData)
			{
				SelectedModButtonLeft.SetActive(value: false);
				SelectedModButtonRight.SetActive(value: true);
				SelectedModButtonRight.GetComponent<Button>().onClick.RemoveAllListeners();
				SelectedModButtonRight.GetComponent<Button>().onClick.AddListener(PublishMod);
				SelectedModButtonRight.GetComponentInChildren<TextMeshProUGUI>().SetText("Publish");
			}
			else
			{
				SelectedModButtonLeft.SetActive(value: true);
				SelectedModButtonLeft.GetComponent<Button>().onClick.RemoveAllListeners();
				SelectedModButtonLeft.GetComponent<Button>().onClick.AddListener(OpenModOnWorkshop);
				SelectedModButtonRight.SetActive(value: true);
				SelectedModButtonRight.GetComponent<Button>().onClick.RemoveAllListeners();
				SelectedModButtonRight.GetComponent<Button>().onClick.AddListener(UnsubscribeButton);
				SelectedModButtonRight.GetComponentInChildren<TextMeshProUGUI>().SetText("Unsubscribe");
			}
		}
	}

	private async void UnsubscribeButton()
	{
		if (_selectedModItem.Data is WorkshopModData workshopModData)
		{
			if (await SteamTransport.Workshop_DeleteItemAsync(workshopModData.WorkshopId))
			{
				ModsConfig.Mods.Remove(_selectedModItem.Data);
			}
			SaveModConfig();
			Init().Forget();
		}
	}

	private async void PublishMod()
	{
		ModData mod = _selectedModItem.Data;
		ModAbout aboutData = mod.GetAboutData();
		PathReference directoryPath = mod.DirectoryPath;
		string text = $"{directoryPath}\\About\\thumb.png";
		if (!File.Exists(text))
		{
			text = Application.streamingAssetsPath + "\\Images\\ModNoThumb.png";
		}
		SteamTransport.WorkShopItemDetail ItemDetail = new SteamTransport.WorkShopItemDetail
		{
			Title = aboutData.Name,
			Path = directoryPath,
			PreviewPath = text,
			Description = aboutData.Description,
			PublishedFileId = aboutData.WorkshopHandle,
			Type = SteamTransport.WorkshopType.Mod,
			CustomTags = aboutData.Tags
		};
		ProgressPanel.ShowProgressBar();
		var (flag, publishedFileId, _) = await SteamTransport.Workshop_PublishItemAsync(ItemDetail);
		ProgressPanel.ShowProgressSuccessOrFailure(flag);
		if (flag)
		{
			ItemDetail.PublishedFileId = publishedFileId;
			SaveWorkShopFileHandle(ItemDetail, mod);
		}
	}

	private void OpenModOnWorkshop()
	{
		if (_selectedModItem.Data is WorkshopModData workshopModData)
		{
			string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={workshopModData.WorkshopId}";
			if (Application.isEditor)
			{
				Application.OpenURL(url);
			}
			else
			{
				NetworkManager.CurrentTransport.OpenWebPageOverlay(url);
			}
		}
	}

	private static void SaveWorkShopFileHandle(SteamTransport.WorkShopItemDetail ItemDetail, ModData mod)
	{
		ModAbout aboutData = mod.GetAboutData();
		aboutData.WorkshopHandle = ItemDetail.PublishedFileId;
		aboutData.SaveXml(mod.AboutXmlPath);
	}
}
