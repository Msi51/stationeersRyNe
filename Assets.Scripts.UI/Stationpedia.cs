using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Genetics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Objects.Chutes;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI.Genetics;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects;
using Objects.Electrical;
using Objects.Items;
using Objects.Rockets;
using Reagents;
using TMPro;
using TraderUI;
using UI.PhaseChange;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class Stationpedia : ResizableWindow, IModal
{
	[Header("Stationpedia References")]
	public SPDAListItem ListInsertPrefab;

	public SPDAListItem ListSearchPrefab;

	public SPDACombustionItem CombustionItemPrefab;

	public StationpediaCategory CategoryPrefab;

	public SPDASlot SlotInsertPrefab;

	public SPDAManufacturer ManufactureInsertPrefab;

	public SPDAVersion MachineTierInsertPrefab;

	public SPDALogic LogicInsertPrefab;

	public SPDAGeneric GenericPrefab;

	public SPDAGeneric LogicBindingPrefab;

	public SPDAFoundIn FoundInInsertPrefab;

	public SPDAFoundIn FermentationInsertPrefab;

	public SPDAGeneric InfoBoxPrefab;

	public SPDALifeRequirement LifeRequirementPrefab;

	public SPDAHomePageCategory HomePageButtonPrefab;

	[Header("Stationpedia UI References")]
	public LayoutElement StationpediaElementLayout;

	public GameObject StationpediaTitleText;

	public TextMeshProUGUI PageTitle;

	public TextMeshProUGUI PageText;

	public UniversalPage UniversalPageRef;

	public Image ModalBackground;

	public Toggle ToggleMouse;

	[Header("Stationpedia Other")]
	public Sprite ResizeExpandIcon;

	public Sprite ResizeShrinkIcon;

	public Sprite ReagentImage;

	public Sprite GeneImage;

	public Sprite VariableImage;

	public GameObject HomePage;

	public RectTransform HomePageContent;

	public static Stationpedia Instance;

	public RectTransform ContentRectTransform;

	public Scrollbar ScrollBarUniversal;

	public Image Background;

	public List<Sprite> LoreFactionThumbnails = new List<Sprite>();

	[Header("PhaseChangeDiagram")]
	public PhaseChangeDiagram PhaseChangeDiagram;

	[SerializeField]
	private GameObject _searchResultsPage;

	public static List<StationpediaPage> StationpediaPages = new List<StationpediaPage>();

	private static Dictionary<string, StationpediaPage> _linkIdLookup = new Dictionary<string, StationpediaPage>();

	private static List<string> _pageHistory = new List<string> { "Home" };

	public static Dictionary<string, float> PageHistoryScroll = new Dictionary<string, float> { { "Home", 1f } };

	public Image ShrinkButton;

	public RectTransform ParentRectTransform;

	public Button HomePageButton;

	[Header("Lore/Guides Variables")]
	public RectTransform LoreGuideContents;

	public GameObject LoreGuideHolder;

	public TextMeshProUGUI LoreGuideTitle;

	public TextMeshProUGUI LoreGuideDescription;

	[Header("Search Variables")]
	[SerializeField]
	private GameObject _homeGuideButtonContainer;

	[SerializeField]
	private GameObject _homeCategoryButtonContainer;

	public GameObject SearchPageItems;

	public GameObject NoResultsFromSearchText;

	public RectTransform SearchContents;

	public Sprite DefaultSearchImage;

	public Sprite ImportantSearchImage;

	public TMP_InputField SearchField;

	private int SearchDelayTime = 500;

	public List<string> SearchResultKeys = new List<string>();

	public static List<string> GuidesPages = new List<string>();

	public static List<string> LorePages = new List<string>();

	private UniTask searchWaitTask;

	private int searchResultsPerPage = 100;

	private List<SPDAListItem> _SPDASearchInserts = new List<SPDAListItem>();

	private List<SPDAListItem> _SPDAGuideLoreInserts = new List<SPDAListItem>();

	private static readonly int OpenHash = Animator.StringToHash("SP_Open");

	private static readonly int CloseHash = Animator.StringToHash("SP_Close");

	private static readonly int SearchHash = Animator.StringToHash("SP_Search");

	public Animator BaseAnimator;

	public List<GasThumbnail> _gasThumbnails;

	private UniTask _finishSearchRoutine;

	private CancellationTokenSource _searchRoutineCancel;

	private readonly Regex _searchRegex = new Regex("[^a-zA-Z0-9-]+", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

	public const float DELAY_AFTER_KEY = 0.4f;

	private float _waitForSearch;

	private int _currentHistoryIndex = -1;

	public bool isExpanded = true;

	private bool _isOpaque = true;

	public Button NextButton;

	public Button PreviousButton;

	private static bool _intialized;

	public Sprite HomePageOverrideImage;

	private List<int> existingCreators;

	public static StationpediaPage ThingSlotHeader;

	public static StationpediaPage ThingSlotItem;

	public static StationpediaPage LogicTypeHeader;

	public static StationpediaPage LogicTypeItem;

	public static StationpediaPage CreatorHeader;

	public static StationpediaPage CreatorItem;

	public static StationpediaPage CreatedHeader;

	public static StationpediaPage ConstructedHeader;

	public static StationpediaPage ConstructedByHeader;

	public static StationpediaPage ModeStringHeader;

	public static StationpediaPage ModeStringItem;

	public static StationpediaPage CreatedReagent;

	public static StationpediaPage ThingStack;

	public static StationpediaPage ThingNutrition;

	public static StationpediaPage IceMelting;

	public static StationpediaPage DevicePage;

	public static StationpediaPage GasCanisterPage;

	public static StationpediaPage AtmosphericsPage;

	public static StationpediaPage NutritionPage;

	public static StationpediaPage CreatedGases;

	public static StationpediaPage ThingTransmissableTemplate;

	public static StationpediaPage TransmitTemplate;

	public static StationpediaPage TierListHeader;

	public static string DefaultThingText = string.Empty;

	public static string CurrentPageKey = "Home";

	public static SPDADataHandler DataHandler = new SPDADataHandler();

	private static string _electronicsPage = "Electronics";

	private static string _organicsPage = "Organics";

	private static string _atmosphericsPage = "Atmospherics";

	private static string _importExportPage = "ImportExport";

	private static string _furniturePage = "Furnitures";

	public static List<SPDAHomePageButtonOverride> HomePageOverrides = new List<SPDAHomePageButtonOverride>();

	private static string lorePageKey = "LorePage";

	private static string faction = "Factions";

	private static readonly int Copied = Animator.StringToHash("Copied");

	public VerticalLayoutGroup WindowGrid;

	private bool _isRendered;

	public override bool DraggingEnabled => GameManager.GameState == GameState.Running;

	public string CurrentHistory
	{
		get
		{
			if (_currentHistoryIndex <= -1)
			{
				return CurrentPageKey;
			}
			return _pageHistory[_currentHistoryIndex];
		}
	}

	public static bool IsMouseLocked { get; private set; }

	public static bool IsOpenAndLocked
	{
		get
		{
			if (IsMouseLocked)
			{
				return IsOpen;
			}
			return false;
		}
	}

	private bool IsOpaque
	{
		get
		{
			return _isOpaque;
		}
		set
		{
			_isOpaque = value;
			Background.color = Background.color.SetAlpha(_isOpaque ? 1f : 0.75f);
		}
	}

	public static bool IsOpen
	{
		get
		{
			if ((bool)Instance)
			{
				return Instance.IsVisible;
			}
			return false;
		}
	}

	public bool UnlockCursor
	{
		get
		{
			if (!IsMouseLocked)
			{
				return SearchField.isFocused;
			}
			return true;
		}
	}

	public static event Event OnOpened;

	public static event Event OnClosed;

	public static event Event OnPageChanged;

	public void Awake()
	{
		SetPage(CurrentPageKey);
		SearchField.onSubmit.AddListener(delegate
		{
			StartSearchNow();
			KeyManager.RemoveInputState("Stationpedia");
		});
		SearchField.onValueChanged.AddListener(SearchBehaviour);
		WorldManager.OnPaused += WorldManagerOnPaused;
		SearchField.onSelect.AddListener(delegate
		{
			KeyManager.SetInputState("Stationpedia", KeyInputState.Typing);
		});
		SearchField.onDeselect.AddListener(delegate
		{
			KeyManager.RemoveInputState("Stationpedia");
		});
		IsMouseLocked = ToggleMouse.isOn;
	}

	private void PauseGameToggle(bool value)
	{
		if (!NetworkManager.IsClient && NetworkBase.Clients.Count == 0 && !InventoryManager.Instance.InGameMenuOpen)
		{
			WorldManager.SetGamePause(value);
		}
	}

	private void SearchBehaviour(string inputText)
	{
		if (inputText.Length == 0)
		{
			ClearPreviousSearch();
			SetPage("Home");
		}
		else
		{
			SetPage("Search");
			StartSearchCountdown();
		}
		_homeGuideButtonContainer.SetActive(inputText.Length == 0);
		_homeCategoryButtonContainer.SetActive(inputText.Length == 0);
		SearchPageItems.SetActive(inputText.Length > 0);
	}

	public static void HelpOnKeyDown()
	{
		if (!GameManager.IsBatchMode && InputKeyWindow.InputState == InputPanelState.None)
		{
			Toggle();
		}
	}

	private void WorldManagerOnPaused(bool isOn)
	{
	}

	public static void ClearAll()
	{
		foreach (StationpediaPage stationpediaPage in StationpediaPages)
		{
			stationpediaPage.Clear();
		}
		StationpediaPages.Clear();
		_linkIdLookup.Clear();
		LorePages.Clear();
		GuidesPages.Clear();
		HomePageOverrides.Clear();
		if (!(Instance?.HomePageContent != null))
		{
			return;
		}
		foreach (Transform item in Instance.HomePageContent)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
	}

	private void StartSearchCountdown()
	{
		_waitForSearch = 0.4f;
		if (SearchField.text.Length >= 3 && searchWaitTask.Status != UniTaskStatus.Pending)
		{
			searchWaitTask = WaitStartSearch();
		}
	}

	private void StartSearchNow()
	{
		_waitForSearch = 0f;
		if (searchWaitTask.Status != UniTaskStatus.Pending)
		{
			searchWaitTask = WaitStartSearch();
		}
	}

	private async UniTask WaitStartSearch()
	{
		while (_waitForSearch > 0f)
		{
			_waitForSearch -= Time.unscaledDeltaTime;
			await UniTask.NextFrame();
		}
		ClearAndStartSearch(SearchField.text);
	}

	private void ForceSearch(string searchText)
	{
		if (string.IsNullOrEmpty(searchText))
		{
			ClearPreviousSearch();
			NoResultsFromSearchText.SetActive(value: true);
			return;
		}
		string hash = Regex.Replace(searchText, "[^0-9-]+", "", RegexOptions.IgnorePatternWhitespace);
		List<string> list = searchText.Split(',').ToList();
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			if (i > 0)
			{
				stringBuilder.Append("|");
			}
			List<string> list2 = text.Split(' ').ToList();
			for (int num = list2.Count - 1; num >= 0; num--)
			{
				list2[num] = _searchRegex.Replace(list2[num], string.Empty);
				if (string.IsNullOrEmpty(list2[num]))
				{
					list2.RemoveAt(num);
				}
			}
			for (int j = 0; j < list2.Count; j++)
			{
				stringBuilder.Append("(?=.*" + list2[j] + ")");
			}
		}
		ClearPreviousSearch();
		_searchRoutineCancel = new CancellationTokenSource();
		_finishSearchRoutine = DoSearch(hash, stringBuilder.ToString(), _searchRoutineCancel);
	}

	private async UniTask DoSearch(string hash, string pattern, CancellationTokenSource cancelToken)
	{
		if (string.IsNullOrEmpty(pattern) && string.IsNullOrEmpty(hash))
		{
			ClearPreviousSearch();
			NoResultsFromSearchText.SetActive(value: true);
			return;
		}
		NoResultsFromSearchText.SetActive(value: false);
		await UniTask.SwitchToThreadPool();
		int count = 0;
		int i = StationpediaPages.Count - 1;
		while (i >= 0 && count < searchResultsPerPage)
		{
			if (cancelToken.IsCancellationRequested)
			{
				return;
			}
			if (!string.IsNullOrEmpty(StationpediaPages[i].Title) && !StationpediaPages[i].Title.Equals("Search") && StationpediaPages[i].IsRegexMatch(hash, pattern))
			{
				StationpediaPage page = _linkIdLookup[StationpediaPages[i].Key];
				SPDAListItem insert = _SPDASearchInserts[count];
				MakePage(page, insert).Forget();
				await UniTask.Delay(20, DelayType.UnscaledDeltaTime);
				count++;
			}
			i--;
		}
		await UniTask.SwitchToMainThread();
		NoResultsFromSearchText.SetActive(count == 0);
	}

	private void ClearAndStartSearch(string searchText)
	{
		ClearPreviousSearch();
		ForceSearch(searchText);
	}

	private async UniTaskVoid MakePage(StationpediaPage page, SPDAListItem insert)
	{
		await UniTask.SwitchToMainThread();
		if (_searchRoutineCancel.IsCancellationRequested)
		{
			return;
		}
		Thing thing = Prefab.Find(page.PrefabHash);
		if (page.ImportantPage)
		{
			insert.InsertImage.sprite = ImportantSearchImage;
			insert.SetSpecial();
		}
		else
		{
			insert.SetNormal();
			if ((bool)thing)
			{
				insert.InsertImage.sprite = thing.Thumbnail;
			}
			else if (page.CustomSpriteToUse != null)
			{
				insert.InsertImage.sprite = page.CustomSpriteToUse;
			}
			else
			{
				insert.InsertImage.sprite = DefaultSearchImage;
			}
		}
		insert.Apply(page.Title);
		insert.InsertsButton.onClick.AddListener(delegate
		{
			OpenPageByKey(page.Key);
		});
		insert.gameObject.SetActive(!string.IsNullOrEmpty(insert.InsertTitle.text));
	}

	private void ClearPreviousSearch()
	{
		if (_finishSearchRoutine.Status == UniTaskStatus.Pending)
		{
			_searchRoutineCancel.Cancel();
		}
		SearchResultKeys.Clear();
		foreach (SPDAListItem sPDASearchInsert in _SPDASearchInserts)
		{
			sPDASearchInsert.InsertsButton.onClick.RemoveAllListeners();
			sPDASearchInsert.Apply(string.Empty);
			sPDASearchInsert.InsertImage.sprite = null;
			sPDASearchInsert.SetVisible(isVisble: false);
		}
	}

	public void Initialize()
	{
		Instance = this;
		AddSearchInsertsToPool(searchResultsPerPage);
	}

	public void ResetClipboardNotification()
	{
		BaseAnimator.SetBool(Copied, value: false);
	}

	public void ToggleResize()
	{
		isExpanded = !isExpanded;
		StationpediaElementLayout.preferredWidth = (isExpanded ? 990 : 543);
		StationpediaTitleText.SetActive(isExpanded);
		ShrinkButton.sprite = (isExpanded ? ResizeShrinkIcon : ResizeExpandIcon);
		Chemistry.GasType gasType = GetPage(CurrentPageKey)?.GasType ?? Chemistry.GasType.Undefined;
		if (gasType != Chemistry.GasType.Undefined && gasType != Chemistry.GasType.Fuel && gasType != Chemistry.GasType.Air && gasType != Chemistry.GasType.Helium)
		{
			PhaseChangeDiagram.SetVisible(isExpanded);
		}
	}

	public void SetMouseState(bool isMouseOn)
	{
		if (isMouseOn)
		{
			IsMouseLocked = true;
			if (IsOpen)
			{
				InventoryManager.Instance?.TooltipRef.ClearTooltip();
				ModalBackground.enabled = true;
			}
		}
		else
		{
			IsMouseLocked = false;
			ModalBackground.enabled = false;
		}
	}

	public static void OpenAt(Thing thing)
	{
		if (!(thing == null))
		{
			if (!Instance.IsVisible)
			{
				Instance.SetVisible(isVisble: true);
				MouseModeController.AddModal(Instance);
			}
			Instance.SetPage("Thing" + thing.PrefabName);
		}
	}

	public static void OpenAt(string page, bool forceOpaque = false)
	{
		if (!string.IsNullOrEmpty(page))
		{
			if (!Instance.IsVisible)
			{
				Instance.SetVisible(isVisble: true);
				MouseModeController.AddModal(Instance);
			}
			Instance.SetPage(page);
			if (forceOpaque)
			{
				Instance.IsOpaque = true;
			}
		}
	}

	public void OpenPageByKey(string page)
	{
		if (!string.IsNullOrEmpty(page))
		{
			if (!Instance.IsVisible)
			{
				Instance.SetVisible(isVisble: true);
				MouseModeController.AddModal(Instance);
			}
			Instance.SetPage(page);
		}
	}

	public void SetPage(string key, bool newPage = true)
	{
		if (string.IsNullOrEmpty(key))
		{
			return;
		}
		if (newPage && CurrentPageKey != key && CurrentHistory != key)
		{
			if (_currentHistoryIndex < _pageHistory.Count - 1)
			{
				int count = _pageHistory.Count - 1 - _currentHistoryIndex;
				_pageHistory.RemoveRange(_currentHistoryIndex + 1, count);
			}
			_pageHistory.Add(key);
			_currentHistoryIndex = _pageHistory.Count - 1;
		}
		PageHistoryScroll[CurrentPageKey] = ScrollBarUniversal.value;
		if (key != CurrentPageKey && newPage)
		{
			ScrollBarUniversal.value = 1f;
		}
		CurrentPageKey = key;
		_linkIdLookup.TryGetValue(key, out var value);
		if (value != null)
		{
			HomePageButton.interactable = !key.Equals("Home");
			switch (key)
			{
			case "Home":
				ButtonHomeForced();
				break;
			case "Search":
				ButtonSearchForced(clear: false);
				break;
			case "Lore":
				SetPageLore();
				break;
			case "Guides":
				SetPageGuides();
				LoreGuideHolder.gameObject.SetActive(value: true);
				break;
			default:
				_searchResultsPage.gameObject.SetActive(value: false);
				LoreGuideHolder.gameObject.SetActive(value: false);
				Render(value);
				break;
			}
			Stationpedia.OnPageChanged?.Invoke();
			UpdateNavigationInteractButtons();
		}
	}

	public StationpediaPage GetPage(string key)
	{
		_linkIdLookup.TryGetValue(key, out var value);
		return value;
	}

	public void ButtonHome()
	{
		SearchField.text = string.Empty;
		SetPage("Home");
	}

	public override void OnDisable()
	{
		base.OnDisable();
		if (!WorldManager.IsGamePaused)
		{
			UIAudioManager.Play(CloseHash);
		}
		if (!TraderCanvas.Instance.IsVisible)
		{
			_ = InventoryManager.Instance.InGameMenuOpen;
		}
	}

	public void ButtonSearchForced(bool clear = true)
	{
		HomePage.SetActive(value: false);
		_searchResultsPage.SetActive(value: true);
		LoreGuideHolder.SetActive(value: false);
		UniversalPageRef.gameObject.SetActive(value: false);
		if (clear)
		{
			SearchField.Select();
			SearchField.text = string.Empty;
			StartSearchNow();
		}
	}

	public void ButtonHomeForced()
	{
		HomePage.SetActive(value: true);
		NoResultsFromSearchText.SetActive(value: false);
		LoreGuideHolder.gameObject.SetActive(value: false);
		UniversalPageRef.gameObject.SetActive(value: false);
		SearchField.Select();
		_searchResultsPage.SetActive(value: false);
	}

	private void PopulateGuideLoreContents(List<string> SPDAKeys, bool important)
	{
		foreach (SPDAListItem sPDAGuideLoreInsert in _SPDAGuideLoreInserts)
		{
			UnityEngine.Object.Destroy(sPDAGuideLoreInsert.gameObject);
		}
		_SPDAGuideLoreInserts.Clear();
		foreach (string key in SPDAKeys)
		{
			SPDAListItem sPDAListItem = UnityEngine.Object.Instantiate(ListSearchPrefab, LoreGuideContents);
			if (_linkIdLookup.TryGetValue(key, out var value))
			{
				sPDAListItem.Apply(value.Title);
				sPDAListItem.InsertsButton.onClick.AddListener(delegate
				{
					OpenPageByKey(key);
				});
				if (important)
				{
					sPDAListItem.InsertImage.sprite = ImportantSearchImage;
					sPDAListItem.SetSpecial();
				}
				else
				{
					sPDAListItem.InsertImage.sprite = value.CustomSpriteToUse;
					sPDAListItem.SetNormal();
				}
				if (sPDAListItem.InsertImage.sprite == null)
				{
					sPDAListItem.InsertImage.gameObject.SetActive(value: false);
				}
				_SPDAGuideLoreInserts.Add(sPDAListItem);
			}
		}
	}

	public void ButtonGuides()
	{
		SetPage("Guides");
	}

	private void SetPageGuides()
	{
		HomePage.SetActive(value: false);
		UniversalPageRef.gameObject.SetActive(value: false);
		LoreGuideHolder.gameObject.SetActive(value: true);
		PopulateGuideLoreContents(GuidesPages, important: true);
		LoreGuideTitle.SetText(Localization.GetInterface("StationpediaBeginnerGuides"));
	}

	public void ButtonLore()
	{
		SetPage("Lore");
	}

	public void SetPageLore()
	{
		HomePage.SetActive(value: false);
		UniversalPageRef.gameObject.SetActive(value: false);
		LoreGuideHolder.gameObject.SetActive(value: true);
		PopulateGuideLoreContents(LorePages, important: false);
		LoreGuideTitle.SetText(Localization.GetInterface("StationpediaAdvancedGuides"));
	}

	public void ButtonNext()
	{
		if (_currentHistoryIndex < _pageHistory.Count)
		{
			_currentHistoryIndex++;
			SetPage(_pageHistory[_currentHistoryIndex], newPage: false);
		}
	}

	public void ButtonPrevious()
	{
		if (_currentHistoryIndex > 0)
		{
			_currentHistoryIndex--;
			SetPage(_pageHistory[_currentHistoryIndex], newPage: false);
		}
	}

	public void ButtonOpacity()
	{
		IsOpaque = !IsOpaque;
	}

	public static void Register(StationpediaPage page, bool fallback = false)
	{
		_linkIdLookup.TryGetValue(page.Key, out var value);
		if (!fallback || value == null)
		{
			if (value != null)
			{
				_linkIdLookup.Remove(value.Key);
				StationpediaPages.Remove(value);
			}
			if (page.DisplayFilter == SPDAEntryType.Guides)
			{
				GuidesPages.Add(page.Key);
			}
			else if (page.DisplayFilter == SPDAEntryType.Lore)
			{
				LorePages.Add(page.Key);
			}
			StationpediaPages.Add(page);
			_linkIdLookup.Add(page.Key, page);
		}
	}

	public static void UpdateNavigationInteractButtons()
	{
		Instance.NextButton.interactable = _pageHistory.Count - 1 > Instance._currentHistoryIndex;
		Instance.PreviousButton.interactable = Instance._currentHistoryIndex > 0;
	}

	private void Render(StationpediaPage page)
	{
		UpdateNavigationInteractButtons();
		if (page != null)
		{
			PageTitle.enabled = true;
			PageText.enabled = true;
			PageTitle.text = page.Title;
			UniversalPageRef.ChangeDisplay(page);
			UniversalPageRef.SetVisible(isVisble: true);
			HomePage.SetActive(value: false);
			LayoutRebuilder.ForceRebuildLayoutImmediate(Instance.ContentRectTransform);
			FixTheScrollValue(page).Forget();
		}
	}

	private static async UniTaskVoid FixTheScrollValue(StationpediaPage page)
	{
		await UniTask.NextFrame();
		if (PageHistoryScroll.TryGetValue(page.Key, out var value))
		{
			Instance.ScrollBarUniversal.value = value;
		}
	}

	private void AddSearchInsertsToPool(int numToAdd)
	{
		for (int i = 0; i < numToAdd; i++)
		{
			SPDAListItem sPDAListItem = UnityEngine.Object.Instantiate(ListSearchPrefab, SearchContents);
			_SPDASearchInserts.Add(sPDAListItem);
			sPDAListItem.gameObject.SetActive(value: false);
		}
	}

	public static void Regenerate()
	{
		if (!(Instance != null))
		{
			return;
		}
		Instance.PopulateLists();
		foreach (StationpediaPage stationpediaPage in StationpediaPages)
		{
			stationpediaPage.ParsePage();
		}
		Instance.PopulateThingPages();
		Instance.PopulateLogicVariables();
		Instance.PopulateLogicSlotVariables();
		Instance.PopulateReagents();
		Instance.PopulateGenes();
		Instance.PopulateTrading();
		Instance.PopulateGases();
		Instance.PopulateFactionLorePages();
		Instance.UpdateLinkedPages();
		Instance.SetPage(CurrentPageKey);
		Instance.SortPages();
		GC.Collect();
		if (!_intialized)
		{
			Localization.OnLanguageChanged = (Action)Delegate.Combine(Localization.OnLanguageChanged, new Action(Regenerate));
			_intialized = true;
		}
	}

	private void SortPages()
	{
		StationpediaPages.Sort(delegate(StationpediaPage a, StationpediaPage b)
		{
			int num = a.SortPriority.CompareTo(b.SortPriority);
			if (num == 0)
			{
				num = string.Compare(a.Title, b.Title, StringComparison.Ordinal);
			}
			return num;
		});
		GuidesPages.Sort((string a, string b) => string.Compare(GetPage(a).Title, GetPage(b).Title, StringComparison.Ordinal));
		LorePages.Sort((string a, string b) => string.Compare(GetPage(a).Title, GetPage(b).Title, StringComparison.Ordinal));
	}

	private void UpdateLinkedPages()
	{
		foreach (SPDAHomePageButtonOverride homePageOverride in HomePageOverrides)
		{
			SPDAHomePageCategory prefab = homePageOverride.Prefab;
			StationpediaPage page = GetPage(homePageOverride.LinkedPageKey);
			if (page != null)
			{
				page.SortPriority++;
				page.Title = "<b>" + page.Title + "</b>";
				page.CustomSpriteToUse = prefab.ButtonImage.sprite;
				page.ImportantPage = true;
			}
		}
		foreach (string guidesPage in GuidesPages)
		{
			StationpediaPage page2 = GetPage(guidesPage);
			if (page2 != null)
			{
				page2.SortPriority++;
				page2.Title = "<b>" + page2.Title + "</b>";
				StationpediaPage stationpediaPage = page2;
				if ((object)stationpediaPage.CustomSpriteToUse == null)
				{
					stationpediaPage.CustomSpriteToUse = ImportantSearchImage;
				}
				page2.ImportantPage = true;
			}
		}
	}

	public static void ResetWindow()
	{
	}

	private static void AddSlotInfo(Thing prefab, ref StationpediaPage page)
	{
		if (prefab.HasAnySlots && ThingSlotItem != null)
		{
			for (int i = 0; i < prefab.Slots.Count; i++)
			{
				Slot slot = prefab.Slots[i];
				StationSlotsInsert stationSlotsInsert = new StationSlotsInsert();
				stationSlotsInsert.SlotName = slot.DisplayName;
				stationSlotsInsert.SlotIndex = i.ToString();
				stationSlotsInsert.SlotIcon = Slot.GetSlotTypeSprite(slot.Type);
				stationSlotsInsert.SlotType = Localization.GetName(slot);
				page.SlotInserts.Add(stationSlotsInsert);
			}
		}
		if (prefab is IProxySlot)
		{
			StationSlotsInsert stationSlotsInsert2 = new StationSlotsInsert();
			stationSlotsInsert2.SlotName = GameStrings.TargetSlot;
			stationSlotsInsert2.SlotIndex = 255.ToString();
			stationSlotsInsert2.SlotIcon = Slot.GetSlotTypeSprite(Slot.Class.None);
			stationSlotsInsert2.SlotType = GameStrings.Proxy;
			page.SlotInserts.Add(stationSlotsInsert2);
		}
	}

	private static void AddLifeRequirementInfo(Thing prefab, ref StationpediaPage page)
	{
		if (prefab is Plant plant && !(plant is Seed))
		{
			plant.lifeRequirements.SetOwnerPlant(plant);
			plant.lifeRequirements.AddToStationpedia(ref page);
		}
	}

	private void AddLogicModeInfo(Thing prefab, ref StationpediaPage page)
	{
		if (!(prefab is RocketAvionicsDevice) && !prefab.HasModeState)
		{
			return;
		}
		if (prefab.ModeStrings == null)
		{
			Debug.LogError("ModeStrings null for " + prefab.PrefabName);
		}
		else
		{
			if (prefab is LogicDial)
			{
				return;
			}
			for (int i = 0; i < prefab.ModeStrings.Length; i++)
			{
				string logicName = prefab.ModeStrings[i];
				StationLogicInsert stationLogicInsert = new StationLogicInsert();
				try
				{
					stationLogicInsert.LogicAccessTypes = i.ToString();
					stationLogicInsert.LogicName = logicName;
					page.ModeInsert.Add(stationLogicInsert);
				}
				catch (FormatException ex)
				{
					Debug.LogError("There was an error with text " + LogicTypeItem.Parsed + " " + ex.Message);
				}
			}
		}
	}

	private void AddConnectionInfo(Thing prefab, ref StationpediaPage page)
	{
		if (!(prefab is SmallGrid smallGrid) || smallGrid.OpenEnds.Count == 0)
		{
			return;
		}
		for (int i = 0; i < smallGrid.OpenEnds.Count; i++)
		{
			Connection connection = smallGrid.OpenEnds[i];
			StationLogicInsert stationLogicInsert = new StationLogicInsert();
			try
			{
				stationLogicInsert.LogicAccessTypes = i.ToString();
				stationLogicInsert.LogicName = connection.ToStationpediaName();
				page.ConnectionInsert.Add(stationLogicInsert);
			}
			catch (FormatException ex)
			{
				Debug.LogError("There was an error with text " + LogicTypeItem.Parsed + " " + ex.Message);
			}
		}
	}

	private void AddLogicTypeInfo(Thing prefab, ref StationpediaPage page)
	{
		ILogicable logicable = prefab as ILogicable;
		if (logicable is DeviceInputOutput deviceInputOutput)
		{
			foreach (Connection openEnd in deviceInputOutput.OpenEnds)
			{
				openEnd.Validate();
			}
		}
		if (logicable == null || LogicTypeItem == null)
		{
			return;
		}
		LogicType[] values = EnumCollections.LogicTypes.Values;
		for (int i = 0; i < values.Length; i++)
		{
			LogicType logicType = values[i];
			bool flag = logicable.CanLogicRead(logicType);
			bool flag2 = logicable.CanLogicWrite(logicType);
			if (!flag2 && !flag)
			{
				continue;
			}
			string helpText = "{LOGICTYPE:" + logicType.ToString() + "}";
			helpText = Localization.ParseHelpText(helpText);
			StationLogicInsert stationLogicInsert = new StationLogicInsert();
			try
			{
				if (flag2 && !flag)
				{
					stationLogicInsert.LogicAccessTypes = GameStrings.LogicWrite;
				}
				else if (!flag2 && flag)
				{
					stationLogicInsert.LogicAccessTypes = GameStrings.LogicRead;
				}
				else
				{
					stationLogicInsert.LogicAccessTypes = GameStrings.LogicReadWrite;
				}
				stationLogicInsert.LogicName = helpText;
				page.LogicInsert.Add(stationLogicInsert);
			}
			catch (FormatException ex)
			{
				Debug.LogError("There was an error with text " + LogicTypeItem.Parsed + " " + ex.Message);
			}
		}
	}

	private void AddBindingsInfo(Thing prefab, ref StationpediaPage page)
	{
		if (!(prefab is ICircuitHolder circuitHolder))
		{
			return;
		}
		if (circuitHolder.GetLogicBindings() == null)
		{
			page.HasBindings = false;
			return;
		}
		foreach (LogicBinding logicBinding in circuitHolder.GetLogicBindings())
		{
			page.HasBindings = true;
			try
			{
				StationBinding item = new StationBinding
				{
					Header = logicBinding.Header,
					Label = logicBinding.Label
				};
				page.LogicBindings.Add(item);
			}
			catch (FormatException ex)
			{
				ConsoleWindow.PrintError("There was an error with text " + prefab.PrefabName + " " + ex.Message);
			}
		}
	}

	private void AddInstructionInfo(Thing prefab, ref StationpediaPage page)
	{
		if (!(prefab is IMemory memory))
		{
			page.MemorySize = NetworkHelper.GetBytesReadable(0L);
			page.MemoryAccess = GameStrings.MemoryAccessNone;
			page.HasMemory = false;
			return;
		}
		page.HasMemory = true;
		page.MemorySize = NetworkHelper.GetBytesReadable(memory.GetStackSize() * 8);
		StationpediaPage obj = page;
		Assets.Scripts.Localization2.GameString gameString = ((!(memory is IMemoryReadable memoryReadable)) ? ((!(memory is IMemoryWritable)) ? GameStrings.MemoryAccessNone : GameStrings.LogicWrite) : ((!(memoryReadable is IMemoryWritable)) ? GameStrings.LogicRead : GameStrings.LogicReadWrite));
		obj.MemoryAccess = gameString;
		if (!(memory is IInstructable instructable))
		{
			return;
		}
		IEnumCollection instructions = instructable.GetInstructions();
		for (int i = 1; i < instructions.Length; i++)
		{
			int intFromIndex = instructions.GetIntFromIndex(i);
			string text = "<link=Clipboard><color=white><color=#20B2AA>" + instructions.GetEnumTypeName() + "</color>." + instructions.GetNameFromIndex(i) + "</color></link> <color=grey>OP_CODE: " + intFromIndex + "</color>";
			StationInstruction stationInstruction = new StationInstruction();
			try
			{
				stationInstruction.Text = text;
				stationInstruction.Info = instructable.GetInstructionDescription(i);
				stationInstruction.Index = i.ToString("X");
				page.LogicInstructions.Add(stationInstruction);
			}
			catch (FormatException ex)
			{
				ConsoleWindow.PrintError("There was an error with text " + text + " " + ex.Message);
			}
		}
	}

	private void AddLogicSlotTypeInfo(Thing prefab, ref StationpediaPage page)
	{
		if (prefab.Slots == null || !(prefab is ILogicable logicable) || LogicTypeItem == null)
		{
			return;
		}
		LogicSlotType[] logicSlotTypes = Logicable.LogicSlotTypes;
		for (int i = 0; i < logicSlotTypes.Length; i++)
		{
			LogicSlotType logicSlotType = logicSlotTypes[i];
			bool flag = false;
			for (int j = 0; j < prefab.Slots.Count; j++)
			{
				if (prefab.Slots[j] != null)
				{
					flag = logicable.CanLogicRead(logicSlotType, j);
					if (flag)
					{
						break;
					}
				}
			}
			if (!flag)
			{
				continue;
			}
			string helpText = "{LOGICSLOTTYPE:" + logicSlotType.ToString() + "}";
			helpText = Localization.ParseHelpText(helpText);
			StationLogicInsert stationLogicInsert = new StationLogicInsert();
			try
			{
				stationLogicInsert.LogicAccessTypes = string.Empty;
				for (int k = 0; k < prefab.Slots.Count; k++)
				{
					if (prefab.Slots[k] != null && logicable.CanLogicRead(logicSlotType, k))
					{
						if (!string.IsNullOrEmpty(stationLogicInsert.LogicAccessTypes))
						{
							stationLogicInsert.LogicAccessTypes += ", ";
						}
						stationLogicInsert.LogicAccessTypes += $"{k}";
					}
				}
				stationLogicInsert.LogicName = helpText;
				page.LogicSlotInsert.Add(stationLogicInsert);
			}
			catch (FormatException ex)
			{
				Debug.LogError("There was an error with text " + LogicTypeItem.Parsed + " " + ex.Message);
			}
		}
	}

	private void AddMadeBy(Thing prefab, ref StationpediaPage page)
	{
		DynamicThing dynamicThing = prefab as DynamicThing;
		if (!dynamicThing || CreatorItem == null || dynamicThing is Plant)
		{
			return;
		}
		List<RecipeReference> allMyCreators = ElectronicReader.GetAllMyCreators(dynamicThing);
		if (allMyCreators == null)
		{
			return;
		}
		existingCreators = new List<int>();
		for (int i = 0; i < allMyCreators.Count; i++)
		{
			StationBuildCostInsert stationBuildCostInsert = new StationBuildCostInsert();
			RecipeReference recipeReference = allMyCreators[i];
			if (!(recipeReference.Creator is Fabricator))
			{
				stationBuildCostInsert.PrinterName = Localization.ParseHelpText("{THING:" + recipeReference.Creator.PrefabName + "}");
				if (dynamicThing.RecipeTier != MachineTier.Undefined && dynamicThing.RecipeTier != MachineTier.Max)
				{
					stationBuildCostInsert.TierName = Localization.GetInterface(dynamicThing.RecipeTier.ToString());
				}
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine(recipeReference.Source ? Localization.ParseHelpText("{THING:" + recipeReference.Source.PrefabName + "}") : recipeReference.Recipe.ToString(recipeReference));
				stationBuildCostInsert.Description = stringBuilder.ToString();
				stationBuildCostInsert.PrinterImage = recipeReference.Creator.Thumbnail;
				stationBuildCostInsert.PageLink = "Thing" + recipeReference.Creator.PrefabName;
				try
				{
					page.HowToBuild.Add(stationBuildCostInsert);
					existingCreators.Add(allMyCreators[i].Creator.PrefabHash);
				}
				catch (FormatException ex)
				{
					Debug.LogError("There was an error with text " + CreatorItem.Parsed + " " + ex.Message);
				}
			}
		}
	}

	private void AddBuildStates(Thing prefab, ref StationpediaPage page)
	{
		if (!(prefab is Structure structure) || structure.BuildStates.Count <= 0)
		{
			return;
		}
		foreach (BuildState buildState in structure.BuildStates)
		{
			if (!buildState.DamagedBuildState)
			{
				StationBuildCostInsert stationBuildCostInsert = new StationBuildCostInsert();
				stationBuildCostInsert.PrinterName = string.Empty;
				StringBuilder stringBuilder = new StringBuilder();
				StringBuilder stringBuilder2 = new StringBuilder();
				if (buildState.CanManufacture)
				{
					buildState.ManufactureDat?.AddString(stringBuilder);
				}
				if (stringBuilder2.Length > 0)
				{
					stringBuilder2.AppendLine();
				}
				if (buildState.Tool.ToolEntry != null)
				{
					stringBuilder2.AppendLine(buildState.Tool.ToolEntry.GetToolString(buildState.Tool.EntryQuantity));
				}
				if (buildState.Tool.ToolEntry2 != null)
				{
					stringBuilder2.AppendLine(buildState.Tool.ToolEntry2.GetToolString(buildState.Tool.EntryQuantity2));
				}
				stationBuildCostInsert.PrinterImage = (buildState.Thumbnail ? buildState.Thumbnail : prefab.Thumbnail);
				stationBuildCostInsert.Details = stringBuilder.ToString();
				stationBuildCostInsert.Description = stringBuilder2.ToString();
				try
				{
					page.BuildStates.Add(stationBuildCostInsert);
				}
				catch (FormatException ex)
				{
					Debug.LogError("There was an error with text " + CreatorItem.Parsed + " " + ex.Message);
				}
			}
		}
	}

	private void AddConstructs(Thing prefab, ref StationpediaPage page)
	{
		if (!(prefab is IConstructionKit constructionKit))
		{
			return;
		}
		List<Thing> constructedPrefabs = constructionKit.GetConstructedPrefabs();
		for (int i = 0; i < constructedPrefabs.Count; i++)
		{
			StationCategoryInsert stationCategoryInsert = new StationCategoryInsert();
			Thing thing = constructedPrefabs[i];
			if (!(thing == null))
			{
				try
				{
					stationCategoryInsert.NameOfThing = Localization.GetThingName(thing.PrefabName);
					stationCategoryInsert.PageLink = "Thing" + thing.PrefabName;
					stationCategoryInsert.PrefabHash = thing.PrefabHash;
					stationCategoryInsert.InsertImage = thing.GetThumbnail();
					page.ConstructedByKits.Add(stationCategoryInsert);
				}
				catch (FormatException ex)
				{
					Debug.LogError("There was an error with text " + thing.PrefabName + " " + ex.Message);
				}
			}
		}
	}

	private void AddUsedIn(Thing prefab, ref StationpediaPage page)
	{
		List<IResourceConsumer> resourceConsumers = Thing.GetResourceConsumers(prefab);
		if (resourceConsumers == null || prefab.HideInStationpedia)
		{
			return;
		}
		for (int i = 0; i < resourceConsumers.Count; i++)
		{
			IResourceConsumer resourceConsumer = resourceConsumers[i];
			if (resourceConsumer != null)
			{
				StationCategoryInsert item = StationCategoryInsert.MakeAsConsumer(prefab, resourceConsumer);
				page.UsedIn.Add(item);
			}
		}
	}

	private void AddCombustion(Chemistry.GasType gasType, ref StationpediaPage page)
	{
		Chemistry.GasType gasType2 = Chemistry.GasType.Undefined;
		Chemistry.GasType gasType3 = Chemistry.GasType.Undefined;
		if (Combustion.IsFuel(gasType))
		{
			gasType2 = gasType;
		}
		else if (Combustion.IsOxidiser(gasType))
		{
			gasType3 = gasType;
		}
		else if (Combustion.IsHypergolic(gasType))
		{
			gasType2 = gasType;
			gasType3 = gasType;
		}
		else
		{
			if (gasType != Chemistry.GasType.Water && gasType != Chemistry.GasType.Steam)
			{
				return;
			}
			Chemistry.GasType[] fuels = Combustion.Fuels;
			foreach (Chemistry.GasType gasType4 in fuels)
			{
				Chemistry.GasType[] oxidisers = Combustion.Oxidisers;
				foreach (Chemistry.GasType gasType5 in oxidisers)
				{
					if (!Combustion.TryGetResult(gasType4, gasType5, out var result))
					{
						continue;
					}
					bool flag = false;
					CombustionValue[] outputs = result.Outputs;
					for (int k = 0; k < outputs.Length; k++)
					{
						Chemistry.GasType gasType6 = outputs[k].GasType;
						if (gasType6 == Chemistry.GasType.Steam || gasType6 == Chemistry.GasType.Water)
						{
							flag = true;
						}
					}
					if (flag)
					{
						StationCombustionInsert item = new StationCombustionInsert(gasType4, gasType5);
						page.CombustionInserts.Add(item);
					}
				}
			}
		}
		CombustionResult result2;
		if (gasType2 == Chemistry.GasType.Undefined)
		{
			Chemistry.GasType[] fuels = Combustion.Fuels;
			foreach (Chemistry.GasType gasType7 in fuels)
			{
				if (Combustion.TryGetResult(gasType7, gasType3, out result2))
				{
					StationCombustionInsert item2 = new StationCombustionInsert(gasType7, gasType3);
					page.CombustionInserts.Add(item2);
				}
			}
		}
		else if (gasType3 == Chemistry.GasType.Undefined)
		{
			Chemistry.GasType[] fuels = Combustion.Oxidisers;
			foreach (Chemistry.GasType gasType8 in fuels)
			{
				if (Combustion.TryGetResult(gasType2, gasType8, out result2))
				{
					StationCombustionInsert item3 = new StationCombustionInsert(gasType2, gasType8);
					page.CombustionInserts.Add(item3);
				}
			}
		}
		else if (Combustion.TryGetResult(gasType2, gasType3, out result2))
		{
			StationCombustionInsert item4 = new StationCombustionInsert(gasType2, gasType3);
			page.CombustionInserts.Add(item4);
		}
	}

	public static SPDAHomePageButtonOverride GetHomePageOverride(string pageKey)
	{
		foreach (SPDAHomePageButtonOverride homePageOverride in HomePageOverrides)
		{
			if (homePageOverride.LinkedPageKey == pageKey)
			{
				return homePageOverride;
			}
		}
		return null;
	}

	private void AddResources(Thing prefab, ref StationpediaPage page)
	{
		if (!(prefab is IResourceConsumer resourceConsumer))
		{
			return;
		}
		if (prefab is IConsumesAllOres)
		{
			StationCategoryInsert item = StationCategoryInsertSpecial.MakeSpecialPage("OrePage");
			page.ResourcesUsed.Add(item);
		}
		if (prefab is IConsumesAllIngots)
		{
			StationCategoryInsert item2 = StationCategoryInsertSpecial.MakeSpecialPage("IngotPage");
			page.ResourcesUsed.Add(item2);
		}
		List<Item> resourcesUsed = resourceConsumer.GetResourcesUsed();
		for (int i = 0; i < resourcesUsed.Count; i++)
		{
			Item item3 = resourcesUsed[i];
			if (!(item3 == null) && (!(prefab is IConsumesAllOres) || !(item3 is Ore)) && (!(prefab is IConsumesAllIngots) || !(item3 is Ingot)))
			{
				StationCategoryInsert item4 = StationCategoryInsert.MakeAsResource(prefab, item3);
				page.ResourcesUsed.Add(item4);
			}
		}
	}

	private void AddDevice(Thing prefab, ref StationpediaPage page)
	{
		if (!(prefab is Device { UsedPower: >0f } device))
		{
			return;
		}
		try
		{
			page.BasePowerDraw = device.UsedPower.ToStringPrefix("W", "yellow");
		}
		catch (FormatException ex)
		{
			Debug.LogError("There was an error with text " + DevicePage.Parsed + " " + ex.Message);
		}
	}

	private void AddChargable(Thing prefab, ref StationpediaPage page)
	{
		if (!(prefab is IChargable chargable))
		{
			return;
		}
		try
		{
			page.PowerStorage = chargable.GetPowerMaximum().ToStringPrefix("W", "yellow");
		}
		catch (FormatException ex)
		{
			Debug.LogError("There was an error with text " + DevicePage.Parsed + " " + ex.Message);
		}
	}

	private void AddPowerGeneration(Thing prefab, ref StationpediaPage page)
	{
		if (!(prefab is IPowerGenerator powerGenerator))
		{
			return;
		}
		try
		{
			page.PowerGeneration = powerGenerator.GetMaxPowerGenerated().ToStringPrefix("W", "yellow");
		}
		catch (FormatException ex)
		{
			Debug.LogError("There was an error with text " + DevicePage.Parsed + " " + ex.Message);
		}
	}

	private void AddGasCanister(Thing prefab, ref StationpediaPage page)
	{
		if (prefab is GasCanister gasCanister)
		{
			try
			{
				page.MaxPressure = string.Format(GasCanisterPage.Parsed, gasCanister.MaxPressure.ToFloat().ToString(CultureInfo.CurrentCulture) + " kPa");
				return;
			}
			catch (FormatException ex)
			{
				Debug.LogError("There was an error with text " + GasCanisterPage.Parsed + " " + ex.Message);
				return;
			}
		}
		if (prefab is DynamicGasCanister dynamicGasCanister)
		{
			try
			{
				page.MaxPressure = string.Format(GasCanisterPage.Parsed, dynamicGasCanister.MaxSetting.ToString(CultureInfo.CurrentCulture) + " kPa");
			}
			catch (FormatException ex2)
			{
				Debug.LogError("There was an error with text " + GasCanisterPage.Parsed + " " + ex2.Message);
			}
		}
	}

	private void AddTank(Thing prefab, ref StationpediaPage page)
	{
		if (!(prefab is Tank tank))
		{
			return;
		}
		if (tank.ContentType == Pipe.ContentType.Liquid)
		{
			try
			{
				page.MaxPressure = string.Format(GasCanisterPage.Parsed, $"{Chemistry.Limits.MAXPressureLiquidPipe.ToFloat()} kPa");
				return;
			}
			catch (FormatException ex)
			{
				Debug.LogError("There was an error with text " + GasCanisterPage.Parsed + " " + ex.Message);
				return;
			}
		}
		try
		{
			page.MaxPressure = string.Format(GasCanisterPage.Parsed, $"{Chemistry.Limits.MAXPressureGasPipe.ToFloat()} kPa");
		}
		catch (FormatException ex2)
		{
			Debug.LogError("There was an error with text " + GasCanisterPage.Parsed + " " + ex2.Message);
		}
	}

	private void AddAtmospherics(Thing prefab, ref StationpediaPage page)
	{
		if (prefab is IVolume volume)
		{
			try
			{
				page.Volume = string.Format(AtmosphericsPage.Parsed, volume.GetVolume.ToFloat().ToString(CultureInfo.InvariantCulture)) + "L";
			}
			catch (FormatException ex)
			{
				Debug.LogError("There was an error with text " + AtmosphericsPage.Parsed + " " + ex.Message);
			}
		}
	}

	private void AddGrowthTime(Thing prefab, ref StationpediaPage page)
	{
		if (!(prefab is Plant plant) || plant is Seed)
		{
			return;
		}
		float num = 0f;
		for (int i = 0; i < plant.GrowthStates.Count; i++)
		{
			PlantStage plantStage = plant.GrowthStates[i];
			if (plantStage.Length <= 0f || plantStage.Mature)
			{
				break;
			}
			num += plantStage.Length;
		}
		page.GrowthTime = ValueDisplay.GetUnitValue(num, ValueDisplay.Unit.Time);
	}

	private void AddRocketInfo(Thing prefab, ref StationpediaPage page)
	{
		if ((prefab is IRocketComponent rocketComponent && !(rocketComponent is IRocketInternals { InternalCellType: RocketInternalCellType.None })) ? true : false)
		{
			page.PlaceableInRocket = GameStrings.ConstructableInRocketsTrue;
			if (prefab is IRocketMassContributor rocketMassContributor)
			{
				page.RocketMass = rocketMassContributor.MassContribution.ToStringRounded() + "kg";
			}
			if (prefab is IRocketEngine rocketEngine)
			{
				float d = rocketEngine.MaxThrust / 1000f;
				float d2 = rocketEngine.MaxExhaustVelocity / 1000f;
				page.RocketEngineForce = " " + StringManager.Get(d.RoundToSignificantDigits(3)) + "kN";
				page.RocketEngineEfficiency = " " + StringManager.Get(rocketEngine.EfficiencyPercent.RoundToSignificantDigits(3)) + "%";
				page.RocketEngineExhaustVelocity = StringManager.Get(d2.RoundToSignificantDigits(3)) + "km/s (Isp: " + StringManager.Get(rocketEngine.SpecificImpulse.RoundToSignificantDigits(3)) + "s)";
			}
		}
	}

	private void AddDrillHeadInfo(Thing prefab, ref StationpediaPage page)
	{
		if (prefab is RocketMiningDrillHead drillHead)
		{
			page.DrillHeadProperties = new StationDrillHeadProperties(drillHead);
		}
	}

	private void AddSuitThermalsInfo(Thing prefab, ref StationpediaPage page)
	{
		if (prefab is SuitBase suitBase)
		{
			page.SolarHeatingFactorText = StringManager.Get(suitBase.SolarHeatingFactor * prefab.SurfaceArea) ?? "";
			page.StationSuitInfo = new StationSuitProperties(suitBase);
		}
	}

	private void AddNutrition(Thing prefab, ref StationpediaPage page)
	{
		if (prefab is INutrition nutrition)
		{
			try
			{
				page.Nutrition = string.Format(NutritionPage.Parsed, nutrition.GetNutritionalValue());
				page.NutritionQuality = Food.GetFoodQualityStationpediaDescription(nutrition);
				string color = ((nutrition.MoodBonus > 0f) ? "yellow" : "red");
				page.MoodBonus = ((nutrition.MoodBonus != 0f) ? (nutrition.MoodBonus * 100f).ToStringPercent(color) : ((string)GameStrings.None));
			}
			catch (FormatException ex)
			{
				Debug.LogError("There was an error with text " + NutritionPage.Parsed + " " + ex.Message);
			}
		}
	}

	private void AddInsertToPage(Thing insertThing, List<StationCategoryInsert> insertsList)
	{
		StationCategoryInsert stationCategoryInsert = new StationCategoryInsert();
		try
		{
			stationCategoryInsert.PageLink = "Thing" + insertThing.PrefabName;
			stationCategoryInsert.NameOfThing = insertThing.DisplayName;
			stationCategoryInsert.PrefabHash = insertThing.PrefabHash;
			stationCategoryInsert.InsertImage = insertThing.GetThumbnail();
			insertsList.Add(stationCategoryInsert);
		}
		catch (FormatException)
		{
		}
	}

	private void AddCreates(Thing prefab, ref StationpediaPage page)
	{
		if (prefab is IProducesAllOres)
		{
			StationCategoryInsert item = StationCategoryInsertSpecial.MakeSpecialPage("OrePage");
			page.ProducedThingsInserts.Add(item);
		}
		if (prefab is IProducesAllIngots)
		{
			StationCategoryInsert item2 = StationCategoryInsertSpecial.MakeSpecialPage("IngotPage");
			page.ProducedThingsInserts.Add(item2);
		}
		if (prefab is Seed seed)
		{
			AddInsertToPage(seed.PlantType, page.ProducedThingsInserts);
		}
		if (prefab is Plant plant)
		{
			foreach (Thing allPrefab in Prefab.AllPrefabs)
			{
				if (allPrefab is Seed seed2 && !(seed2.PlantType != plant))
				{
					AddInsertToPage(seed2, page.ConstructedByKits);
				}
			}
		}
		List<RecipeReference> allRecipies = ElectronicReader.GetAllRecipies(prefab);
		List<int> list = new List<int>();
		if (allRecipies == null)
		{
			return;
		}
		for (int i = 0; i < allRecipies.Count; i++)
		{
			RecipeReference recipeReference = allRecipies[i];
			if ((!(recipeReference.DynamicThing is Ore) || !(prefab is IProducesAllOres)) && (!(recipeReference.DynamicThing is Ingot) || !(prefab is IProducesAllIngots)) && !list.Contains(recipeReference.DynamicThing.PrefabHash))
			{
				StationCategoryInsert stationCategoryInsert = new StationCategoryInsert();
				list.Add(recipeReference.DynamicThing.PrefabHash);
				try
				{
					stationCategoryInsert.PageLink = "Thing" + recipeReference.DynamicThing.PrefabName;
					stationCategoryInsert.NameOfThing = recipeReference.DynamicThing.DisplayName;
					stationCategoryInsert.PrefabHash = recipeReference.DynamicThing.PrefabHash;
					stationCategoryInsert.InsertImage = recipeReference.DynamicThing.GetThumbnail();
					page.ProducedThingsInserts.Add(stationCategoryInsert);
				}
				catch (FormatException)
				{
				}
			}
		}
	}

	private void AddConstructedBy(Thing prefab, ref StationpediaPage page)
	{
		List<IConstructionKit> allConstructors = ElectronicReader.GetAllConstructors(prefab);
		if (allConstructors == null)
		{
			return;
		}
		for (int i = 0; i < allConstructors.Count; i++)
		{
			IConstructionKit constructionKit = allConstructors[i];
			try
			{
				StationCategoryInsert stationCategoryInsert = new StationCategoryInsert();
				stationCategoryInsert.PageLink = "Thing" + constructionKit.GetPrefabName();
				stationCategoryInsert.NameOfThing = Localization.GetThingName(constructionKit.GetPrefabName());
				stationCategoryInsert.PrefabHash = Animator.StringToHash(constructionKit.GetPrefabName());
				if (Prefab.Find(constructionKit.GetPrefabName()) == null || Prefab.Find(constructionKit.GetPrefabName()).GetThumbnail() == null)
				{
					Debug.LogError(constructionKit.GetPrefabName() + " . Get prefab name is either null or is its thumb is null please check. Skipping object to load game");
				}
				else
				{
					stationCategoryInsert.InsertImage = Prefab.Find(constructionKit.GetPrefabName()).GetThumbnail();
				}
				page.ConstructedThings.Add(stationCategoryInsert);
			}
			catch (FormatException ex)
			{
				Debug.LogError("There was an error with text " + constructionKit.GetPrefabName() + " " + ex.Message);
			}
		}
	}

	private void AddModeStrings(Thing prefab, ref StationpediaPage page)
	{
		if (!(prefab != null) || !prefab.HasModeState || ModeStringItem == null || prefab.ModeStrings == null)
		{
			return;
		}
		for (int i = 0; i < prefab.ModeStrings.Length; i++)
		{
			string text = prefab.ModeStrings[i];
			try
			{
				page.Text += string.Format("\n" + ModeStringItem.Parsed, i, text.ToString());
			}
			catch (FormatException ex)
			{
				Debug.LogError("There was an error with text " + ModeStringItem.Parsed + " " + ex.Message);
			}
		}
	}

	private void AddCreatedReagent(Thing prefab, ref StationpediaPage page)
	{
		Item item = prefab as Item;
		if (item != null && item.ReagentMixture != null && item.CreatedReagentMixture != null && item.CreatedReagentMixture.TotalReagents > 0.0 && CreatedReagent != null)
		{
			try
			{
				page.FoundInOre = item.ReagentMixture.ToStationpediaString();
			}
			catch (FormatException ex)
			{
				Debug.LogError("There was an error with text " + CreatedReagent.Parsed + " " + ex.Message);
			}
		}
	}

	private void AddCreatedGases(Thing prefab, ref StationpediaPage page)
	{
		Ore ore = prefab as Ore;
		StationFoundInInsert stationFoundInInsert = new StationFoundInInsert();
		if (!(ore != null) || ore is PureIce)
		{
			return;
		}
		for (int i = 0; i < ore.SpawnContents.Count; i++)
		{
			stationFoundInInsert = new StationFoundInInsert();
			SpawnGas spawnGas = ore.SpawnContents[i];
			try
			{
				stationFoundInInsert.NameOfThing = Localization.ParseHelpText("{GAS:" + spawnGas.Name + "}");
				stationFoundInInsert.QuantityOfThing = spawnGas.Quantity.ToStringPrefix("mol", "yellow");
				page.FoundInGas.Add(stationFoundInInsert);
			}
			catch (FormatException ex)
			{
				Debug.LogError("There was an error with text " + spawnGas.Type.ToString() + " " + ex.Message);
			}
		}
	}

	private void AddFermentedGases(Thing prefab, ref StationpediaPage page)
	{
		if (!(prefab is IFermentable fermentable))
		{
			return;
		}
		for (int i = 0; i < fermentable.SpawnGasList.Length; i++)
		{
			StationFoundInInsert stationFoundInInsert = new StationFoundInInsert();
			SpawnGas spawnGas = fermentable.SpawnGasList[i];
			try
			{
				stationFoundInInsert.NameOfThing = Localization.ParseHelpText("{GAS:" + spawnGas.Name + "}");
				stationFoundInInsert.QuantityOfThing = spawnGas.Quantity.ToStringPrefix("mol", "yellow");
				page.FoundInFermentation.Add(stationFoundInInsert);
			}
			catch (FormatException ex)
			{
				Debug.LogError($"There was an error with text {spawnGas.Type} {ex.Message}");
			}
		}
	}

	private void PopulateThingPages()
	{
		StationpediaPage page = GetPage("ThingTemplate");
		StationpediaPage page2 = GetPage("ThingThermalTemplate");
		ThingSlotHeader = GetPage("ThingSlotHeaderTemplate");
		ThingSlotItem = GetPage("ThingSlotItemTemplate");
		CreatorHeader = GetPage("CreatorsHeaderTemplate");
		CreatorItem = GetPage("CreatorTemplate");
		LogicTypeHeader = GetPage("LogicTypeHeaderTemplate");
		LogicTypeItem = GetPage("LogicTypeTemplate");
		CreatedHeader = GetPage("CreatedHeaderTemplate");
		ConstructedHeader = GetPage("ConstructedHeaderTemplate");
		ConstructedByHeader = GetPage("ConstructedByHeaderTemplate");
		ModeStringHeader = GetPage("ModeStringHeaderTemplate");
		ModeStringItem = GetPage("ModeStringItemTemplate");
		CreatedReagent = GetPage("CreatedReagentTemplate");
		ThingStack = GetPage("ThingStackTemplate");
		ThingNutrition = GetPage("ThingNutritionTemplate");
		IceMelting = GetPage("IceMeltingTemplate");
		DevicePage = GetPage("DeviceTemplate");
		GasCanisterPage = GetPage("GasCanisterTemplate");
		AtmosphericsPage = GetPage("AtmosphericsTemplate");
		NutritionPage = GetPage("NutritionPageTemplate");
		CreatedGases = GetPage("CreatedGasesTemplate");
		ThingTransmissableTemplate = GetPage("ThingTransmissibleTemplate");
		TransmitTemplate = GetPage("LogicTransmitHeaderTemplate");
		TierListHeader = GetPage("TierListHeaderTemplate");
		DataHandler.HandleThingPageOverrides();
		foreach (Thing allPrefab in Prefab.AllPrefabs)
		{
			StationpediaPage page3 = new StationpediaPage($"Thing{allPrefab.PrefabName}", allPrefab.DisplayName);
			DataHandler.HiddenInPedia.TryGetValue(allPrefab.PrefabName, out var value);
			if (allPrefab.HideInStationpedia || value)
			{
				continue;
			}
			if (allPrefab is IQuantity quantity && ThingStack != null)
			{
				try
				{
					page3.StackSizeText = StringManager.Get(quantity.GetMaxQuantity);
				}
				catch (FormatException ex)
				{
					Debug.LogError("There was an error with text " + ThingStack.Parsed + " " + ex.Message);
				}
			}
			Ice ice = allPrefab as Ice;
			if (ice != null && IceMelting != null)
			{
				try
				{
					if (ice is PureIce { GasType: not Chemistry.GasType.Undefined } pureIce)
					{
						Mole mole = new Mole(pureIce.GasType, MoleQuantity.Zero, MoleEnergy.Zero);
						page3.FreezeTemperatureText = mole.FreezingTemperature().ToFloat().ToStringPrefix("K") + " (" + StringManager.Get(RocketMath.KelvinToCelsius(mole.FreezingTemperature())) + " <sup>o</sup>C))";
					}
					else
					{
						page3.FreezeTemperatureText = ice.MeltTemperature.ToFloat().ToStringPrefix("K") + " (" + StringManager.Get(RocketMath.KelvinToCelsius(ice.MeltTemperature)) + " <sup>o</sup>C))";
					}
				}
				catch (FormatException ex2)
				{
					Debug.LogError("There was an error with text " + IceMelting.Parsed + " " + ex2.Message);
				}
			}
			if (page == null)
			{
				continue;
			}
			try
			{
				page3.PrefabName = allPrefab.PrefabName;
				page3.PrefabHash = allPrefab.PrefabHash;
				page3.PrefabHashString = allPrefab.PrefabHash.ToString();
				page3.PaintableText = ((allPrefab.PaintableMaterial != null) ? GameStrings.YesPaintable : GameStrings.NoPaintable);
				page3.Description = Localization.ParseHelpText(Localization.GetThingDescription(allPrefab.PrefabName));
				if (allPrefab is ITransmitable && ThingTransmissableTemplate != null)
				{
					try
					{
						page3.Description += string.Format(ThingTransmissableTemplate.Parsed);
					}
					catch (FormatException ex3)
					{
						Debug.LogError("There was an error with text " + ThingTransmissableTemplate.Parsed + " " + ex3.Message);
					}
				}
			}
			catch (FormatException ex4)
			{
				Debug.LogError("There was an error with text " + page.Parsed + " " + ex4.Message);
			}
			AddFermentedGases(allPrefab, ref page3);
			AddCreatedReagent(allPrefab, ref page3);
			AddCreatedGases(allPrefab, ref page3);
			AddConstructedBy(allPrefab, ref page3);
			if (allPrefab is Structure structure && structure.MaxPressureDelta > PressurekPa.Zero)
			{
				page3.PressureBreakText = StringManager.Get(structure.MaxPressureDelta.ToFloat()) + "kpa";
			}
			if (allPrefab is IThermal thermal)
			{
				page3.RadiationFactorText = StringManager.Get(thermal.RadiationFactor * allPrefab.SurfaceArea) ?? "";
				page3.ConvectionFactorText = StringManager.Get(thermal.ConvectionFactor * allPrefab.SurfaceArea) ?? "";
				page3.SolarHeatingFactorText = StringManager.Get(thermal.SolarHeatingFactor * allPrefab.SurfaceArea) ?? "";
			}
			if (allPrefab is Cable cable)
			{
				page3.CableBreakText = StringManager.Get(cable.MaxVoltage) + "W";
			}
			if (allPrefab is Pipe pipe)
			{
				PressurekPa pressurekPa = ((pipe.PipeContentType == Pipe.ContentType.Liquid) ? Chemistry.Limits.MAXPressureLiquidPipe : Chemistry.Limits.MAXPressureGasPipe);
				page3.PressureBreakText = StringManager.Get(pressurekPa.ToFloat()) + "kpa";
			}
			try
			{
				page3.AutoIgnitionText = ((allPrefab.AutoignitionTemperature < TemperatureKelvin.Zero) ? "" : (allPrefab.AutoignitionTemperature.ToFloat().ToStringPrefix("K") + " (" + StringManager.Get(RocketMath.KelvinToCelsius(allPrefab.AutoignitionTemperature)) + "<sup>o</sup>C)"));
				page3.FlashpointText = ((allPrefab.FlashPointTemperature < TemperatureKelvin.Zero) ? "" : (allPrefab.FlashPointTemperature.ToFloat().ToStringPrefix("K") + " (" + StringManager.Get(RocketMath.KelvinToCelsius(allPrefab.FlashPointTemperature)) + "<sup>o</sup>C)"));
			}
			catch (FormatException ex5)
			{
				Debug.LogError("There was an error with text " + page2.Parsed + " " + ex5.Message);
			}
			AddLifeRequirementInfo(allPrefab, ref page3);
			AddSlotInfo(allPrefab, ref page3);
			AddLogicModeInfo(allPrefab, ref page3);
			AddLogicTypeInfo(allPrefab, ref page3);
			AddLogicSlotTypeInfo(allPrefab, ref page3);
			AddBindingsInfo(allPrefab, ref page3);
			AddInstructionInfo(allPrefab, ref page3);
			AddConnectionInfo(allPrefab, ref page3);
			PopulateStructureTiers(allPrefab, ref page3);
			AddConstructs(allPrefab, ref page3);
			AddMadeBy(allPrefab, ref page3);
			AddBuildStates(allPrefab, ref page3);
			AddCreates(allPrefab, ref page3);
			AddResources(allPrefab, ref page3);
			AddUsedIn(allPrefab, ref page3);
			AddDevice(allPrefab, ref page3);
			AddChargable(allPrefab, ref page3);
			AddPowerGeneration(allPrefab, ref page3);
			AddGasCanister(allPrefab, ref page3);
			AddTank(allPrefab, ref page3);
			AddAtmospherics(allPrefab, ref page3);
			AddNutrition(allPrefab, ref page3);
			AddGrowthTime(allPrefab, ref page3);
			AddRocketInfo(allPrefab, ref page3);
			AddDrillHeadInfo(allPrefab, ref page3);
			AddSuitThermalsInfo(allPrefab, ref page3);
			page3.ParsePage();
			Register(page3);
		}
	}

	private void PopulateStructureTiers(Thing thing, ref StationpediaPage page)
	{
		Structure structure = thing as Structure;
		if (!structure)
		{
			return;
		}
		foreach (BuildState buildState in structure.BuildStates)
		{
			StationStructureVersionInsert stationStructureVersionInsert = new StationStructureVersionInsert();
			if (buildState.ManufactureDat.MachinesTier != MachineTier.Max && buildState.ManufactureDat.MachinesTier != MachineTier.Undefined)
			{
				stationStructureVersionInsert.StructureVersion = Localization.GetInterface(buildState.ManufactureDat.MachinesTier.ToString());
				stationStructureVersionInsert.CreationMultiplier = buildState.ManufactureDat.ItemSpawnMultiplier.ToString();
				stationStructureVersionInsert.EnergyCostMultiplier = buildState.ManufactureDat.EnergyCostMultiplier.ToString();
				stationStructureVersionInsert.MaterialCostMultiplier = buildState.ManufactureDat.MaterialCostMultiplier.ToString();
				stationStructureVersionInsert.BuildTimeMultiplier = buildState.ManufactureDat.BuildTimeMultiplier.ToString();
				page.StructVersionInsert.Add(stationStructureVersionInsert);
			}
		}
	}

	private void PopulateLogicVariables()
	{
		StationpediaPage page = GetPage("LogicTypePageTemplate");
		if (page == null)
		{
			return;
		}
		LogicType[] values = EnumCollections.LogicTypes.Values;
		for (int i = 0; i < values.Length; i++)
		{
			LogicType logicType = values[i];
			if (LogicBase.IsDeprecated(logicType))
			{
				continue;
			}
			try
			{
				string logicDescription = LogicBase.GetLogicDescription(logicType);
				string text = string.Format(page.Parsed, logicDescription);
				string text2 = EnumCollections.LogicTypes.GetName(logicType);
				string title = "LogicSlotType." + text2;
				StationpediaPage stationpediaPage = new StationpediaPage("LogicType" + text2, title, text);
				if (logicType == LogicType.SoundAlert)
				{
					for (int j = 0; j < EnumCollections.SpeakerSounds.Length; j++)
					{
						string logicName = EnumCollections.SpeakerSounds.Names[j];
						StationLogicInsert stationLogicInsert = new StationLogicInsert();
						try
						{
							stationLogicInsert.LogicAccessTypes = j.ToString();
							stationLogicInsert.LogicName = logicName;
							stationpediaPage.ModeInsert.Add(stationLogicInsert);
						}
						catch (FormatException ex)
						{
							Debug.LogError("There was an error with text " + LogicTypeItem.Parsed + " " + ex.Message);
						}
					}
				}
				stationpediaPage.Title = "LogicType." + EnumCollections.LogicTypes.GetName(logicType);
				stationpediaPage.Description = text;
				stationpediaPage.CustomSpriteToUse = VariableImage;
				stationpediaPage.ParsePage();
				Register(stationpediaPage);
			}
			catch (Exception ex2)
			{
				Debug.LogError("Unable to add logic page for " + logicType.ToString() + ":" + ex2.Message);
			}
		}
	}

	private void PopulateLogicSlotVariables()
	{
		StationpediaPage page = GetPage("LogicTypePageTemplate");
		if (page == null)
		{
			return;
		}
		LogicSlotType[] logicSlotTypes = Logicable.LogicSlotTypes;
		for (int i = 0; i < logicSlotTypes.Length; i++)
		{
			LogicSlotType logicSlotType = logicSlotTypes[i];
			if (!LogicBase.IsDeprecated(logicSlotType))
			{
				try
				{
					string logicDescription = LogicBase.GetLogicDescription(logicSlotType);
					string text = string.Format(page.Parsed, logicDescription);
					string text2 = EnumCollections.LogicSlotTypes.GetName(logicSlotType);
					string title = "LogicSlotType." + text2;
					StationpediaPage stationpediaPage = new StationpediaPage("LogicSlotType" + text2, title, text);
					stationpediaPage.Title = title;
					stationpediaPage.Description = text;
					stationpediaPage.CustomSpriteToUse = VariableImage;
					stationpediaPage.ParsePage();
					Register(stationpediaPage);
				}
				catch (Exception ex)
				{
					Debug.LogError("Unable to add logic page for " + logicSlotType.ToString() + ":" + ex.Message);
				}
			}
		}
	}

	private void PopulateFactionLorePages()
	{
		for (int i = 1; i < 14; i++)
		{
			StationpediaPage obj = new StationpediaPage
			{
				Title = Localization.GetInterface($"{(LoreFactions)i}"),
				CustomSpriteToUse = LoreFactionThumbnails[i - 1],
				Description = Localization.GetInterface($"{(LoreFactions)i}Desc")
			};
			LoreFactions loreFactions = (LoreFactions)i;
			obj.Key = loreFactions.ToString();
			obj.ParsePage();
			Register(obj);
		}
	}

	private void PopulateReagents()
	{
		if (CreatorItem == null)
		{
			return;
		}
		foreach (Reagent allReagent in Reagent.AllReagents)
		{
			StationpediaPage stationpediaPage = new StationpediaPage($"Reagent{allReagent.DisplayName}", allReagent.DisplayName + " (Reagent)", "");
			_ = string.Empty;
			List<Item> allSources = ElectronicReader.GetAllSources(allReagent);
			if (allSources != null)
			{
				for (int i = 0; i < allSources.Count; i++)
				{
					Item item = allSources[i];
					try
					{
						StationFoundInInsert stationFoundInInsert = new StationFoundInInsert();
						stationFoundInInsert.NameOfThing = Localization.ParseHelpText("{THING:" + allSources[i].PrefabName + "}");
						stationFoundInInsert.QuantityOfThing = allSources[i].QuantityPerUse.ToString();
						stationpediaPage.FoundInOre.Add(stationFoundInInsert);
						stationpediaPage.ReagentsType = allReagent.TypeNameShort;
						stationpediaPage.ReagentsHash = allReagent.Hash;
						stationpediaPage.UnitText = allReagent.Unit;
						stationpediaPage.Description = allReagent.ToString();
						stationpediaPage.CustomSpriteToUse = ReagentImage;
					}
					catch (FormatException ex)
					{
						Debug.LogError("There was an error with text " + item.PrefabName + " " + ex.Message);
					}
				}
			}
			try
			{
				stationpediaPage.Title = allReagent.DisplayName;
				stationpediaPage.UnitText = allReagent.Unit;
				stationpediaPage.CustomSpriteToUse = ReagentImage;
			}
			catch (FormatException ex2)
			{
				Debug.LogError("There was an error with text in ReagentPage " + ex2.Message);
				continue;
			}
			stationpediaPage.ParsePage();
			Register(stationpediaPage);
		}
	}

	private void PopulateGenes()
	{
		foreach (Gene item in EnumUtil.GetValues<Gene>().ToList())
		{
			StationpediaPage stationpediaPage = new StationpediaPage("Gene" + item, GeneHelper.DisplayName(item), string.Empty);
			stationpediaPage.Title = GeneHelper.DisplayName(item);
			stationpediaPage.Description = GeneHelper.Description(item);
			stationpediaPage.CustomSpriteToUse = GeneImage;
			stationpediaPage.ParsePage();
			Register(stationpediaPage);
		}
	}

	private void PopulateTrading()
	{
		Gene[] values = EnumCollections.Genes.Values;
		for (int i = 0; i < values.Length; i++)
		{
			Gene gene = values[i];
			StationpediaPage stationpediaPage = new StationpediaPage("Gene" + gene, GeneHelper.DisplayName(gene), string.Empty);
			stationpediaPage.Title = GeneHelper.DisplayName(gene);
			stationpediaPage.Description = GeneHelper.Description(gene);
			stationpediaPage.CustomSpriteToUse = GeneImage;
			stationpediaPage.ParsePage();
			Register(stationpediaPage);
		}
	}

	private void PopulateGases()
	{
		foreach (Mole gase in Chemistry.Gases)
		{
			StationpediaPage page = new StationpediaPage($"Gas{gase.Type}", gase.DisplayName + " (Gas)", string.Empty);
			List<Ore> allSources = ElectronicReader.GetAllSources(gase.Type);
			if (allSources != null)
			{
				for (int i = 0; i < allSources.Count; i++)
				{
					Ore ore = allSources[i];
					foreach (SpawnGas spawnContent in ore.SpawnContents)
					{
						if (gase.Type == spawnContent.Type)
						{
							try
							{
								StationFoundInInsert specificSpawnGasDat = spawnContent.GetSpecificSpawnGasDat(gase.Type, ore.PrefabName);
								specificSpawnGasDat.NameOfThing = Localization.ParseHelpText("{THING:" + ore.PrefabName + "}");
								specificSpawnGasDat.QuantityOfThing += " mol";
								page.FoundInOre.Add(specificSpawnGasDat);
							}
							catch (FormatException ex)
							{
								Debug.LogError("There was an error with text " + ore.PrefabName + " " + ex.Message);
							}
						}
					}
				}
			}
			page.Title = gase.DisplayName;
			page.Description = MoleHelper.GetMoleDescription(gase.Type);
			page.SpecificHeatText = StringManager.Get(gase.SpecificHeat().ToFloat()) + " J/K";
			if (gase.FreezingTemperature() > TemperatureKelvin.Zero)
			{
				page.FreezeTemperatureText = StringManager.Get(Math.Round(gase.FreezingTemperature().ToFloat(), 1)) + "K (" + StringManager.Get(Math.Round(RocketMath.KelvinToCelsius(gase.FreezingTemperature()), 1)) + "C)";
			}
			else
			{
				page.FreezeTemperatureText = "";
			}
			if (MoleHelper.CanEvaporate(gase.Type) || MoleHelper.CanCondense(gase.Type))
			{
				page.MinLiquidPressure = StringManager.Get(gase.MinLiquidPressure().ToFloat()) + "kPa at " + StringManager.Get(Math.Round(gase.FreezingTemperature().ToFloat(), 1)) + "K (" + StringManager.Get(Math.Round(RocketMath.KelvinToCelsius(gase.FreezingTemperature()), 1)) + "C)";
				page.BoilingTemperatureText = ((gase.BoilingPoint() < gase.FreezingTemperature()) ? GameStrings.NotApplicableString.DisplayString : (StringManager.Get(Math.Round(gase.BoilingPoint().ToFloat(), 1)) + "K (" + StringManager.Get(Math.Round(RocketMath.KelvinToCelsius(gase.BoilingPoint()), 1)) + "C) at 100kPa"));
				page.MaxLiquidTemperatureText = StringManager.Get(Math.Round(gase.MaxLiquidTemperature().ToFloat(), 1)) + "K (" + StringManager.Get(Math.Round(RocketMath.KelvinToCelsius(gase.MaxLiquidTemperature()), 1)) + "C) at " + StringManager.Get(gase.MinimumLiquidPressureAtMaxTemperature().ToFloat()) + "kPa";
				if (gase.LatentHeatOfVaporization() > 0.0)
				{
					page.LatentHeatText = StringManager.Get(gase.LatentHeatOfVaporization() / 1000.0) + " kJ/mol";
				}
				else
				{
					page.LatentHeatText = "";
				}
			}
			else
			{
				page.MinLiquidPressure = "";
				page.BoilingTemperatureText = "";
				page.MaxLiquidTemperatureText = "";
			}
			if (gase.MolarVolume() > VolumeLitres.Zero)
			{
				page.MolesPerLitreText = StringManager.Get(1f / gase.MolarVolume().ToFloat()) + " mols";
				page.MolesPerLitreInWorldText = StringManager.Get(1.0 / ((double)gase.MolarVolume().ToFloat() * MoleHelper.GetInWorldVolumeMultiplier(gase.Type))) + " mols";
			}
			else
			{
				page.MolesPerLitreText = GameStrings.NotApplicableString.DisplayString;
				page.MolesPerLitreInWorldText = GameStrings.NotApplicableString.DisplayString;
			}
			if (TryGetGasThumbnail(gase.Type, out var thumbnail))
			{
				page.CustomSpriteToUse = thumbnail;
			}
			page.GasType = gase.Type;
			AddCombustion(page.GasType, ref page);
			page.ParsePage();
			Register(page);
		}
	}

	public static bool TryGetGasThumbnail(Chemistry.GasType gasType, out Sprite thumbnail)
	{
		foreach (GasThumbnail gasThumbnail in Instance._gasThumbnails)
		{
			if (gasThumbnail.GasType == gasType)
			{
				thumbnail = gasThumbnail.Thumbnail;
				return true;
			}
		}
		thumbnail = null;
		return false;
	}

	public static bool GetGasThumbnail(string iconName, out Sprite thumbnail)
	{
		foreach (GasThumbnail gasThumbnail in Instance._gasThumbnails)
		{
			if (string.Equals(gasThumbnail.Thumbnail.name, iconName, StringComparison.InvariantCultureIgnoreCase))
			{
				thumbnail = gasThumbnail.Thumbnail;
				return true;
			}
		}
		thumbnail = null;
		return false;
	}

	private void PopulateLists()
	{
		DataHandler.ClearAll();
		foreach (SortingClass value in Enum.GetValues(typeof(SortingClass)))
		{
			GenerateList(value, DynamicThing.DynamicThingPrefabs);
		}
		GenerateLoreList();
		GenerateList("Devices", Device.AllDevicePrefabs);
		GenerateList("Ores", Ore.AllOrePrefabs);
		GenerateList("Ingots", Ingot.AllIngotPrefabs);
		GenerateList("Cartridges", Cartridge.AllCartridgePrefabs);
		GenerateList("Structures", Structure.AllStructurePrefabs);
		GenerateList("Gases", Chemistry.Gases);
		GenerateList("Reagents", Reagent.AllReagents);
		GenerateList("Genetic_Devices", IGenetics.AllGeneticsList);
		GenerateList("Trading_Devices", ITrading.AllTradingList);
		GenerateList("Genes", Enum.GetValues(typeof(Gene)).Cast<Gene>());
		GenerateList("Fabricators", FabricatorBase.AllFabricatorPrefabs);
		GenerateList("Airlock_Devices", AirlockControlBase.AllAirlockEnabledPrefabs);
		GenerateList("Logic_Units", LogicUnitBase.AllLogicPrefabs);
		GenerateList("Rocket", IRocketComponent.AllRocketPrefabs);
		GenerateList("Sanitation", ISanitation.AllSanitationPrefabs);
		GenerateList();
		foreach (SPDAHomePageButtonOverride homePageOverride in HomePageOverrides)
		{
			SPDAHomePageCategory sPDAHomePageCategory = UnityEngine.Object.Instantiate(Instance.HomePageButtonPrefab);
			sPDAHomePageCategory.SetUp(homePageOverride);
			sPDAHomePageCategory.transform.SetParent(Instance.HomePageContent, worldPositionStays: false);
		}
	}

	private void GenerateList(SortingClass slotType, List<DynamicThing> listOfThings)
	{
		foreach (DynamicThing listOfThing in listOfThings)
		{
			StationCategoryInsert stationCategoryInsert = new StationCategoryInsert();
			if (listOfThing.SortingClass == slotType && !listOfThing.HideInStationpedia)
			{
				stationCategoryInsert.PageLink = "Thing" + listOfThing.PrefabName;
				stationCategoryInsert.NameOfThing = listOfThing.DisplayName;
				stationCategoryInsert.PrefabHash = listOfThing.PrefabHash;
				stationCategoryInsert.InsertImage = listOfThing.Thumbnail;
				if (!AddElectronicsStationpedia(listOfThing, stationCategoryInsert) && !AddOrganicsStationpedia(listOfThing, stationCategoryInsert) && !AddAtmosphericsStationpedia(listOfThing, stationCategoryInsert))
				{
					DataHandler.AddNewListItem(slotType.ToString(), listOfThing.GetStationpediaCategory(), stationCategoryInsert);
				}
			}
		}
		DataHandler.AddToAllLists(slotType.ToString());
	}

	private void GenerateLoreList()
	{
		for (int i = 1; i < 14; i++)
		{
			StationCategoryInsert stationCategoryInsert = new StationCategoryInsert();
			LoreFactions loreFactions = (LoreFactions)i;
			stationCategoryInsert.NameOfThing = Localization.GetInterface(loreFactions.ToString());
			loreFactions = (LoreFactions)i;
			stationCategoryInsert.PageLink = loreFactions.ToString();
			stationCategoryInsert.InsertImage = LoreFactionThumbnails[i - 1];
			DataHandler.AddNewListItem(faction, faction, stationCategoryInsert);
		}
		DataHandler.AddToAllLists(faction);
	}

	private void GenerateList(string reference, List<Device> listOfThings)
	{
		foreach (Device listOfThing in listOfThings)
		{
			DataHandler.HiddenInPedia.TryGetValue(listOfThing.PrefabName, out var value);
			if (!(listOfThing.HideInStationpedia || value))
			{
				StationCategoryInsert stationCategoryInsert = new StationCategoryInsert();
				stationCategoryInsert.PageLink = "Thing" + listOfThing.PrefabName;
				stationCategoryInsert.NameOfThing = listOfThing.DisplayName;
				stationCategoryInsert.PrefabHash = listOfThing.PrefabHash;
				DataHandler.AddNewListItem(reference, listOfThing.GetStationpediaCategory(), stationCategoryInsert);
			}
		}
		DataHandler.AddToAllLists(reference);
	}

	private void GenerateList(string reference, List<ISanitation> listOfThings)
	{
		foreach (ISanitation listOfThing in listOfThings)
		{
			Thing thing = listOfThing as Thing;
			DataHandler.HiddenInPedia.TryGetValue(thing.PrefabName, out var value);
			if (!(thing.HideInStationpedia || value))
			{
				StationCategoryInsert stationCategoryInsert = new StationCategoryInsert();
				stationCategoryInsert.PageLink = "Thing" + thing.PrefabName;
				stationCategoryInsert.NameOfThing = thing.DisplayName;
				stationCategoryInsert.PrefabHash = thing.PrefabHash;
				DataHandler.AddNewListItem(reference, thing.GetStationpediaCategory(), stationCategoryInsert);
			}
		}
		DataHandler.AddToAllLists(reference);
	}

	private void GenerateList(string reference, List<IRocketComponent> listOfThings)
	{
		foreach (IRocketComponent listOfThing in listOfThings)
		{
			Thing thing = listOfThing as Thing;
			DataHandler.HiddenInPedia.TryGetValue(thing.PrefabName, out var value);
			if (!(thing.HideInStationpedia || value))
			{
				StationCategoryInsert stationCategoryInsert = new StationCategoryInsert();
				stationCategoryInsert.PageLink = "Thing" + thing.PrefabName;
				stationCategoryInsert.NameOfThing = thing.DisplayName;
				stationCategoryInsert.PrefabHash = thing.PrefabHash;
				DataHandler.AddNewListItem(reference, thing.GetStationpediaCategory(), stationCategoryInsert);
			}
		}
		DataHandler.AddToAllLists(reference);
	}

	private void GenerateList(string reference, List<IGenetics> listOfThings)
	{
		foreach (IGenetics listOfThing in listOfThings)
		{
			Thing thing = listOfThing as Thing;
			DataHandler.HiddenInPedia.TryGetValue(thing.PrefabName, out var value);
			if (!(thing.HideInStationpedia || value))
			{
				StationCategoryInsert dat = new StationCategoryInsert
				{
					PageLink = "Thing" + thing.PrefabName,
					NameOfThing = thing.DisplayName,
					PrefabHash = thing.PrefabHash
				};
				DataHandler.AddNewListItem(reference, Localization.GetInterface(StationpediaCategoryStrings.GeneticDevices), dat);
			}
		}
		DataHandler.AddToAllLists(reference);
	}

	private void GenerateList(string reference, List<ITrading> listOfThings)
	{
		foreach (ITrading listOfThing in listOfThings)
		{
			Thing thing = listOfThing as Thing;
			DataHandler.HiddenInPedia.TryGetValue(thing.PrefabName, out var value);
			if (!(thing.HideInStationpedia || value))
			{
				StationCategoryInsert dat = new StationCategoryInsert
				{
					PageLink = "Thing" + thing.PrefabName,
					NameOfThing = thing.DisplayName,
					PrefabHash = thing.PrefabHash
				};
				DataHandler.AddNewListItem(reference, Localization.GetInterface(StationpediaCategoryStrings.TradingDevices), dat);
			}
		}
		DataHandler.AddToAllLists(reference);
	}

	private void GenerateList(string reference, List<Cartridge> listOfThings)
	{
		foreach (Cartridge listOfThing in listOfThings)
		{
			DataHandler.HiddenInPedia.TryGetValue(listOfThing.PrefabName, out var value);
			if (!(listOfThing.HideInStationpedia || value))
			{
				StationCategoryInsert dat = new StationCategoryInsert
				{
					PageLink = "Thing" + listOfThing.PrefabName,
					NameOfThing = listOfThing.DisplayName,
					PrefabHash = listOfThing.PrefabHash
				};
				DataHandler.AddNewListItem(reference, listOfThing.GetStationpediaCategory(), dat);
			}
		}
		DataHandler.AddToAllLists(reference);
	}

	private void GenerateList(string reference, List<Structure> listOfThings)
	{
		foreach (Structure listOfThing in listOfThings)
		{
			DataHandler.HiddenInPedia.TryGetValue(listOfThing.PrefabName, out var value);
			if (listOfThing.HideInStationpedia || value)
			{
				continue;
			}
			StationCategoryInsert stationCategoryInsert = new StationCategoryInsert();
			stationCategoryInsert.PageLink = "Thing" + listOfThing.PrefabName;
			stationCategoryInsert.NameOfThing = listOfThing.DisplayName;
			stationCategoryInsert.PrefabHash = listOfThing.PrefabHash;
			stationCategoryInsert.InsertImage = listOfThing.Thumbnail;
			AddElectronicsStationpedia(listOfThing, stationCategoryInsert);
			AddOrganicsStationpedia(listOfThing, stationCategoryInsert);
			AddAtmosphericsStationpedia(listOfThing, stationCategoryInsert);
			AddFurnitureStationpedia(listOfThing, stationCategoryInsert);
			AddImportExportStationpedia(listOfThing, stationCategoryInsert);
			if (ShouldAddToStructurePage(listOfThing))
			{
				if (listOfThing is Chute)
				{
					DataHandler.AddNewListItem("ImportExport", listOfThing.GetStationpediaCategory(), stationCategoryInsert);
				}
				DataHandler.AddNewListItem(reference, listOfThing.GetStationpediaCategory(), stationCategoryInsert);
			}
		}
		DataHandler.AddToAllLists(reference);
	}

	private void GenerateList(string reference, List<Ore> listOfThings)
	{
		foreach (Ore listOfThing in listOfThings)
		{
			DataHandler.HiddenInPedia.TryGetValue(listOfThing.PrefabName, out var value);
			if (!(listOfThing.HideInStationpedia || value))
			{
				StationCategoryInsert stationCategoryInsert = new StationCategoryInsert();
				stationCategoryInsert.PageLink = "Thing" + listOfThing.PrefabName;
				stationCategoryInsert.NameOfThing = listOfThing.DisplayName;
				stationCategoryInsert.PrefabHash = listOfThing.PrefabHash;
				string category = ((listOfThing is PureIce) ? Localization.GetInterface("PureIceHeader") : ((!(listOfThing is Ice)) ? Localization.GetInterface("OreHeader") : Localization.GetInterface("FrozenOreHeader")));
				DataHandler.AddNewListItem(reference, category, stationCategoryInsert);
			}
		}
		DataHandler.AddToAllLists(reference);
	}

	public static string Trim(string input)
	{
		return Regex.Replace(input, "^[\\s]+|[\\n\\t]+", string.Empty);
	}

	private void GenerateList()
	{
		foreach (string key in DataHandler.ThingOverrideData.Keys)
		{
			foreach (SPDAThingOverideData item2 in DataHandler.ThingOverrideData[key])
			{
				if (item2.HideInSPDA)
				{
					continue;
				}
				Thing thing = Prefab.Find(item2.ThingName);
				StationCategoryInsert item = new StationCategoryInsert
				{
					NameOfThing = thing.DisplayName,
					PageLink = "Thing" + item2.ThingName,
					PrefabHash = thing.PrefabHash,
					InsertImage = thing.GetThumbnail()
				};
				if (item2.HideInSPDA)
				{
					continue;
				}
				if (DataHandler._listDictionary.ContainsKey(key))
				{
					if (DataHandler._listDictionary[key].ContainsKey(item2.CustomCategory))
					{
						DataHandler._listDictionary[key][item2.CustomCategory].Add(item);
						continue;
					}
					DataHandler._listDictionary[key][item2.CustomCategory] = new List<StationCategoryInsert> { item };
				}
				else
				{
					DataHandler._listDictionary[key] = new Dictionary<string, List<StationCategoryInsert>>();
					DataHandler._listDictionary[key][item2.CustomCategory] = new List<StationCategoryInsert> { item };
				}
			}
			DataHandler.AddToAllLists(key);
		}
	}

	private void GenerateList(string reference, List<Ingot> listOfThings)
	{
		DataHandler._listDictionary.Add(IngotType.Basic.GetName(), new Dictionary<string, List<StationCategoryInsert>>());
		DataHandler._listDictionary.Add(IngotType.Alloy.GetName(), new Dictionary<string, List<StationCategoryInsert>>());
		DataHandler._listDictionary.Add(IngotType.SuperAlloy.GetName(), new Dictionary<string, List<StationCategoryInsert>>());
		foreach (Ingot listOfThing in listOfThings)
		{
			DataHandler.HiddenInPedia.TryGetValue(listOfThing.PrefabName, out var value);
			if (!(listOfThing.HideInStationpedia || value))
			{
				StationCategoryInsert stationCategoryInsert = new StationCategoryInsert();
				stationCategoryInsert.PageLink = "Thing" + listOfThing.PrefabName;
				stationCategoryInsert.NameOfThing = listOfThing.DisplayName;
				stationCategoryInsert.PrefabHash = listOfThing.PrefabHash;
				DataHandler.AddNewListItem(reference, listOfThing.IngotType.GetName(), stationCategoryInsert);
			}
		}
		DataHandler.AddToAllLists(reference);
	}

	private void GenerateList(string reference, List<Mole> listOfThings)
	{
		foreach (Mole listOfThing in listOfThings)
		{
			StationCategoryInsert stationCategoryInsert = new StationCategoryInsert();
			stationCategoryInsert.PageLink = "Gas" + listOfThing.Type;
			stationCategoryInsert.NameOfThing = listOfThing.DisplayName;
			if (TryGetGasThumbnail(listOfThing.Type, out var thumbnail))
			{
				stationCategoryInsert.InsertImage = thumbnail;
			}
			DataHandler.AddNewListItem(reference, Localization.GetInterface("GasesHeader"), stationCategoryInsert);
		}
		DataHandler.AddToAllLists(reference);
	}

	private void GenerateList(string reference, List<Reagent> listOfThings)
	{
		foreach (Reagent listOfThing in listOfThings)
		{
			StationCategoryInsert stationCategoryInsert = new StationCategoryInsert();
			stationCategoryInsert.PageLink = "Reagent" + listOfThing.DisplayName;
			stationCategoryInsert.NameOfThing = listOfThing.DisplayName;
			stationCategoryInsert.InsertImage = ReagentImage;
			DataHandler.AddNewListItem(reference, Localization.GetInterface("ReagentsHeader"), stationCategoryInsert);
		}
		DataHandler.AddToAllLists(reference);
	}

	private void GenerateList(string reference, IEnumerable<Gene> listOfGenes)
	{
		foreach (Gene listOfGene in listOfGenes)
		{
			if (listOfGene != Gene.None)
			{
				string text = Enum.GetName(typeof(Gene), listOfGene);
				string nameOfThing = GeneHelper.DisplayName(listOfGene);
				StationCategoryInsert dat = new StationCategoryInsert
				{
					PageLink = "Gene" + text,
					NameOfThing = nameOfThing,
					InsertImage = GeneImage
				};
				DataHandler.AddNewListItem(reference, Localization.GetInterface("GeneticsHeader"), dat);
			}
		}
		DataHandler.AddToAllLists(reference);
	}

	public bool AddElectronicsStationpedia(Thing dynamicThing, StationCategoryInsert insert)
	{
		if (dynamicThing is Cable || dynamicThing is PowerGeneratorSlot || dynamicThing is WallLight || dynamicThing is ElectricalInputOutput || dynamicThing is BatteryCell || dynamicThing is TurbineGenerator || dynamicThing is PortableGenerator || dynamicThing is DynamicGenerator || dynamicThing is Appliance)
		{
			DataHandler.AddNewListItem(_electronicsPage, dynamicThing.GetStationpediaCategory(), insert);
			return true;
		}
		return false;
	}

	public bool AddOrganicsStationpedia(Thing dynamicThing, StationCategoryInsert insert)
	{
		if (dynamicThing is Harvester || dynamicThing is IGrower || dynamicThing is INutrition)
		{
			DataHandler.AddNewListItem(_organicsPage, dynamicThing.GetStationpediaCategory(), insert);
			return true;
		}
		return false;
	}

	public bool AddAtmosphericsStationpedia(Thing dynamicThing, StationCategoryInsert insert)
	{
		if (dynamicThing is DeviceAtmospherics || dynamicThing is GasSensor || dynamicThing is DevicePipeMounted || dynamicThing is Pipe || dynamicThing is WallHeater || dynamicThing is DynamicScrubber || dynamicThing is DynamicAirConditioner || dynamicThing is GasFilter || dynamicThing is Computer || dynamicThing is Diskette || dynamicThing is Circuit || dynamicThing is DynamicGasCanister || dynamicThing is GasTankStorage || dynamicThing is GasCanister)
		{
			DataHandler.AddNewListItem(_atmosphericsPage, dynamicThing.GetStationpediaCategory(), insert);
			return true;
		}
		return false;
	}

	public void AddImportExportStationpedia(Thing dynamicThing, StationCategoryInsert insert)
	{
		if (dynamicThing is DeviceImportExport || dynamicThing is Chute || dynamicThing is ChuteBin || dynamicThing is ChuteInlet)
		{
			DataHandler.AddNewListItem(_importExportPage, dynamicThing.GetStationpediaCategory(), insert);
		}
	}

	public void AddFurnitureStationpedia(Thing dynamicThing, StationCategoryInsert insert)
	{
		if (dynamicThing is Seat || dynamicThing is AtmosphericSeat || dynamicThing is Table || dynamicThing is Bench || dynamicThing is Locker || dynamicThing is CryoTube)
		{
			DataHandler.AddNewListItem(_furniturePage, dynamicThing.GetStationpediaCategory(), insert);
		}
	}

	public bool ShouldAddToStructurePage(Structure thing)
	{
		if (!thing.GetStationpediaCategoryKey().Equals("DoorCategory") && !thing.GetStationpediaCategoryKey().Equals("ChairTableCategory") && !thing.GetStationpediaCategoryKey().Equals("WallFloorCategory"))
		{
			return thing.GetStationpediaCategoryKey().Equals("SafetyCategory");
		}
		return true;
	}

	public new void OnEnable()
	{
		if (string.IsNullOrEmpty(CurrentPageKey))
		{
			ButtonHome();
			return;
		}
		SetPage(CurrentPageKey);
		if (XmlSaveLoad.IsReadyToPlayWorldAudio && !WorldManager.IsGamePaused)
		{
			UIAudioManager.Play(OpenHash);
		}
		if (IsMouseLocked)
		{
			SetMouseState(isMouseOn: true);
		}
	}

	public override void SetVisible(bool isVisble)
	{
		base.SetVisible(isVisble);
		if (isVisble)
		{
			MouseModeController.AddModal(Instance);
			SetPage(CurrentPageKey);
			if (!_isRendered)
			{
				_isRendered = true;
				ResetWindow();
			}
			Stationpedia.OnOpened?.Invoke();
			if (IsMouseLocked)
			{
				InventoryManager.Instance?.TooltipRef.ClearTooltip();
				ModalBackground.enabled = true;
			}
			else
			{
				ModalBackground.enabled = false;
			}
		}
		else
		{
			MouseModeController.RemoveModal(Instance);
			Stationpedia.OnClosed?.Invoke();
			KeyManager.RemoveInputState("Stationpedia");
			ModalBackground.enabled = false;
			WorldManager.OnPanelClose();
		}
		if (isVisble)
		{
			SelectSearchField().Forget();
		}
	}

	private async UniTaskVoid SelectSearchField()
	{
		int maxAttempts = 10;
		while (!SearchField.isFocused && IsVisible && maxAttempts > 0)
		{
			await UniTask.NextFrame();
			SearchField.Select();
			SearchField.ActivateInputField();
			maxAttempts--;
		}
	}

	public static void Cancel()
	{
		if (Instance.IsVisible)
		{
			Instance.SetVisible(isVisble: false);
			MouseModeController.RemoveModal(Instance);
		}
	}

	public static void Open()
	{
		if (!Instance.IsVisible)
		{
			Instance.SetVisible(isVisble: true);
			MouseModeController.AddModal(Instance);
		}
	}

	public static void Toggle()
	{
		Instance.SetVisible(!Instance.IsVisible);
		if (Instance.IsVisible)
		{
			MouseModeController.AddModal(Instance);
		}
		else
		{
			MouseModeController.RemoveModal(Instance);
		}
	}
}
