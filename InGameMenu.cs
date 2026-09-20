using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using CharacterCustomisation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameMenu : Window, IModal
{
	[Header("Page Manager")]
	[SerializeField]
	private MainMenuWindowManager _pageManager;

	[Header("Buttons")]
	[SerializeField]
	private Button _saveButton;

	[SerializeField]
	private Button _saveAsButton;

	[SerializeField]
	private Button _changeAppearanceButton;

	[SerializeField]
	private TextMeshProUGUI _saveButtonText;

	[SerializeField]
	private TextMeshProUGUI _saveAsButtonText;

	public static InGameMenu Instance;

	public bool UnlockCursor => true;

	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
		_saveButton.onClick.AddListener(SaveButtonPressed);
		_saveAsButton.onClick.AddListener(SaveAsButtonPressed);
		_changeAppearanceButton.onClick.AddListener(CharacterCustomisationManager.LoadScene);
	}

	private static void SaveButtonPressed()
	{
		XmlSaveLoad.Instance.ButtonSave();
		InventoryManager.SetMenuState(showMenu: false);
	}

	private static void SaveAsButtonPressed()
	{
		XmlSaveLoad.Instance.ShowSavePanel();
	}

	public override void OnEnable()
	{
		base.OnEnable();
		CharacterCustomisationManager.OnSceneLoaded += CharacterCustomisationManagerOnSceneLoaded;
		HandleButtons();
		MouseModeController.AddModal(this);
	}

	private void HandleButtons()
	{
		bool active = !GameManager.IsNewTutorial && !NetworkManager.IsClient;
		_saveButton.gameObject.SetActive(active);
		_saveAsButton.gameObject.SetActive(active);
		_changeAppearanceButton.gameObject.SetActive(!GameManager.IsNewTutorial);
		_saveAsButtonText.text = GameStrings.SaveAsButtonGameString.DisplayString;
	}

	public override void OnDisable()
	{
		base.OnDisable();
		CharacterCustomisationManager.OnSceneLoaded -= CharacterCustomisationManagerOnSceneLoaded;
		MouseModeController.RemoveModal(this);
	}

	private void CharacterCustomisationManagerOnSceneLoaded()
	{
		_pageManager.DisableAllPages();
	}
}
