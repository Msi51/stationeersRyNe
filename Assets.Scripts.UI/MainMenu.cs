using System.Threading;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using CharacterCustomisation;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class MainMenu : Window
{
	private static MainMenu _instance;

	public GameObject PatchNotes;

	public Canvas MainMenuCanvas;

	public WorldDescription WorldInfo;

	public NewWorldStartCondition StartConditionInfo;

	[Header("Main Buttons")]
	[SerializeField]
	private Button _newGameButton;

	[SerializeField]
	private Button _loadButton;

	[SerializeField]
	private Button _tutorialButton;

	[SerializeField]
	private Button _joinServerButton;

	[Header("Small Button List")]
	[SerializeField]
	private Button _workshopButton;

	[SerializeField]
	private Button _scenarioButton;

	[SerializeField]
	private Button _settingsButton;

	[SerializeField]
	private Button _appearanceButton;

	[Header("Footer Buttons")]
	[SerializeField]
	private Button _exitButton;

	[SerializeField]
	private Button _changeLogButton;

	[Header("Page Manager")]
	[SerializeField]
	private MainMenuWindowManager _pageManager;

	private GameObject _mainMenuScene;

	private Settings _settings;

	public static MainMenu Instance
	{
		get
		{
			if (!_instance)
			{
				return _instance = Object.FindObjectOfType<MainMenu>();
			}
			return _instance;
		}
	}

	public override bool IsVisible => MainMenuCanvas.enabled;

	public MainMenuWindowManager PageManager => _pageManager;

	private void Awake()
	{
		_mainMenuScene = GameObject.Find("MainMenuScene");
		_settings = GameObject.Find("AlertCanvas").GetComponentInChildren<Settings>(includeInactive: true);
	}

	private void Start()
	{
		CharacterCustomisationManager.OnSceneLoaded += OnCharacterCustomisationSceneLoaded;
		CharacterCustomisationManager.OnSceneUnloaded += OnCharacterCustomisationSceneUnloaded;
		_pageManager.RegisterPage(_settings.GetComponent<MainMenuPage>());
		_newGameButton.onClick.AddListener(delegate
		{
			_pageManager.EnableMainMenuPage("NewGame");
		});
		_loadButton.onClick.AddListener(delegate
		{
			_pageManager.EnableMainMenuPage("LoadSave");
		});
		_tutorialButton.onClick.AddListener(delegate
		{
			_pageManager.EnableMainMenuPage("TutorialScenarios");
		});
		_joinServerButton.onClick.AddListener(delegate
		{
			_pageManager.EnableMainMenuPage("JoinServer");
		});
		_exitButton.onClick.AddListener(ExitGame);
		_workshopButton.onClick.AddListener(delegate
		{
			_pageManager.EnableMainMenuPage("WorkshopMods");
		});
		_scenarioButton.onClick.AddListener(delegate
		{
			_pageManager.EnableMainMenuPage("Scenarios");
		});
		_settingsButton.onClick.AddListener(delegate
		{
			_pageManager.EnableMainMenuPage("Settings");
		});
		_appearanceButton.onClick.AddListener(CharacterCustomisationManager.LoadScene);
	}

	public static async void StartGame()
	{
		string saveName = StartConditionMenu.Instance._worldNameInput.text;
		saveName = SaveHelper.SanitizeSaveName(saveName);
		if (string.IsNullOrEmpty(saveName))
		{
			PromptPanel.Instance.ShowPrompt(GameStrings.StartGameFailurePromptTitle, GameStrings.StartGameFailurePromptEmptyName, GameStrings.OkayConfirmation, delegate
			{
			});
			return;
		}
		if (StationSaveUtils.IsSaveExist(saveName))
		{
			if (!SaveHelper.GetUniqueDirectoryName(StationSaveUtils.GetSavePathSavesSubDir(), saveName, out var uniqueDirectoryName))
			{
				ConsoleWindow.PrintError("Could not generate unique save name");
				return;
			}
			saveName = uniqueDirectoryName;
		}
		XmlSaveLoad.ClearAll();
		NewWorldMenu.SelectedWorld.PlanetScene.SetVisible(isVisble: false);
		WorldSetting worldSetting = NewWorldMenu.SelectedWorld.WorldSetting;
		string id = worldSetting.Id;
		StartLocationData startLocationData = null;
		if (NewWorldMenu.SelectedStartLocation != null)
		{
			startLocationData = NewWorldMenu.SelectedStartLocation.Data;
		}
		WorldSetting.SetCurrent(worldSetting, StartConditionMenu.SelectedStartCondition, startLocationData);
		DifficultySetting.SetCurrent(WorldConfigurationMenu.SelectedDifficulty);
		await World.StartNewWorld(id);
		await IsSaveReady();
		if (GameManager.GameState != GameState.None)
		{
			PointOfInterestManager.DiscoverPoiAtStartLocationWithNoMessage();
			SaveResult saveResult = await SaveHelper.NewSave(saveName, default(CancellationToken));
			if (saveResult.Success)
			{
				XmlSaveLoad.Instance.CurrentStationName = saveName;
			}
			else
			{
				ConsoleWindow.PrintError(saveResult.Message, suppressStacktrace: true);
			}
		}
	}

	private static async UniTask IsSaveReady()
	{
		while (GameManager.GameState == GameState.None && (GameManager.GameTickCount <= 5 || InventoryManager.Parent?.ParentSlot != null))
		{
			await UniTask.WaitForEndOfFrame();
		}
	}

	public override void OnEnable()
	{
		base.OnEnable();
		if ((bool)_mainMenuScene)
		{
			MainMenuSceneEnabled(enabled: true);
		}
	}

	public override void OnDisable()
	{
		base.OnDisable();
		if ((bool)_mainMenuScene)
		{
			MainMenuSceneEnabled(enabled: false);
		}
	}

	public override void SetVisible(bool isVisble)
	{
		SetActive(isVisble);
	}

	public override void SetActive(bool active)
	{
		if (active != IsVisible)
		{
			MainMenuCanvas.enabled = active;
			InvokeOnVisibilityChanged(active);
			if (active)
			{
				CameraController.Instance.SetCullingMask(isThirdPerson: true);
			}
			if (active)
			{
				CharacterCustomisationManager.OnSceneLoaded += OnCharacterCustomisationSceneLoaded;
				CharacterCustomisationManager.OnSceneUnloaded += OnCharacterCustomisationSceneUnloaded;
			}
			else
			{
				CharacterCustomisationManager.OnSceneLoaded -= OnCharacterCustomisationSceneLoaded;
				CharacterCustomisationManager.OnSceneUnloaded -= OnCharacterCustomisationSceneUnloaded;
			}
		}
	}

	public void TogglePatchNotesView()
	{
		PatchNotes.SetActive(!PatchNotes.activeInHierarchy);
	}

	private void OnCharacterCustomisationSceneLoaded()
	{
		MainMenuSceneEnabled(enabled: false);
		_pageManager.DisableAllPages();
	}

	private void OnCharacterCustomisationSceneUnloaded()
	{
		MainMenuSceneEnabled(enabled: true);
		_pageManager.EnableMainMenuPage("MainMenu");
	}

	private void MainMenuSceneEnabled(bool enabled)
	{
		if (GameManager.IsBatchMode)
		{
			enabled = false;
		}
		_mainMenuScene.SetActive(enabled);
	}

	public void CurrentPageChanged(MainMenuPage page)
	{
		if (page.MenuSceneShouldBeVisible != _mainMenuScene.activeInHierarchy)
		{
			MainMenuSceneEnabled(page.MenuSceneShouldBeVisible);
		}
	}

	private void ExitGame()
	{
		GameManager.QuitPrompt();
	}
}
