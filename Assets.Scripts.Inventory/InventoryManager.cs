using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.FirstPerson;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Assets.Scripts.Vehicles;
using CharacterCustomisation;
using Cysharp.Threading.Tasks;
using Networking.Servers;
using Objects.Items;
using Objects.Pipes;
using Objects.Rockets;
using Objects.Rockets.UI;
using Sound;
using Steamworks;
using TerrainSystem;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Assets.Scripts.Inventory;

public class InventoryManager : ManagerBase
{
	public enum Mode
	{
		Normal,
		Placement,
		PrecisionPlacement
	}

	public delegate void Event();

	[Serializable]
	public class HelperArrow
	{
		[ReadOnly]
		public string DisplayName;

		public GameObject Prefab;

		public ConnectionRole ConnectionRole;

		public NetworkType ConnectionType;
	}

	private enum RichPresenceState
	{
		None,
		Disembodied,
		DaysInWorld,
		InRocket
	}

	public struct OriginalSlot
	{
		public Slot ThingOriginalSlot;

		public long ReferenceId;
	}

	public delegate void DelegateEvent();

	private readonly StringBuilder _gasContents = new StringBuilder();

	public static bool EnablePlayerKeys = true;

	[Header("System Variables")]
	[ReadOnly]
	public static Brain ParentBrain;

	private static Entity _parent;

	private Animator _parentAnimator;

	private MovementController _movementController;

	[ReadOnly]
	public static Mode CurrentMode;

	[ReadOnly]
	public SlotDisplay ActiveHand;

	[ReadOnly]
	public SlotDisplay InactiveHand;

	public static Event PrecisionPlaceEvent;

	[Header("Construction Variables")]
	[ReadOnly]
	public static Structure ConstructionCursor;

	[ReadOnly]
	public static int CurrentFace = RocketGrid.FaceInt.South;

	[ReadOnly]
	public static Quaternion CurrentRotation = Quaternion.identity;

	[Header("UI Defines")]
	[Tooltip("Panel that is used to display constructable things when using a multi-constructor.")]
	public ConstructionPanel ConstructionPanel;

	public List<SlotDisplay> DisplaySlots;

	[Header("Mirror slot display and button")]
	public SlotDisplayMirror SuitBackSlotDisplay;

	public SlotDisplayButtonMirror SuitBackSlotDisplayButton;

	[Space(15f)]
	public ScoreBoard ScreenScoreBoard;

	[Tooltip("Panel that is used to display Hand slot information")]
	public GameObject PanelHandsGameObject;

	[Tooltip("Panel that is used to display tooltip information")]
	public Tooltip TooltipRef;

	[Tooltip("Panel that is used to display progress during an action")]
	[SerializeField]
	private UIProgressionBar _uiProgressBarPanel;

	[Tooltip("Panel used to enact inventory clothing animations")]
	public ClothingPanel ClothingPanel;

	[Tooltip("Panel that is used to display Status")]
	public GameObject StatusPanel;

	[Tooltip("GameObject that represents the UI Cursor")]
	public GameObject UiCursor;

	[Tooltip("Panel used for displaying book information")]
	public GameObject ReadingPanel;

	private static long _lastActionedID;

	[Header("UI Debug")]
	public TextMeshProUGUI DebugContents;

	public TextMeshProUGUI DebugSpawnPrefab;

	[Header("UI Canvas Defines")]
	public GameObject SystemCanvas;

	public Assets.Scripts.UI.MainMenu MenuCanvas;

	public GameObject GameMenuPanel;

	public GameObject MainMenuPanel;

	public Button GameMenuSaveButton;

	public GameObject GameMenuClearPlayersButton;

	public GameObject GameMenuRespawnButton;

	public Canvas GameCanvas;

	public GameObject PanelGameInfo;

	public GameObject PanelTabInstructions;

	public GameObject PauseButton;

	public GameObject SettingsPanel;

	public GameObject IncidentsButton;

	public HotkeyDisplay LeftHandHotkey;

	public HotkeyDisplay RightHandHotkey;

	public static bool ShowUi = true;

	public static bool ShowMenu = true;

	[Header("Dropdown Panel UI")]
	public DropDownPanel DropdownPanel;

	public TextMeshProUGUI DropdownPanelTitle;

	public TMP_Dropdown DropdownPanelDropdown;

	public TMP_FontAsset DefualtFont;

	public static InputPanelState DropdownState = InputPanelState.None;

	[Header("UI variables")]
	private float _cursorRotate;

	private float _cursorRotateVertical;

	[Range(0f, 1f)]
	[Tooltip("The alpha of the cursor selection for an interactable")]
	public float CursorAlphaInteractable = 0.1f;

	[Range(0f, 1f)]
	[Tooltip("The alpha of the cursor selection mesh during construction")]
	public float CursorAlphaConstructionMesh = 0.3f;

	[Range(0f, 1f)]
	[Tooltip("The alpha of the cursor helper (arrows)")]
	public float CursorAlphaConstructionHelper = 0.3f;

	[Range(0f, 1f)]
	[Tooltip("The alpha of the cursor grid (grid highlight)")]
	public float CursorAlphaConstructionGrid = 0.05f;

	[Range(0f, 1f)]
	[Tooltip("The alpha of the cursor grid (grid highlight)")]
	public float CursorAlphaLine = 0.3f;

	public static Vector3 WorldPosition;

	private static readonly Dictionary<string, Structure> _constructionCursors = new Dictionary<string, Structure>();

	private static readonly Dictionary<string, GameObject> _dynamicThingCursors = new Dictionary<string, GameObject>();

	public static readonly List<string> DynamicThingPrefabs = new List<string>();

	public static ICreativeSpawnable SpawnPrefab;

	private int _lastHorizontalFace = RocketGrid.FaceInt.South;

	private Vector3 _usePrimaryPosition;

	private Quaternion _usePrimaryRotation;

	private static float _minThrowForce = 1f;

	private float _dropTime = 0.5f;

	private float _currentDropTime;

	private bool _canEquip;

	[ReadOnly]
	public static GameObject PrecisionPlaceCursor;

	public float newScrollData;

	private static readonly int InventoryPanelOpenHash = Animator.StringToHash("SFX_UI_InventoryPanelOpen");

	private static readonly int InventoryPanelCloseHash = Animator.StringToHash("SFX_UI_InventoryPanelClose");

	public InventoryWindowManager InventoryWindowManager;

	public static InventoryManager Instance;

	public static Event OnInitialize;

	public static Slot LeftHandSlot;

	public static Slot RightHandSlot;

	private Slot BeltSlot;

	public List<HelperArrow> HelperIndicators = new List<HelperArrow>();

	public static string HelperTag = "BlueprintHelper";

	private GameObject _helperSmallPlus;

	private GameObject _constructionCursorParent;

	private GameObject _dynamicCursorParent;

	private static readonly Action<string, string> _chatMessage = OnChatMessage;

	private static readonly UnityAction<string> _onTypeChatMessage = OnTypeChatMessage;

	private static readonly TemperatureKelvin HeatHazeThreshold = new TemperatureKelvin(Chemistry.Temperature.ZeroDegrees.ToDouble() + 45.0);

	private static bool _isUnconscious;

	private RichPresenceState _richPresenceState;

	private const float SHAKE_CAMERA_FORCE = 6000f;

	private static readonly int UiSwapActiveHandHash = Animator.StringToHash("UI_SwapActiveHand");

	private static readonly int HideCursorHash = Animator.StringToHash("HideCursor");

	private static OriginalSlot[] _originalSlots = new OriginalSlot[2]
	{
		new OriginalSlot
		{
			ReferenceId = 0L,
			ThingOriginalSlot = null
		},
		new OriginalSlot
		{
			ReferenceId = 0L,
			ThingOriginalSlot = null
		}
	};

	private static List<InteractableType> _excludeHandSlots = new List<InteractableType>
	{
		InteractableType.Slot1,
		InteractableType.Slot2
	};

	private static float _animationHighlightDelay = 0.5f;

	private Item _activeSmartTool;

	private Thing _smartToolTargetThing;

	private Slot _activeSmartToolOriginalSlot;

	private bool _didSmartToolSwapHands;

	private bool _canDoHandlePrimaryUse = true;

	public bool IsRefreshingAllChunkClutter;

	private bool _primaryAvailable = true;

	private bool _isMineVoxelTaskRunning;

	private static readonly int RotateBlueprintHash = Animator.StringToHash("RotateBlueprint");

	private float _placementZoom;

	private Vector3 _cursorPosition;

	private Vector3 _worldGrid;

	private Vector3 _worldMasterGrid;

	private bool _usingAutoplace;

	private PassiveTooltip tooltip;

	public Coroutine ActionCoroutine;

	public DelegateEvent OnComplete;

	public float LastCompletedRatio;

	public PooledAudioSource ActionSoundAudio;

	public PooledAudioSource ActionUISoundAudio;

	private readonly int _uiActionStartHash = Animator.StringToHash("UI_ActionStart");

	private readonly int _uiActionActiveHash = Animator.StringToHash("UI_ActionActive");

	private readonly int _uiActionFinishedHash = Animator.StringToHash("UI_ActionFinished");

	private static readonly int Offset = Shader.PropertyToID("_Offset");

	private static readonly int HasItem = Animator.StringToHash("Has_Item");

	public static Entity Parent
	{
		get
		{
			return _parent;
		}
		set
		{
			_parent = value;
			if (Parent == null)
			{
				ParentHuman = null;
				return;
			}
			ParentHuman = _parent as Human;
			if ((object)_parent != null)
			{
				OcclusionManager.CheckAllOcclusion();
				Plant.RefreshAll();
			}
		}
	}

	public static Vector3 ParentPosition
	{
		get
		{
			if (!Parent)
			{
				return Vector3.zero;
			}
			return Parent.Position;
		}
	}

	public static Human ParentHuman { get; private set; }

	public static Slot ActiveHandSlot => Instance.ActiveHand?.Slot;

	public UIProgressionBar UIProgressionBar => _uiProgressBarPanel;

	public static bool AllowMouseControl
	{
		get
		{
			if (!Cursor.visible && !CharacterCustomisationManager.IsSceneLoaded && !ConsoleWindow.IsOpen)
			{
				return !CameraController.CinematicMode;
			}
			return false;
		}
	}

	public Transform DynamicCursorParentTransform => _dynamicCursorParent.transform;

	public bool InGameMenuOpen => GameMenuPanel.activeInHierarchy;

	public bool IsUsingSmartTool => _activeSmartTool != null;

	public static bool IsAuthoringMode => Instance.ActiveHand.Slot.Occupant is AuthoringTool;

	public static event Event OnActiveHandChanged;

	public static void ClearAll()
	{
		Parent = null;
		InventoryManager.OnActiveHandChanged = null;
	}

	public IEnumerator SetUIPanelVisibility(bool panelHands, bool panelClothing, bool statusPanel, float delay = 0f)
	{
		yield return Yielders.WaitForSeconds(delay);
		PanelHandsGameObject.SetActive(panelHands);
		if (!ClothingPanel.gameObject.activeSelf && panelClothing)
		{
			UIAudioManager.Play(UIAudioManager.EnableHudLeftHash);
		}
		ClothingPanel.gameObject.SetActive(panelClothing);
		if (!StatusPanel.gameObject.activeSelf && statusPanel)
		{
			UIAudioManager.Play(UIAudioManager.EnableHudRightHash);
		}
		StatusPanel.SetActive(statusPanel);
	}

	public static void SetMenuState(bool showMenu, bool playSound = true)
	{
		if (GameManager.IsBatchMode || !ShowUi)
		{
			return;
		}
		if (UIAudioManager.Instance != null && playSound && InventoryWindowManager._inventoryFinishedLoading && GameManager.GameState == GameState.Running)
		{
			UIAudioManager.Play(showMenu ? InventoryPanelOpenHash : InventoryPanelCloseHash);
		}
		if (Instance.PauseButton != null)
		{
			Instance.PauseButton.SetActive(!NetworkManager.IsClient);
		}
		if (Instance.IncidentsButton != null)
		{
			Instance.IncidentsButton.SetActive(!NetworkManager.IsClient);
		}
		ShowMenu = showMenu;
		if (ShowMenu)
		{
			if (GameManager.GameState == GameState.Running)
			{
				if ((bool)Instance.GameMenuPanel)
				{
					Instance.GameMenuPanel.SetActive(value: true);
					if (!NetworkManager.IsClient && NetworkBase.Clients.Count == 0)
					{
						if (Instance.PauseButton != null)
						{
							Instance.PauseButton.SetActive(value: false);
						}
						WorldManager.SetGamePause(pauseGame: true);
					}
					PlayerCookie.Current.SetWorldPrefsInterfaceData(InventoryWindowManager.Instance.GenerateUISaveData());
					PlayerCookie.Current.SetDiscoveredPois(PointOfInterestManager.GetSaveData());
					PlayerCookie.Current.Save();
				}
				if ((bool)Instance.GameMenuSaveButton)
				{
					Instance.GameMenuSaveButton.interactable = !NetworkManager.IsClient && !GameManager.IsTutorial;
				}
				if ((bool)Instance.GameMenuClearPlayersButton)
				{
					Instance.GameMenuClearPlayersButton.SetActive(!NetworkManager.IsClient);
				}
				if ((bool)Instance.GameMenuRespawnButton)
				{
					Instance.GameMenuRespawnButton.SetActive(!GameManager.IsTutorial);
				}
			}
			else if ((bool)Instance.MenuCanvas)
			{
				Instance.MenuCanvas.SetVisible(isVisble: true);
			}
			if ((bool)Instance.GameCanvas)
			{
				Instance.GameCanvas.enabled = false;
			}
			return;
		}
		if (!GameManager.IsBatchMode && (bool)Instance.GameCanvas)
		{
			Instance.GameCanvas.enabled = Parent;
		}
		if ((bool)Instance.MenuCanvas)
		{
			Instance.MenuCanvas.SetVisible(isVisble: false);
		}
		if ((bool)Instance.GameMenuPanel)
		{
			Instance.GameMenuPanel.SetActive(value: false);
			if (!NetworkBase.IsPaused)
			{
				WorldManager.SetGamePause(pauseGame: false);
			}
		}
		CursorManager.Instance.OnApplicationFocus(focus: true);
		AnimateActiveHands();
	}

	public static bool IsDisabled()
	{
		if (!ShowMenu && !ImguiCreativeSpawnMenu.Show)
		{
			return ConsoleWindow.IsOpen;
		}
		return true;
	}

	public static void SetDynamicInvState(bool show)
	{
		if (!GameManager.IsBatchMode && ShowUi)
		{
			ImguiCreativeSpawnMenu.ShowMenu(show && WorldManager.Instance.GameMode == GameMode.Creative && !GameManager.IsTutorial);
		}
	}

	public static void SetGameInfoState(bool enabled, bool hideUI = true)
	{
		if (hideUI)
		{
			ShowUi = enabled;
		}
		Instance.PanelGameInfo.SetActive(hideUI ? (!ShowUi) : (!enabled));
		Instance.PanelTabInstructions.SetActive(hideUI ? (!ShowUi) : (!enabled));
	}

	public static void SetUiState(bool showUi)
	{
		ShowUi = showUi;
		if (!showUi)
		{
			Instance.GameCanvas.enabled = false;
			Instance.MenuCanvas.SetVisible(isVisble: false);
			Instance.SystemCanvas.SetActive(value: false);
			ImguiCreativeSpawnMenu.Show = false;
		}
		else
		{
			Instance.SystemCanvas.SetActive(value: true);
			SetMenuState(ShowMenu, playSound: false);
			ImguiCreativeSpawnMenu.ShowMenu(ImguiCreativeSpawnMenu.Show);
		}
	}

	public void ToggleScoreboard(bool forceHide = false)
	{
		if (!ScreenScoreBoard || InputWindowBase.IsInputWindow)
		{
			return;
		}
		if (DropdownState == InputPanelState.Waiting)
		{
			ButtonDropdownCancel();
		}
		if (InputSourceCode.Instance.IsVisible)
		{
			InputSourceCode.Instance.SetVisible(isVisble: false);
		}
		bool flag = !ScreenScoreBoard.GameObject.activeSelf && !forceHide;
		ScreenScoreBoard.SetActive(flag);
		if (!flag)
		{
			MouseModeController.RemoveModal(ScreenScoreBoard);
			if ((bool)PanelProfile.Instance)
			{
				PanelProfile.Instance.ClosePlayerProfile();
			}
			if ((bool)PanelServerInfo.Instance)
			{
				PanelServerInfo.Instance.OnCloseServerInfo();
			}
			BlockedPlayerManager.Instance.HidePopup();
			CursorManager.Instance.OnApplicationFocus(focus: true);
		}
		else
		{
			MouseModeController.AddModal(ScreenScoreBoard);
		}
	}

	public void ButtonDropdownSubmit()
	{
		DropdownState = InputPanelState.Submitted;
		Instance.DropdownPanel.SetActive(active: false);
		MouseModeController.RemoveModal(DropdownPanel);
	}

	public void ButtonDropdownCancel()
	{
		DropdownState = InputPanelState.Cancelled;
		Instance.DropdownPanel.SetActive(active: false);
		MouseModeController.RemoveModal(DropdownPanel);
	}

	public void ButtonGameMenuCancel()
	{
		SetMenuState(showMenu: false);
	}

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		if (Instance == null)
		{
			Instance = this;
		}
		CharacterCustomisationManager.OnSceneLoaded += OnCharacterCustomisationSceneLoaded;
		CharacterCustomisationManager.OnSceneUnloaded += OnCharacterCustomisationSceneUnloaded;
		Slot.PopulateSlotTypeSprites();
		if (!GameManager.IsBatchMode)
		{
			InventoryWindowManager.Initialize();
		}
		DynamicThingPrefabs.Clear();
		SetMenuState(showMenu: true);
		if (OnInitialize != null)
		{
			OnInitialize();
			OnInitialize = null;
		}
	}

	private void OnDestroy()
	{
		CharacterCustomisationManager.OnSceneLoaded -= OnCharacterCustomisationSceneLoaded;
		CharacterCustomisationManager.OnSceneUnloaded -= OnCharacterCustomisationSceneUnloaded;
	}

	private void OnCharacterCustomisationSceneLoaded()
	{
		GameMenuPanel.SetActive(value: false);
		ClothingPanel.gameObject.SetActive(value: false);
		PanelHandsGameObject.SetActive(value: false);
		ShowMenu = false;
		MouseModeController.InCharacterCustomisation = true;
	}

	private void OnCharacterCustomisationSceneUnloaded()
	{
		ClothingPanel.gameObject.SetActive(value: true);
		PanelHandsGameObject.SetActive(value: true);
		StartCoroutine(HighlightSlot(DisplaySlots.IndexOf(ActiveHand)));
		UiCursor.SetActive(value: true);
		SetMenuState(showMenu: true);
		MouseModeController.InCharacterCustomisation = false;
	}

	public void Initialize()
	{
		SetupConstructionCursors();
		if (!GameManager.IsBatchMode)
		{
			TooltipRef.ClearTooltip();
			TooltipRef.DrawTooltip();
		}
		_uiProgressBarPanel.SetActive(active: false);
	}

	private void HandleStructurePrefab(Thing prefab)
	{
		Structure structure = prefab as Structure;
		if (structure == null)
		{
			return;
		}
		structure.IsCursor = true;
		Structure.IsCursorCreating = true;
		Structure structure2 = UnityEngine.Object.Instantiate(structure, Vector3.zero, Quaternion.identity);
		structure2.CachePrefabBounds();
		structure.IsCursor = false;
		Structure.IsCursorCreating = false;
		structure2.tag = "Cursor";
		structure2.IgnoreSave = true;
		structure2.IsCursor = true;
		structure2.name = structure.name + "_cursor";
		structure2.transform.parent = _constructionCursorParent.transform;
		if ((bool)structure2.BaseAnimator)
		{
			UnityEngine.Object.Destroy(structure2.BaseAnimator);
		}
		RocketEngineEffect[] componentsInChildren = structure2.GetComponentsInChildren<RocketEngineEffect>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren[i]);
		}
		Light[] componentsInChildren2 = structure2.GetComponentsInChildren<Light>();
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren2[i]);
		}
		Collider[] componentsInChildren3 = structure2.GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren3.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren3[i]);
		}
		TextMesh[] componentsInChildren4 = structure2.GetComponentsInChildren<TextMesh>();
		for (int i = 0; i < componentsInChildren4.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren4[i]);
		}
		RectTransform[] componentsInChildren5 = structure2.GetComponentsInChildren<RectTransform>();
		for (int i = 0; i < componentsInChildren5.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren5[i].gameObject);
		}
		SmallGrid component = structure2.GetComponent<SmallGrid>();
		if ((bool)component)
		{
			foreach (Connection openEnd in component.OpenEnds)
			{
				GameObject original = HelperIndicators[0].Prefab;
				Quaternion localRotation = openEnd.Transform.localRotation;
				int num = HelperIndicators.FindIndex((HelperArrow p) => p.ConnectionRole == openEnd.ConnectionRole && p.ConnectionType == openEnd.ConnectionType);
				if (num >= 0)
				{
					original = HelperIndicators[num].Prefab;
				}
				if ((bool)(component as Cable) && openEnd.ConnectionRole == ConnectionRole.None)
				{
					original = _helperSmallPlus;
				}
				GameObject gameObject = UnityEngine.Object.Instantiate(original, openEnd.Transform.position.GridCenter(SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset), localRotation);
				gameObject.transform.localScale = Vector3.one;
				gameObject.transform.parent = structure2.transform;
				openEnd.HelperRenderer = gameObject.GetComponent<Renderer>();
				gameObject.tag = HelperTag;
			}
		}
		_constructionCursors.Add(structure.name, structure2);
		structure2.gameObject.SetActive(value: false);
		if ((bool)structure2.Blueprint)
		{
			int count = structure2.Renderers.Count;
			while (count-- > 0)
			{
				structure2.Renderers[count].Destroy();
			}
			structure2.Renderers = new List<ThingRenderer>();
			GameObject gameObject2 = UnityEngine.Object.Instantiate(structure2.Blueprint);
			gameObject2.transform.parent = structure2.transform;
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
			structure2.Wireframe = gameObject2.GetComponent<Wireframe>();
			if ((bool)structure2.Wireframe)
			{
				structure2.Wireframe.OrientationArrowOffset = structure2.BlueprintOrientVector;
			}
			return;
		}
		foreach (ThingRenderer renderer in structure2.Renderers)
		{
			if (!renderer.Enabled)
			{
				renderer.Destroy();
				continue;
			}
			Material[] materials = renderer.Materials;
			for (int num2 = 0; num2 < materials.Length; num2++)
			{
				Material material = new Material(CursorManager.Instance.CursorShader);
				material.color = Color.white;
				material.SetFloat(Offset, -300f);
				material.mainTexture = null;
				materials[num2] = material;
			}
			renderer.Materials = materials;
			renderer.SetShadowCastMode(ShadowCastingMode.Off);
		}
	}

	private void HandleDynamicThingPrefab(Thing prefab)
	{
		DynamicThing dynamicThing = prefab as DynamicThing;
		if (dynamicThing == null)
		{
			return;
		}
		if (!dynamicThing.CompareTag("NotSpawnable"))
		{
			DynamicThingPrefabs.Add(dynamicThing.name);
			ImguiCreativeSpawnMenu.AddDynamicItem(dynamicThing);
		}
		if (!(dynamicThing == null) && !(dynamicThing as Entity) && !(dynamicThing as Monster) && !(dynamicThing as WheeledBase) && !(dynamicThing.Blueprint == null))
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(dynamicThing.Blueprint);
			if (!(gameObject == null))
			{
				gameObject.name += "_cursor";
				gameObject.tag = "Cursor";
				gameObject.transform.parent = _dynamicCursorParent.transform;
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.localRotation = Quaternion.identity;
				Color green = Color.green;
				green.a = 0.2f;
				gameObject.GetComponent<MeshRenderer>().sharedMaterial.color = green;
				gameObject.gameObject.SetActive(value: false);
				_dynamicThingCursors.Add(dynamicThing.name, gameObject.gameObject);
			}
		}
	}

	public void AddDynamicThingCursor(string prefabName, GameObject blueprint)
	{
		_dynamicThingCursors.Add(prefabName, blueprint);
	}

	private void SetupConstructionCursors()
	{
		if (_constructionCursors.Count > 0)
		{
			return;
		}
		_constructionCursors.Clear();
		_dynamicThingCursors.Clear();
		_helperSmallPlus = Resources.Load<GameObject>("UI/HelperSmallPlus");
		_constructionCursorParent = new GameObject("~ConstructionCursors");
		_dynamicCursorParent = new GameObject("~DynamicThingCursors");
		foreach (Thing sourcePrefab in WorldManager.Instance.SourcePrefabs)
		{
			if ((object)sourcePrefab != null && !sourcePrefab.IsCustomThing)
			{
				HandleStructurePrefab(sourcePrefab);
				HandleDynamicThingPrefab(sourcePrefab);
			}
		}
	}

	public static void RefreshDisplaySlotBindings()
	{
		if (!(Instance == null))
		{
			Instance.LeftHandHotkey.Assignment = ((Instance.ActiveHand.Slot == LeftHandSlot) ? "ActiveHandSlot" : "SwapHands");
			Instance.RightHandHotkey.Assignment = ((Instance.ActiveHand.Slot == RightHandSlot) ? "ActiveHandSlot" : "SwapHands");
			Instance.LeftHandHotkey.Refresh();
			Instance.RightHandHotkey.Refresh();
		}
	}

	public void Initialize(Entity parent)
	{
		Parent = parent;
		if (CameraController.IsThirdPerson)
		{
			CameraController.Instance.ThirdController.ResetAll();
		}
		_movementController = parent.GetComponent<MovementController>();
		StatusUpdates.MovementController = _movementController;
		_parentAnimator = parent.GetComponent<Animator>();
		foreach (SlotDisplay displaySlot in DisplaySlots)
		{
			if ((bool)displaySlot.HotkeyGrid)
			{
				displaySlot.HotkeyGrid.HideAll();
			}
			if (!displaySlot.SlotDisplayButton.IsAvailableForSpecies(parent.AsHuman.SpeciesClass))
			{
				displaySlot.SlotDisplayButton.SetVisible(isVisble: false);
				continue;
			}
			displaySlot.LinkToSlot(parent);
			displaySlot.Slot.RefreshSlotDisplay();
		}
		SuitBackSlotDisplayButton.SlotDisplay = SuitBackSlotDisplay;
		if (DisplaySlots.Count > 0)
		{
			ActiveHand = DisplaySlots[0];
			InactiveHand = DisplaySlots[1];
		}
		if (CameraController.Instance.PortraitCamera == null)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(CameraController.Instance.PortraitCameraPrefab);
			CameraController.Instance.PortraitCamera = gameObject.GetComponent<Camera>();
		}
		CameraController.SetPortrait(parent);
		CursorManager.CursorCenter = Input.mousePosition;
		SetMenuState(showMenu: false, playSound: false);
		SetDynamicInvState(show: false);
		LeftHandSlot = DisplaySlots.Find((SlotDisplay slot) => slot.Slot.StringHash == Slot.LeftHandHash).Slot;
		RightHandSlot = DisplaySlots.Find((SlotDisplay slot) => slot.Slot.StringHash == Slot.RightHandHash).Slot;
		BeltSlot = DisplaySlots.Find((SlotDisplay s) => s.Slot.Type == Slot.Class.Belt).Slot;
		AnimateActiveHands();
		RefreshSlotWearVisibility();
		if (!GameManager.IsBatchMode)
		{
			InventoryWindowManager.AssignParent(Parent);
			EnvironmentalAudioHandler.Instance.InitReverbZone(Parent);
		}
		RefreshDisplaySlotBindings();
		Localization.OnLanguageChanged = (Action)Delegate.Combine(Localization.OnLanguageChanged, new Action(RefreshDisplaySlotBindings));
		KeyManager.OnControlsChanged += RefreshDisplaySlotBindings;
		ParentHuman.RefreshBackSlots();
		PlayerStateWindow.Instance?.UpdateDaysPastText();
		WorldManager.PublishDaysPast();
	}

	public static void RefreshSlotWearVisibility()
	{
		if (!Parent)
		{
			return;
		}
		foreach (Slot slot in Parent.Slots)
		{
			if ((bool)slot.Occupant)
			{
				slot.Occupant.SetWearVisibility();
			}
		}
	}

	public static void SpawnDynamicThing(ICreativeSpawnable prefab)
	{
		if (prefab is DynamicThing dynamicThing)
		{
			SpawnDynamicThing(dynamicThing.PrefabName);
		}
		if (prefab is SpawnData spawnData)
		{
			OnServer.SpawnSpawnData(Parent.ReferenceId, spawnData.IdHash);
		}
	}

	public static void SpawnDynamicThing(string prefabName)
	{
		OnServer.SpawnDynamicThingMaxStack(Parent.ReferenceId, prefabName);
	}

	public void ButtonSettings()
	{
		SettingsPanel.SetActive(value: true);
		Settings.Instance.InitCurrentPage();
	}

	public void ToggleGameMenuVisibility(bool enable)
	{
		if (GameManager.GameState != GameState.None)
		{
			GameMenuPanel.SetActive(enable);
		}
	}

	public static void OnActiveEvent()
	{
		InventoryManager.OnActiveHandChanged?.Invoke();
	}

	public void CancelKeyActionsTypingState()
	{
		if (RocketCanvas.Instance.IsVisible)
		{
			RocketCanvas.Instance.Hide();
		}
		if (InputPrefabs.Instance.IsVisible)
		{
			InputPrefabs.Instance.ClearFilter();
		}
	}

	public void CancelKeyActions()
	{
		if (!KeyManager.IsMenuInputAllowed)
		{
			return;
		}
		if (ScreenScoreBoard.GameObject.activeSelf)
		{
			ToggleScoreboard();
		}
		if (ImguiCreativeSpawnMenu.Show)
		{
			SetDynamicInvState(show: false);
			return;
		}
		if (AlertPanel.Instance.AlertWindow.activeInHierarchy)
		{
			AlertPanel.Instance.DisableAlertPanel();
			return;
		}
		if (PromptPanel.Instance.PromptWindow.activeInHierarchy)
		{
			if (PromptPanel.Instance.IsEscapable)
			{
				PromptPanel.Instance.CancelButton.onClick.Invoke();
			}
			else if (Parent == null || Parent.IsUnresponsive)
			{
				PromptPanel.Instance.CancelButton.onClick.Invoke();
				SetMenuState(!ShowMenu);
			}
			return;
		}
		if (Singleton<ConfirmationPanel>.Instance.gameObject.activeInHierarchy)
		{
			Singleton<ConfirmationPanel>.Instance.OnEscapePressed();
			return;
		}
		if (DropdownState == InputPanelState.Waiting)
		{
			ButtonDropdownCancel();
			return;
		}
		if (InputPrefabs.Instance.isActiveAndEnabled)
		{
			InputPrefabs.CancelInput();
			return;
		}
		if (Stationpedia.IsOpen)
		{
			Stationpedia.Cancel();
			return;
		}
		if (InputWindowBase.IsInputWindow)
		{
			InputWindowBase.Cancel();
			return;
		}
		if ((bool)WorkshopMenu.Instance && WorkshopMenu.Instance.isActiveAndEnabled)
		{
			if ((bool)WorkshopMenu.Instance)
			{
				WorkshopMenu.Instance.CloseWorkshopMenu();
			}
			return;
		}
		if (CharacterCustomisationManager.IsSceneLoaded)
		{
			CharacterCustomisationManager.UnloadScene();
			return;
		}
		if (XmlSaveLoad.Instance.PanelSave.activeInHierarchy)
		{
			XmlSaveLoad.Instance.ButtonCancel();
			return;
		}
		if (Settings.Instance.isActiveAndEnabled)
		{
			Settings.Instance.CloseSetting();
			return;
		}
		SetMenuState(!ShowMenu);
		if (ShowMenu)
		{
			return;
		}
		if ((bool)Parent)
		{
			if (Parent.IsUnresponsive && !ShowMenu && (bool)ParentHuman)
			{
				ParentHuman.ShowHumanRespawnPrompt();
			}
		}
		else
		{
			Human.DisplayDecayPrompt();
		}
	}

	private static void OnChatMessage(string input1, string input2)
	{
		if (!string.IsNullOrWhiteSpace(input1))
		{
			ChatMessage chatMessage = new ChatMessage
			{
				ChatText = input1,
				DisplayName = Human.LocalHuman.DisplayName,
				HumanId = Human.LocalHuman.ReferenceId
			};
			if (NetworkManager.IsServer)
			{
				chatMessage.DisplayName += " (Host)";
			}
			if (NetworkManager.IsClient)
			{
				NetworkClient.SendToServer(chatMessage);
			}
			else if (NetworkManager.IsServer)
			{
				chatMessage.PrintToConsole();
				NetworkServer.SendToClients(chatMessage, NetworkChannel.GeneralTraffic, -1L);
			}
			else
			{
				chatMessage.PrintToConsole();
			}
		}
	}

	private static void OnTypeChatMessage(string text)
	{
	}

	private void RunGameState()
	{
		if (GameManager.GameState != GameState.Running || !KeyManager.IsMenuInputAllowed || WorldManager.IsGamePaused || GameManager.IsBatchMode || InGameMenuOpen)
		{
			return;
		}
		if (KeyManager.GetButtonDown(KeyMap.Chatting))
		{
			HandleReturnInput();
		}
		if (KeyManager.GetButtonDown(KeyMap.ShowDynamicPanel) && !ShowMenu && !InputWindowBase.IsInputWindow)
		{
			SetDynamicInvState(!ImguiCreativeSpawnMenu.Show);
		}
		if (KeyManager.GetButtonDown(KeyMap.ToggleHelperHints))
		{
			HelperHintsTextController.ToggleMainPanel();
		}
		if (KeyManager.GetButtonDown(KeyMap.ToggleUi))
		{
			SetUiState(!ShowUi);
			if (ShowUi)
			{
				ActiveHand.Animate(SlotDisplayState.Highlighted);
			}
		}
		if (!ShowUi && KeyManager.GetButtonDown(KeyMap.Cancel))
		{
			SetUiState(showUi: true);
			ActiveHand.Animate(SlotDisplayState.Highlighted);
		}
		if (KeyManager.GetButtonDown(KeyMap.ToggleInfo))
		{
			SetGameInfoState(PanelTabInstructions.activeSelf, hideUI: false);
		}
		if (KeyManager.GetButtonDown(KeyMap.HideAllWindows))
		{
			InventoryWindowManager.HideAll();
		}
	}

	private void HandleReturnInput()
	{
		if (DropdownState == InputPanelState.Waiting)
		{
			ButtonDropdownSubmit();
		}
		if (InputWindow.InputState == InputPanelState.Waiting)
		{
			if (InputWindow.IsMultiLine())
			{
				return;
			}
			InputWindow.SubmitInput();
		}
		if (!Stationpedia.IsOpen && !InputWindowBase.IsInputWindow && !ConsoleWindow.IsOpen && !RocketCanvas.Instance.IsVisible)
		{
			InputWindow.GetText(GameStrings.InputChatMessage, _chatMessage, _onTypeChatMessage).Forget();
		}
	}

	private void DebugContentsMethod()
	{
		if ((bool)DebugContents)
		{
			_gasContents.Clear();
			if (Parent.BreathingAtmosphere != null)
			{
				GasMixture gasMixture = Parent.BreathingAtmosphere.GasMixture;
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.Oxygen);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.Nitrogen);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.CarbonDioxide);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.Methane);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.Pollutant);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.Water);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.PollutedWater);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.NitrousOxide);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.LiquidNitrogen);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.LiquidOxygen);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.LiquidMethane);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.LiquidCarbonDioxide);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.LiquidPollutant);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.LiquidNitrousOxide);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.Steam);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.Hydrogen);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.LiquidHydrogen);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.Hydrazine);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.LiquidHydrazine);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.LiquidAlcohol);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.Helium);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.LiquidSodiumChloride);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.Silanol);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.LiquidSilanol);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.HydrochloricAcid);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.LiquidHydrochloricAcid);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.Ozone);
				AtmosphericsManager.DisplayGas(_gasContents, gasMixture, gasMixture.LiquidOzone);
			}
			else
			{
				_gasContents.AppendLine("None");
			}
			DebugContents.text = _gasContents.ToString();
		}
	}

	private void Update_UnityEditor()
	{
		if (GameManager.GameState != GameState.Running || !KeyManager.GetButton(KeyCode.LeftShift))
		{
			return;
		}
		if (KeyManager.GetButtonUp(KeyCode.F12))
		{
			AudioSource[] array = UnityEngine.Object.FindObjectsOfType(typeof(AudioSource)) as AudioSource[];
			foreach (AudioSource audioSource in array)
			{
				if (audioSource.isPlaying)
				{
					Debug.Log(audioSource.name + " is playing " + audioSource.clip.name + " at volume " + audioSource.volume);
				}
			}
			Debug.Log("---------------------------");
			Debug.Break();
		}
		if (KeyManager.GetButtonUp(KeyCode.F10) && (bool)ControlsPanel.Instance)
		{
			ControlsPanel.Instance.DisableControlsPanel();
		}
		if (KeyManager.GetButtonUp(KeyCode.F8))
		{
			ParentHuman.Collider.enabled = !ParentHuman.Collider.enabled;
		}
	}

	private void UpdateParentAnimator()
	{
		_parentAnimator.SetInteger(MovementController.ActiveHandHash, ActiveHand.Slot.SlotIndex);
		_parentAnimator.SetBool(MovementController.HasItemHash, ActiveHand.Slot.Occupant);
		if (_parentAnimator.GetBool(MovementController.HasItemHash))
		{
			if (ActiveHand.Slot.Occupant is Item item)
			{
				_parentAnimator.SetInteger(MovementController.HandGripHash, (int)item.GripType);
				_parentAnimator.SetFloat(MovementController.CastingAnimationHash, (float)ActiveHandSlot.Occupant.CastAnimation);
			}
			else
			{
				_parentAnimator.SetInteger(MovementController.HandGripHash, 1);
				_parentAnimator.SetFloat(MovementController.CastingAnimationHash, 0f);
			}
		}
	}

	private static void HandleHeatHaze()
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		if (Parent.WorldAtmosphere != null && Parent.WorldAtmosphere.IsAboveArmstrong())
		{
			FirstPersonHelmetOverlay.CurrentFrostSetting = 0f - RocketMath.MapToScale(Chemistry.Temperature.ZeroDegrees.ToFloat(), Chemistry.Temperature.TwentyDegrees.ToFloat(), 0f, 1f, Parent.WorldAtmosphere.Temperature.ToFloat());
			if (Parent.WorldAtmosphere.Temperature > HeatHazeThreshold)
			{
				HeatHaze.Apply(Parent.WorldAtmosphere);
			}
			else
			{
				HeatHaze.Clear();
			}
		}
		else
		{
			FirstPersonHelmetOverlay.CurrentFrostSetting = 0f;
			HeatHaze.Clear();
		}
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		RunGameState();
		if (!Parent || WorldManager.IsGamePaused)
		{
			return;
		}
		WorldPosition = ((CameraController.CinematicMode && CameraController.Instance != null) ? CameraController.Instance.MainCameraTransform.position : Parent.ThingTransformPosition);
		CheckRichPresence();
		UpdateParentAnimator();
		DebugContentsMethod();
		HandleHeatHaze();
		HandleNearRockets();
		if (Stationpedia.IsOpen && Input.GetKeyDown(KeyMap.Cancel))
		{
			Stationpedia.Cancel();
		}
		bool flag = Parent == null || Parent.IsUnresponsive || Parent.IsSleeping || Parent.RootParent is ILifeSuspender;
		if (_isUnconscious != flag)
		{
			_isUnconscious = flag;
			InventoryWindowManager.Instance?.ToggleWindows(!flag);
			ClothingPanel.SetActive(!flag);
			PanelHandsGameObject.SetActive(!flag);
			UiCursor.SetActive(!flag);
			InventoryWindowManager.ReShowCloseAllButton();
		}
		if (Cursor.visible || Parent.IsUnresponsive || ConsoleWindow.IsOpen)
		{
			return;
		}
		CheckDisplaySlotInput();
		CheckSeatedInput();
		if (!Input.GetKey(KeyMap.HelmetSlot) && !Input.GetKey(KeyMap.GlassesSlot) && !Input.GetKey(KeyMap.BackSlot) && !Input.GetKey(KeyMap.SuitSlot) && !Input.GetKey(KeyMap.UniformSlot) && !Input.GetKey(KeyMap.ToolBeltSlot))
		{
			_canEquip = true;
			_currentDropTime = 0f;
		}
		else if (_currentDropTime <= _dropTime + 1f)
		{
			_currentDropTime += Time.deltaTime;
		}
		if (Parent.State == EntityState.Alive && IsAllowedToLook() && IsParentSafe() && !Stationpedia.IsOpenAndLocked)
		{
			switch (CurrentMode)
			{
			case Mode.Normal:
				NormalMode();
				break;
			case Mode.Placement:
				PlacementMode();
				break;
			case Mode.PrecisionPlacement:
				PrecisionPlacementMode();
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
		else
		{
			ClearCursor();
		}
		TooltipRef.DrawTooltip();
		if (ActiveHandSlot.Contains<TerrainEditor>(out var occupant))
		{
			TerrainEditor.ShowWindow(occupant);
		}
		else
		{
			TerrainEditor.HideWindow();
		}
	}

	private void CheckSeatedInput()
	{
		if (!(Parent == null) && !Parent.IsUnresponsive && KeyManager.GetButton(KeyMap.OpenSeatScreen) && Parent?.ParentSlot?.Parent is CrewModuleChair crewModuleChair)
		{
			crewModuleChair.OpenSeatScreen();
		}
	}

	private RichPresenceState GetRichPresenceState()
	{
		if (ParentHuman != null)
		{
			if (ParentHuman != null && ParentHuman.ParentSlot?.Parent is CrewModuleChair)
			{
				return RichPresenceState.InRocket;
			}
			return RichPresenceState.DaysInWorld;
		}
		if (ParentBrain != null)
		{
			return RichPresenceState.Disembodied;
		}
		return RichPresenceState.None;
	}

	private void CheckRichPresence()
	{
		if (!SteamClient.IsValid)
		{
			return;
		}
		RichPresenceState richPresenceState = GetRichPresenceState();
		if (richPresenceState != _richPresenceState)
		{
			_richPresenceState = richPresenceState;
			switch (_richPresenceState)
			{
			case RichPresenceState.Disembodied:
			case RichPresenceState.DaysInWorld:
				GameManager.SetSteamRichPresence("steam_display", "#Status_DaysInWorld");
				break;
			case RichPresenceState.InRocket:
				GameManager.SetSteamRichPresence("steam_display", "#Status_InRocket");
				break;
			default:
				throw new ArgumentOutOfRangeException();
			case RichPresenceState.None:
				break;
			}
		}
	}

	private void HandleNearRockets()
	{
		float num = 0f;
		foreach (Rocket allRocket in Rocket.AllRockets)
		{
			if (allRocket.RocketState == RocketState.InSpace)
			{
				continue;
			}
			float num2 = Vector3.SqrMagnitude(allRocket.GetWorldPosition() - WorldPosition);
			if (num2 > 6400f)
			{
				continue;
			}
			float num3 = allRocket.GetThrust();
			if (!(num3 <= float.Epsilon))
			{
				if (num2 > float.Epsilon)
				{
					num3 /= num2;
				}
				num += num3;
			}
		}
		if (!(num <= float.Epsilon))
		{
			CameraController.SetCameraShake(num / 6000f);
		}
	}

	private static bool IsAllowedToLook()
	{
		if (Parent.IsChild)
		{
			return Parent.AllowChildInteraction;
		}
		return true;
	}

	private static bool IsLookingAtParent()
	{
		if (Parent.ParentSlot != null)
		{
			return CursorManager.CursorThing == Parent.ParentSlot.Parent;
		}
		return false;
	}

	private static bool IsParentSafe()
	{
		if (Parent.IsChild)
		{
			return CurrentMode == Mode.Normal;
		}
		return true;
	}

	private static IEnumerator HighlightSlot(int i)
	{
		if (GameManager.GameState == GameState.Running)
		{
			Parent.Slots[i].Display.Animate(SlotDisplayState.Normal);
			yield return Yielders.WaitForSecondsRealtime(0.1f);
			Parent.Slots[i].Display.Animate(SlotDisplayState.Highlighted);
		}
	}

	private async UniTaskVoid DropSlot(SlotDisplay displaySlot)
	{
		while (_currentDropTime > 0f && _canEquip)
		{
			float dropRatio = _currentDropTime / _dropTime;
			displaySlot.SlotDisplayButton.SetDropRatio(dropRatio);
			await UniTask.NextFrame();
		}
		displaySlot.SlotDisplayButton.SetDropRatio(0f);
		displaySlot.Animate(SlotDisplayState.Normal);
	}

	public static void AnimateActiveHands()
	{
		if (LeftHandSlot != null && RightHandSlot != null)
		{
			LeftHandSlot.Display.SlotDisplayButton.RefreshAnimation();
			RightHandSlot.Display.SlotDisplayButton.RefreshAnimation();
		}
	}

	private void CheckDisplaySlotInput()
	{
		newScrollData = Input.mouseScrollDelta.y / 10f;
		if (!KeyManager.GetButton(KeyMap.QuantityModifier))
		{
			if (ConstructionPanel.IsVisible)
			{
				if (newScrollData < 0f)
				{
					ConstructionPanel.SelectUp();
				}
				else if (newScrollData > 0f)
				{
					ConstructionPanel.SelectDown();
				}
				if (!ConstructionPanel.Parent)
				{
					ConstructionPanel.SetVisible(isVisble: false);
				}
			}
			if (!ConstructionPanel.Parent)
			{
				ConstructionPanel.SetVisible(isVisble: false);
			}
		}
		if (KeyManager.GetButtonDown(KeyMap.NextItem))
		{
			InventoryWindowManager.PreviousButton();
		}
		else if (KeyManager.GetButtonDown(KeyMap.PreviousItem))
		{
			InventoryWindowManager.NextButton();
		}
		if (CurrentMode != Mode.Placement)
		{
			float num = newScrollData * (float)((!Settings.CurrentData.InvertMouseWheelInventory) ? 1 : (-1));
			if (num > 0f)
			{
				InventoryWindowManager.NextButton();
			}
			else if (num < 0f)
			{
				InventoryWindowManager.PreviousButton();
			}
		}
		foreach (SlotDisplay displaySlot in DisplaySlots)
		{
			if (displaySlot.SwapButton != string.Empty && CheckDisplaySlot(displaySlot))
			{
				return;
			}
			if ((bool)displaySlot.Slot.Occupant && displaySlot.Slot.Type != Slot.Class.None)
			{
				displaySlot.Slot.Occupant.CheckInteractionKeys();
			}
		}
		if (SuitBackSlotDisplay != null && (string.IsNullOrEmpty(SuitBackSlotDisplay.SwapButton) || !CheckDisplaySlot(SuitBackSlotDisplay)) && (bool)SuitBackSlotDisplay?.Slot?.Occupant)
		{
			SlotDisplayMirror suitBackSlotDisplay = SuitBackSlotDisplay;
			if (suitBackSlotDisplay == null || suitBackSlotDisplay.Slot?.Type != Slot.Class.None)
			{
				SuitBackSlotDisplay.Slot?.Occupant.CheckInteractionKeys();
			}
		}
	}

	private bool MoveEquipmentSlot(SlotDisplay displaySlot, string buttonName)
	{
		if (!_canEquip)
		{
			return false;
		}
		if (!Input.GetKey(KeyManager.GetKey(buttonName)))
		{
			return false;
		}
		if (displaySlot?.Slot == null)
		{
			return false;
		}
		if (displaySlot.Slot.IsHandSlot && displaySlot.Slot != ActiveHandSlot)
		{
			return false;
		}
		if (displaySlot.Slot == ParentHuman.BackpackSlot && ParentHuman.SuitSlot.Contains<SuitBase>())
		{
			return false;
		}
		if (_currentDropTime > 0f && _canEquip)
		{
			DropSlot(displaySlot).Forget();
		}
		if (_currentDropTime < _dropTime)
		{
			return false;
		}
		if (!Input.GetKey(KeyManager.GetKey(buttonName)))
		{
			return false;
		}
		if ((bool)displaySlot.Slot.Occupant && ActiveHand.Slot.Occupant == null)
		{
			OnServer.MoveToSlot(displaySlot.Slot.Occupant, ActiveHand.Slot);
			_canEquip = false;
			_currentDropTime = 0f;
			return true;
		}
		if (displaySlot.Slot.Occupant == null && ActiveHand.Slot.Occupant != null && displaySlot.Slot.Type == ActiveHand.Slot.Occupant.SlotType)
		{
			OnServer.MoveToSlot(ActiveHand.Slot.Occupant, displaySlot.Slot);
			_canEquip = false;
			_currentDropTime = 0f;
			return true;
		}
		if (!displaySlot.Slot.Occupant)
		{
			return false;
		}
		if (displaySlot.Slot.Occupant != null && ActiveHand.Slot.Occupant != null && displaySlot.Slot.Occupant.SlotType != ActiveHand.Slot.Occupant.SlotType)
		{
			return false;
		}
		CheckCancelMultiConstructor();
		OnServer.SwapSlots(Parent.netId, Parent.netId, ActiveHand.Slot.SlotIndex, displaySlot.Slot.SlotIndex);
		_canEquip = false;
		_currentDropTime = 0f;
		return true;
	}

	private bool CheckDisplaySlot(SlotDisplay displaySlot, string buttonName)
	{
		if (MoveEquipmentSlot(displaySlot, buttonName))
		{
			displaySlot.KeyDownSwap = true;
			return false;
		}
		KeyCode key = KeyManager.GetKey(buttonName);
		if (displaySlot.KeyDownSwap && Input.GetKeyDown(key))
		{
			displaySlot.KeyDownSwap = false;
		}
		if (displaySlot.KeyDownSwap || !Input.GetKeyUp(key))
		{
			return false;
		}
		if (!(displaySlot.Slot?.Occupant))
		{
			return false;
		}
		if (displaySlot.Slot.IsHandSlot && displaySlot.Slot != ActiveHandSlot)
		{
			return false;
		}
		displaySlot.SlotDisplayButton.PrimaryAction(isButtonPress: true);
		return true;
	}

	private bool CheckDisplaySlot(SlotDisplay displaySlot)
	{
		return CheckDisplaySlot(displaySlot, displaySlot.SwapButton);
	}

	public void CheckCancelMultiConstructor()
	{
		if (PrecisionPlaceCursor != null)
		{
			CancelPlacement();
		}
		if (CurrentMode == Mode.Placement && ActiveHand.Slot.Contains<IConstructionStarter>())
		{
			ConstructionPanel.SetVisible(isVisble: false);
		}
	}

	public static void UpdatePrecisionPlacement(DynamicThing dynamicThing)
	{
		if ((bool)dynamicThing)
		{
			if ((bool)PrecisionPlaceCursor)
			{
				PrecisionPlaceCursor.SetActive(value: false);
			}
			_dynamicThingCursors.TryGetValue(dynamicThing.PrefabName, out PrecisionPlaceCursor);
			if ((bool)PrecisionPlaceCursor)
			{
				PrecisionPlaceCursor.transform.rotation = Quaternion.identity;
				PrecisionPlaceCursor.SetActive(value: true);
			}
		}
	}

	public static void UpdatePlacement(Constructor constructorItem)
	{
		if (constructorItem == null)
		{
			return;
		}
		Structure constructionCursor = ConstructionCursor;
		Quaternion rotation = Quaternion.identity;
		_constructionCursors.TryGetValue(constructorItem.BuildStructure.name, out ConstructionCursor);
		if (ConstructionCursor == constructionCursor)
		{
			return;
		}
		if ((bool)constructionCursor)
		{
			constructionCursor.gameObject.SetActive(value: false);
			rotation = constructionCursor.ThingTransform.rotation;
		}
		if ((bool)ConstructionCursor)
		{
			CurrentFace = RocketGrid.FaceInt.South;
			if ((ConstructionCursor.RotationAxis & RotationAxis.Y) == 0 && RocketGrid.FaceInt.IsHorizontalFace(CurrentFace))
			{
				CurrentFace = RocketGrid.FaceInt.Up;
			}
			if ((ConstructionCursor.RotationAxis & RotationAxis.X) == 0 && (CurrentFace == RocketGrid.FaceInt.Up || CurrentFace == RocketGrid.FaceInt.Down))
			{
				CurrentFace = RocketGrid.FaceInt.South;
			}
			if ((bool)ConstructionCursor)
			{
				CurrentFace = RocketGrid.FaceInt.Down;
			}
			if (ConstructionCursor.PlacementType == PlacementSnap.Face)
			{
				ConstructionCursor.ThingTransformPosition -= ConstructionCursor.GridSize * ConstructionCursor.ThingTransform.forward / 2f;
			}
			ConstructionCursor.gameObject.SetActive(value: true);
		}
		CurrentFace = RocketGrid.FaceInt.FaceIntFromDir(RocketGrid.GetForwardDir(ConstructionCursor.ThingTransform.forward));
		CurrentRotation = ConstructionCursor.ThingTransform.rotation;
		if ((bool)constructionCursor)
		{
			constructionCursor.ThingTransform.rotation = rotation;
		}
	}

	public static void UpdatePlacement(Structure structure)
	{
		if (structure == null)
		{
			return;
		}
		Structure constructionCursor = ConstructionCursor;
		int currentFace = CurrentFace;
		Quaternion currentRotation = CurrentRotation;
		Quaternion rotation = Quaternion.identity;
		_constructionCursors.TryGetValue(structure.name, out ConstructionCursor);
		if (ConstructionCursor == constructionCursor)
		{
			return;
		}
		if ((bool)constructionCursor)
		{
			rotation = constructionCursor.ThingTransform.rotation;
			constructionCursor.gameObject.SetActive(value: false);
		}
		if ((bool)ConstructionCursor)
		{
			CurrentFace = RocketGrid.FaceInt.FaceIntFromDir(RocketGrid.GetForwardDir(ConstructionCursor.ThingTransform.forward));
			if ((ConstructionCursor.AllowedRotations & AllowedRotations.Wall) == 0 && RocketGrid.FaceInt.IsHorizontalFace(CurrentFace))
			{
				if ((ConstructionCursor.AllowedRotations & AllowedRotations.Ceiling) != AllowedRotations.None)
				{
					CurrentFace = RocketGrid.FaceInt.Up;
					ConstructionCursor.ThingTransform.rotation = new Quaternion(1f / Mathf.Sqrt(2f), 0f, 0f, 1f / Mathf.Sqrt(2f));
				}
				else
				{
					CurrentFace = RocketGrid.FaceInt.Down;
					ConstructionCursor.ThingTransform.rotation = new Quaternion(-1f / Mathf.Sqrt(2f), 0f, 0f, 1f / Mathf.Sqrt(2f));
				}
			}
			else if ((ConstructionCursor.AllowedRotations & AllowedRotations.Ceiling) == 0 && CurrentFace == RocketGrid.FaceInt.Up)
			{
				CurrentFace = RocketGrid.FaceInt.South;
				ConstructionCursor.ThingTransform.rotation = Quaternion.identity;
			}
			else if ((ConstructionCursor.AllowedRotations & AllowedRotations.Floor) == 0 && CurrentFace == RocketGrid.FaceInt.Down)
			{
				CurrentFace = RocketGrid.FaceInt.South;
				ConstructionCursor.ThingTransform.rotation = Quaternion.identity;
			}
			ConstructionCursor.gameObject.SetActive(value: true);
			if ((bool)constructionCursor && constructionCursor.PlacementType == ConstructionCursor.PlacementType && constructionCursor.AllowedRotations == ConstructionCursor.AllowedRotations && constructionCursor.RotationAxis == ConstructionCursor.RotationAxis)
			{
				ConstructionCursor.ThingTransformPosition = constructionCursor.ThingTransformPosition;
				ConstructionCursor.ThingTransform.rotation = constructionCursor.ThingTransform.rotation;
				CurrentFace = currentFace;
				CurrentRotation = currentRotation;
			}
			else
			{
				CurrentRotation = ConstructionCursor.ThingTransform.rotation;
			}
		}
		if ((bool)constructionCursor)
		{
			constructionCursor.ThingTransform.rotation = rotation;
		}
	}

	public static Structure GetStructureCursor(string structureName)
	{
		Structure value = null;
		_constructionCursors.TryGetValue(structureName, out value);
		return value;
	}

	public void CancelPlacement()
	{
		CurrentMode = Mode.Normal;
		if ((bool)CursorManager.CursorSelectionRenderer)
		{
			CursorManager.CursorSelectionRenderer.gameObject.SetActive(value: false);
		}
		if ((bool)ConstructionCursor)
		{
			ConstructionCursor.gameObject.SetActive(value: false);
			ConstructionCursor = null;
		}
		if ((bool)PrecisionPlaceCursor)
		{
			PrecisionPlaceCursor.gameObject.SetActive(value: false);
			_placementZoom = 0f;
			if ((bool)ActiveHand.Slot.Occupant)
			{
				foreach (ThingRenderer renderer in ActiveHand.Slot.Occupant.Renderers)
				{
					renderer.Enabled = true;
				}
				foreach (Slot slot in ActiveHand.Slot.Occupant.Slots)
				{
					if (slot.Occupant == null || slot.HidesOccupant)
					{
						continue;
					}
					foreach (ThingRenderer renderer2 in slot.Occupant.Renderers)
					{
						renderer2.Enabled = true;
					}
				}
			}
		}
		if (ConstructionPanel.gameObject.activeSelf)
		{
			UIAudioManager.Play(HideCursorHash);
			ConstructionPanel.SetVisible(isVisble: false);
		}
	}

	private bool CanAttackWith(Collider selectedCollider)
	{
		if (selectedCollider.isTrigger && CursorManager.CursorThing.ThingTransform != selectedCollider.transform)
		{
			return CursorManager.CursorThing is StaticDecal;
		}
		return true;
	}

	private static Slot FindFreeSlotOpenWindowsSlotPriority(Slot.Class slotType, bool requiredVisible = true)
	{
		List<Slot> list = FindFreeSlotFromOpenWindows(slotType, requiredVisible);
		foreach (Slot item in list)
		{
			if (slotType == item.Type)
			{
				return item;
			}
		}
		if (list.Count > 0)
		{
			return list[0];
		}
		return null;
	}

	private static List<Slot> FindFreeSlotFromOpenWindows(Slot.Class slotType, bool requiredVisible = true)
	{
		List<Slot> list = new List<Slot>();
		foreach (InventoryWindow window in InventoryWindowManager.Instance.Windows)
		{
			if ((bool)window && (bool)window.Parent && !(!window.IsVisible && requiredVisible))
			{
				list.AddRange(window.Parent.GetFreeSlots(slotType));
			}
		}
		return list;
	}

	public static void SmartStow(Slot selectedSlot)
	{
		if (selectedSlot == null || selectedSlot.Occupant == null)
		{
			return;
		}
		if (LeftHandSlot.Occupant == selectedSlot.Occupant || RightHandSlot.Occupant == selectedSlot.Occupant)
		{
			Slot slot = ParentHuman.GetFreeSlot(selectedSlot.Occupant.SlotType, _excludeHandSlots);
			if (slot != null && slot == ParentHuman.BackpackSlot && ParentHuman.SuitSlot.Contains<SuitBase>())
			{
				slot = null;
			}
			if (slot == null)
			{
				OriginalSlot[] originalSlots = _originalSlots;
				for (int i = 0; i < originalSlots.Length; i++)
				{
					OriginalSlot originalSlot = originalSlots[i];
					if (selectedSlot.Occupant.ReferenceId == originalSlot.ReferenceId)
					{
						if (originalSlot.ThingOriginalSlot == null || originalSlot.ThingOriginalSlot.Parent == null || originalSlot.ThingOriginalSlot.Parent.RootParent != ParentHuman.RootParent)
						{
							return;
						}
						slot = originalSlot.ThingOriginalSlot.Parent.GetFreeSlot(selectedSlot.Occupant.SlotType);
						if (slot != null && (slot.Type == Slot.Class.None || slot.Type == originalSlot.ThingOriginalSlot.Type))
						{
							int num = ((!(LeftHandSlot.Occupant == selectedSlot.Occupant)) ? 1 : 0);
							_originalSlots[num].ReferenceId = 0L;
							_originalSlots[num].ThingOriginalSlot = null;
							break;
						}
					}
				}
			}
			if (slot == null)
			{
				slot = FindFreeSlotOpenWindowsSlotPriority(selectedSlot.Occupant.SlotType);
			}
			if (slot == null)
			{
				foreach (Slot slot2 in ParentHuman.Slots)
				{
					if (slot2 != null && !_excludeHandSlots.Contains(slot2.Action) && slot2.Occupant != null)
					{
						Slot freeSlot = slot2.Occupant.GetFreeSlot(selectedSlot.Occupant.SlotType);
						if (freeSlot != null)
						{
							slot = freeSlot;
							Instance.StartCoroutine(PerformHiddenSlotMoveToAnimation(slot2, selectedSlot, selectedSlot.Occupant));
							break;
						}
					}
				}
			}
			if (slot == null)
			{
				UIAudioManager.Play(UIAudioManager.ActionFailHash);
				return;
			}
			Instance.CheckCancelMultiConstructor();
			OnServer.MoveToSlot(selectedSlot.Occupant, slot);
			InventoryWindowManager.Instance.TryUpdateSelectedInventorySlot(slot);
			UIAudioManager.Play(UIAudioManager.AddToInventoryHash);
		}
		else if (LeftHandSlot.Occupant == null)
		{
			_originalSlots[0].ThingOriginalSlot = selectedSlot;
			_originalSlots[0].ReferenceId = selectedSlot.Occupant.ReferenceId;
			OnServer.MoveToSlot(selectedSlot.Occupant, LeftHandSlot);
			UIAudioManager.Play(UIAudioManager.ObjectIntoHandHash);
		}
		else if (RightHandSlot.Occupant == null)
		{
			_originalSlots[1].ThingOriginalSlot = selectedSlot;
			_originalSlots[1].ReferenceId = selectedSlot.Occupant.ReferenceId;
			OnServer.MoveToSlot(selectedSlot.Occupant, RightHandSlot);
			UIAudioManager.Play(UIAudioManager.ObjectIntoHandHash);
		}
		else
		{
			UIAudioManager.Play(UIAudioManager.ActionFailHash);
		}
	}

	private static IEnumerator PerformHiddenSlotMoveToAnimation(Slot freeSlotParent, Slot currentItemSlot, DynamicThing dynamicThing)
	{
		if (freeSlotParent.Display != null && !(freeSlotParent.Display.DisplayImage == null))
		{
			freeSlotParent.Button.SetDisplayAnimState(SlotDisplayState.Highlighted);
			freeSlotParent.Display.DisplayImage.sprite = dynamicThing.GetThumbnail();
			yield return Yielders.WaitForSeconds(_animationHighlightDelay);
			freeSlotParent.Button.SetDisplayAnimState(SlotDisplayState.Normal);
			freeSlotParent.RefreshSlotDisplay();
		}
	}

	private void CheckSmartTool_Construction()
	{
	}

	public void CancelSmartTool()
	{
	}

	private bool NormalModeThing()
	{
		Collider cursorTargetCollider = CursorManager.Instance.CursorTargetCollider;
		Interactable interactable = ((CursorManager.CursorThing != null) ? CursorManager.CursorThing.GetInteractable(cursorTargetCollider) : null);
		PassiveTooltip cursorPassiveTooltip = ((CursorManager.CursorThing != null) ? CursorManager.CursorThing.GetPassiveTooltip(null) : default(PassiveTooltip));
		PassiveTooltip cursorPassiveTooltip2 = ((CursorManager.CursorThing != null) ? CursorManager.CursorThing.GetPassiveTooltip(cursorTargetCollider) : default(PassiveTooltip));
		Thing.DelayedActionInstance delayedActionInstance = null;
		if (CanAttackWith(cursorTargetCollider))
		{
			Attack attack = new Attack(ActiveHand.Slot, InactiveHand.Slot, CursorManager.CursorHit.point, CursorManager.CursorThing, 0f, cursorTargetCollider, (bool)(ActiveHand.Slot.Occupant as AuthoringTool) && Input.GetKey(KeyMap.QuantityModifier), (bool)(ActiveHand.Slot.Occupant as AuthoringTool) && Input.GetKey(KeyCode.F));
			delayedActionInstance = CursorManager.CursorThing.AttackWith(attack, doAction: false);
		}
		else
		{
			CancelSmartTool();
		}
		Interaction interaction = default(Interaction);
		Thing.DelayedActionInstance delayedActionInstance2 = null;
		Thing.DelayedActionInstance delayedActionInstance3 = null;
		if (interactable != null)
		{
			interaction = new Interaction(Parent, ActiveHandSlot, CursorManager.CursorThing, KeyManager.GetButton(KeyMap.QuantityModifier));
			delayedActionInstance2 = ((!CursorManager.CursorThing.PreventInteraction(out var failResult, interactable, interaction)) ? CursorManager.CursorThing.InteractWith(interactable, interaction, doAction: false) : failResult);
		}
		if (CursorManager.CursorThing.RootParent == Parent)
		{
			interactable = null;
		}
		if (CursorManager.CursorThing is Human { IsDraggable: not false } human)
		{
			delayedActionInstance3 = human.DragInto(ActiveHandSlot, CursorManager.CursorHit.point, doAction: false);
		}
		Color color = Color.red;
		Item item = CursorManager.CursorThing as Item;
		if ((item != null && !item.IsChild) || interactable != null || delayedActionInstance != null || delayedActionInstance3 != null)
		{
			if (MyGraphicsRaycaster.ShouldCast && (bool)item)
			{
				if (interactable == null && delayedActionInstance == null)
				{
					return delayedActionInstance3 != null;
				}
				return true;
			}
			SelectionInstance selection = CursorManager.CursorThing.GetSelection();
			if (interactable != null && delayedActionInstance == null)
			{
				selection = interactable.GetSelection();
			}
			else if (delayedActionInstance != null && delayedActionInstance.Selection != null)
			{
				selection = delayedActionInstance.Selection;
			}
			if (delayedActionInstance3 != null)
			{
				color = ((!delayedActionInstance3.IsDisabled) ? ((delayedActionInstance3.Duration > 0f) ? Color.yellow : Color.green) : Color.red);
				PassiveTooltip cursorPassiveTooltip3 = new PassiveTooltip(delayedActionInstance3, string.Empty, CursorManager.CursorThing);
				cursorPassiveTooltip3.color = color;
				TooltipRef.HandleToolTipDisplay(cursorPassiveTooltip3);
			}
			else if (interactable != null && delayedActionInstance2 != null && delayedActionInstance == null)
			{
				PassiveTooltip cursorPassiveTooltip4 = new PassiveTooltip(delayedActionInstance2, string.Empty, CursorManager.CursorThing);
				Tooltip.SetValuesForInteractable(ref cursorPassiveTooltip4, CursorManager.CursorThing, interactable);
				color = ((delayedActionInstance2.IsDisabled || !CursorManager.CursorThing.AllowInteraction) ? Color.red : ((!WillStackFromInteractable(interactable)) ? ((delayedActionInstance2.Duration > 0f) ? Color.yellow : Color.green) : Color.yellow));
				TooltipRef.HandleToolTipDisplay(cursorPassiveTooltip4);
				if (KeyManager.GetMouseDown("Primary") && delayedActionInstance2.Duration >= 0f && CursorManager.CursorThing.AllowInteraction)
				{
					_canDoHandlePrimaryUse = false;
					if (delayedActionInstance2.Duration > 0f && !delayedActionInstance2.IsDisabled)
					{
						ActionCoroutine = StartCoroutine(WaitUntilDone(interactable, delayedActionInstance2, interaction));
					}
					else if (GameManager.RunSimulation)
					{
						OnServer.InteractWith(interactable, interaction);
					}
					else
					{
						NetworkClient.InteractWith(interactable, interaction);
					}
				}
			}
			else if (delayedActionInstance != null)
			{
				color = ((!delayedActionInstance.IsDisabled) ? ((delayedActionInstance.Duration > 0f) ? Color.yellow : Color.green) : Color.red);
				PassiveTooltip cursorPassiveTooltip5 = new PassiveTooltip(delayedActionInstance, string.Empty, CursorManager.CursorThing);
				cursorPassiveTooltip5.color = color;
				TooltipRef.HandleToolTipDisplay(cursorPassiveTooltip5);
			}
			else if (item != null)
			{
				color = (cursorPassiveTooltip.color = Tooltip.SetColorForItemAction(ref cursorPassiveTooltip, item));
				TooltipRef.HandleToolTipDisplay(cursorPassiveTooltip);
			}
			color.a = CursorAlphaInteractable;
			CursorManager.SetSelection(selection, color);
			CursorManager.SetSelectionVisibility(ShowUi);
		}
		else if (!cursorPassiveTooltip2.Title.Equals(string.Empty))
		{
			TooltipRef.HandleToolTipDisplay(cursorPassiveTooltip2);
			CursorManager.ClearLastSelectionId();
		}
		else
		{
			CursorManager.SetSelectionVisibility(isVisible: false);
			CursorManager.ClearLastSelectionId();
			if (cursorPassiveTooltip.Title != string.Empty && CursorManager.CursorThing.RootParent != Parent && !IsLookingAtParent())
			{
				TooltipRef.Mode = TooltipMode.ActionLast;
				TooltipRef.HandleToolTipDisplay(cursorPassiveTooltip);
				if (interactable == null)
				{
					return delayedActionInstance != null;
				}
				return true;
			}
		}
		if (interactable == null)
		{
			return delayedActionInstance != null;
		}
		return true;
	}

	public void ClearCursor()
	{
		if (GameManager.GameState == GameState.Running)
		{
			CursorManager.Instance.CursorHighlighter.transform.position = CursorManager.CursorPlanePosition;
			CursorManager.Instance.CursorHighlighter.SetActive(value: false);
			CursorManager.SetSelectionVisibility(isVisible: false);
		}
	}

	private void NormalMode()
	{
		DynamicThing dynamicThing = ActiveHandSlot?.Occupant;
		if (dynamicThing is CableGun cableGun && dynamicThing.OnOff)
		{
			cableGun.TickPlacement();
			ClearCursor();
			CursorManager.SetSelectionVisibility(isVisible: false);
			CursorManager.ClearLastSelectionId();
			TooltipRef.HandleToolTipDisplay(cableGun.BuildTooltip());
			return;
		}
		bool flag = false;
		ClearCursor();
		if ((bool)CursorManager.CursorThing)
		{
			flag = NormalModeThing();
		}
		else if (CursorManager.CursorTerrain.IsValid)
		{
			CursorManager.ClearLastSelectionId();
			Vector3 currentVoxelWorld = CursorManager.GetCurrentVoxelWorld();
			byte activeIndex = byte.MaxValue;
			if (CursorManager.CursorTerrain.IsValid && CursorManager.CursorTerrain.CursorVein != null && CursorManager.CursorTerrain.CursorVein.HasActivePosition(currentVoxelWorld.FloorToInt(), out activeIndex) && ActionCoroutine == null)
			{
				tooltip.Action = ActionStrings.Mine;
				tooltip.color = Color.white;
				tooltip.ShowAction = true;
				tooltip.Title = Localization.GetName(CursorManager.CursorTerrain.CursorVein.Type);
				tooltip.Slider = -1f;
				TooltipRef.HandleToolTipDisplay(tooltip);
			}
			if (ActiveHand.Slot.Contains<IMiningTool>(out var occupant) && occupant.IsAvailable())
			{
				CursorManager.CursorSelectionTransform.localScale = Vector3.one;
				CursorManager.CursorSelectionTransform.position = currentVoxelWorld;
				CursorManager.CursorSelectionTransform.rotation = Quaternion.identity;
				if (KeyManager.GetMouse("Primary") && ActionCoroutine == null)
				{
					float mineCompletionTime = occupant.GetMineCompletionTime();
					mineCompletionTime = ((CursorManager.CursorTerrain.CursorVein != null) ? (mineCompletionTime * CursorManager.CursorTerrain.CursorVein.Data.MiningTime) : (mineCompletionTime * 1f));
					float mineAmount = occupant.GetMineAmount();
					ActionCoroutine = StartCoroutine(WaitUntilDone(delegate
					{
						MineAsteroidComplete(mineAmount);
					}, mineCompletionTime, "", currentVoxelWorld, CursorManager.CursorTerrain, activeIndex));
				}
			}
		}
		else
		{
			CursorManager.SetSelectionVisibility(isVisible: false);
			CursorManager.ClearLastSelectionId();
		}
		if (CursorManager.CursorThing == null)
		{
			CancelSmartTool();
		}
		if (KeyManager.GetMouse("Primary"))
		{
			if (_canDoHandlePrimaryUse)
			{
				HandlePrimaryUse();
			}
		}
		else
		{
			_canDoHandlePrimaryUse = true;
		}
		if (ActiveHand.Slot.IsNotEmpty() && KeyManager.GetButtonDown(KeyMap.PrecisionPlace))
		{
			if (ActiveHand.Slot.Contains<IDraggable>())
			{
				return;
			}
			CurrentMode = Mode.PrecisionPlacement;
			CursorManager.SetSelectionVisibility(isVisible: false);
			CursorManager.ClearLastSelectionId();
			UpdatePrecisionPlacement(ActiveHand.Slot.Occupant);
		}
		if ((bool)ActiveHand.Slot.Occupant && KeyManager.GetMouseDown("Secondary"))
		{
			dynamicThing = ActiveHand.Slot.Occupant;
			if (!(dynamicThing is Constructor constructorItemPlacement))
			{
				if (!(dynamicThing is MultiConstructor multiConstructorItemPlacement))
				{
					if (!(dynamicThing is AuthoringTool))
					{
						if (dynamicThing is Item item)
						{
							Thing.DelayedActionInstance delayedActionInstance = item.OnUseSecondary();
							if (delayedActionInstance != null && delayedActionInstance.Duration > 0f)
							{
								ActionCoroutine = StartCoroutine(WaitUntilDone(UseSecondaryComplete, delayedActionInstance.Duration, delayedActionInstance.ActionMessage, item, delayedActionInstance.ActionSoundHash, delayedActionInstance.ActionCompleteSoundHash));
							}
							else if (GameManager.RunSimulation)
							{
								OnServer.UseItemSecondary(Parent, ActiveHand.SlotId, LastCompletedRatio);
							}
							else
							{
								NetworkClient.UseItemSecondary(Parent, ActiveHand.SlotId, LastCompletedRatio);
							}
						}
					}
					else
					{
						ICreativeSpawnable spawnPrefab = SpawnPrefab;
						if (!(spawnPrefab is Constructor constructorItemPlacement2))
						{
							if (spawnPrefab is MultiConstructor multiConstructorItemPlacement2)
							{
								SetMultiConstructorItemPlacement(multiConstructorItemPlacement2);
							}
						}
						else
						{
							SetConstructorItemPlacement(constructorItemPlacement2);
						}
					}
				}
				else
				{
					SetMultiConstructorItemPlacement(multiConstructorItemPlacement);
				}
			}
			else
			{
				SetConstructorItemPlacement(constructorItemPlacement);
			}
		}
		if (!CursorManager.CursorThing || flag)
		{
			return;
		}
		Item item2 = CursorManager.CursorThing as Item;
		if (!item2 || !item2.AllowInteraction || MyGraphicsRaycaster.ShouldCast || !KeyManager.GetMouseUp("Primary"))
		{
			return;
		}
		bool flag2 = false;
		if (CursorManager.CursorThing is IMergeable mergeable && ActiveHand.Slot.Contains<IMergeable>(out var occupant2) && mergeable.CanStack(occupant2))
		{
			Thing.CmdMoveToStack(mergeable, occupant2);
			ActiveHand.Slot.PlaySlotEnterUiSound();
			flag2 = true;
		}
		else if (ActiveHand.Slot.IsEmpty())
		{
			OnServer.MoveToSlot(item2, ActiveHand.Slot);
			_parentAnimator.SetBool(MovementController.HasItemHash, value: true);
			ActiveHand.Slot.PlaySlotEnterUiSound();
			flag2 = true;
		}
		if (flag2)
		{
			Vector3 zero = Vector3.zero;
			zero = ((!Parent.RigidBody.useGravity) ? ((item2.RigidBody.velocity - Parent.RigidBody.velocity) / 2f) : ((item2.RigidBody.velocity - Parent.RigidBody.velocity) / 10f));
			if (!Parent.RigidBody.isKinematic)
			{
				Parent.RigidBody.velocity += zero;
			}
		}
	}

	private void HandlePrimaryUse()
	{
		if (CursorManager.CursorThing is Human { IsDraggable: not false } human && (object)ActiveHand.Slot.Occupant == null)
		{
			if (KeyManager.GetMouseDown("Primary"))
			{
				human.DragInto(ActiveHand.Slot, human.Position - CursorManager.CursorHit.point);
			}
		}
		else if ((bool)ActiveHand.Slot.Occupant)
		{
			Item item = ActiveHand.Slot.Occupant as Item;
			ISuitReparier suitReparier = ActiveHand.Slot.Occupant as ISuitReparier;
			if ((!_primaryAvailable && suitReparier != null) || !item)
			{
				return;
			}
			if ((bool)CursorManager.CursorThing)
			{
				Attack attack = new Attack(ActiveHand.Slot, InactiveHand.Slot, CursorManager.CursorHit.point, CursorManager.CursorThing, 0f, CursorManager.Instance.CursorTargetCollider, (bool)(item as AuthoringTool) && Input.GetKey(KeyMap.QuantityModifier), (bool)(ActiveHand.Slot.Occupant as AuthoringTool) && Input.GetKey(KeyCode.F));
				Thing.DelayedActionInstance delayedActionInstance = ((CanAttackWith(CursorManager.Instance.CursorTargetCollider) || item is AuthoringTool) ? CursorManager.CursorThing.AttackWith(attack, doAction: false) : null);
				if (ActionCoroutine == null && delayedActionInstance != null && !delayedActionInstance.IsDisabled)
				{
					if (CursorManager.CursorThing.ReferenceId != _lastActionedID)
					{
						if (delayedActionInstance.Duration > 0f)
						{
							if ((bool)item)
							{
								ActionCoroutine = StartCoroutine(WaitUntilDone(UseItemComplete, delayedActionInstance, item.ConstructingSoundHash, item.FinishedConstructingSoundHash));
							}
							else
							{
								ActionCoroutine = StartCoroutine(WaitUntilDone(UseItemComplete, delayedActionInstance, 0, 0));
							}
						}
						else
						{
							LastCompletedRatio = 1f;
							UseItemComplete();
						}
						_lastActionedID = CursorManager.CursorThing.ReferenceId;
					}
					else
					{
						_lastActionedID = 0L;
					}
				}
				else if ((bool)item)
				{
					UseItemOnSelf(item);
				}
			}
			else if ((bool)item)
			{
				UseItemOnSelf(item);
			}
		}
		else
		{
			_primaryAvailable = true;
		}
	}

	private void SetMultiConstructorItemPlacement(MultiConstructor multiConstructorItem)
	{
		bool visible = !ConstructionPanel.IsVisible;
		CurrentMode = Mode.Placement;
		ConstructionPanel.Assign(multiConstructorItem);
		ConstructionPanel.SetVisible(visible);
	}

	private static void SetConstructorItemPlacement(Constructor constructorItem)
	{
		CurrentMode = Mode.Placement;
		UpdatePlacement(constructorItem);
	}

	private void UseItemOnSelf(Item item)
	{
		if (!item.AllowSelfUse)
		{
			return;
		}
		Vector3 targetLocation = (item.AllowForwardCursor ? CursorManager.CursorPositionForward : CursorManager.CursorPlanePosition);
		if (item.AttackWithEvent == AttackWithEvent.Server)
		{
			if (IsAuthoringMode)
			{
				OnServer.UseItemPrimaryAuthoring(Parent, ActiveHand.SlotId, targetLocation, Quaternion.identity, ParentBrain.ClientId, SpawnPrefab);
			}
			else
			{
				OnServer.UseItemPrimary(Parent, ActiveHand.SlotId, targetLocation, Quaternion.identity, ParentBrain.ClientId, SpawnPrefab);
			}
		}
		else
		{
			item.OnUsePrimary(targetLocation, Quaternion.identity, ParentBrain.ClientId, IsAuthoringMode);
		}
	}

	private void PrecisionPlacementMode()
	{
		float y = Input.mouseScrollDelta.y;
		_placementZoom += y;
		if (_placementZoom > 0f)
		{
			_placementZoom = 0f;
		}
		if (_placementZoom < -5f)
		{
			_placementZoom = -5f;
		}
		if (KeyManager.GetMouseDown("Secondary") || KeyManager.GetButtonDown(KeyMap.PrecisionPlace) || ActiveHand.Slot.Occupant == null)
		{
			CancelPlacement();
			return;
		}
		Vector3 center = ActiveHand.Slot.Occupant.Bounds.center;
		PrecisionPlaceCursor.transform.position = Parent.GetPrecisionPlacePoint(CameraController.CurrentCamera.transform.position, CameraController.CurrentCamera.transform.forward, 2f) - center + CameraController.CurrentCamera.transform.forward * (_placementZoom / 10f) + new Vector3(0f, ActiveHand.Slot.Occupant.Bounds.size.y / 2f, 0f);
		if (KeyManager.GetButtonUp(KeyMap.RotateLeft))
		{
			PrecisionPlaceCursor.transform.RotateAround(PrecisionPlaceCursor.transform.position, Vector3.up, 90f);
			UIAudioManager.Play(RotateBlueprintHash);
		}
		if (KeyManager.GetButtonUp(KeyMap.RotateDown))
		{
			PrecisionPlaceCursor.transform.RotateAround(PrecisionPlaceCursor.transform.position, Vector3.up, -90f);
			UIAudioManager.Play(RotateBlueprintHash);
		}
		if (KeyManager.GetButtonUp(KeyMap.RotateUp))
		{
			PrecisionPlaceCursor.transform.RotateAround(PrecisionPlaceCursor.transform.position, Vector3.right, 90f);
			UIAudioManager.Play(RotateBlueprintHash);
		}
		if (KeyManager.GetButtonUp(KeyMap.RotateDown))
		{
			PrecisionPlaceCursor.transform.RotateAround(PrecisionPlaceCursor.transform.position, Vector3.right, -90f);
			UIAudioManager.Play(RotateBlueprintHash);
		}
		if (KeyManager.GetMouseUp("Primary"))
		{
			PrecisionPlaceEvent?.Invoke();
			ActiveHand.Slot.Occupant.SetVisibility(isVisible: true);
			if (NetworkManager.IsClient)
			{
				NetworkClient.SendToServer(new MoveToWorldMessage
				{
					ChildId = ActiveHand.Slot.Occupant.ReferenceId,
					Force = 0f,
					IsPrecisionPlacement = true,
					Position = PrecisionPlaceCursor.transform.position,
					Rotation = PrecisionPlaceCursor.transform.rotation,
					Velocity = Vector3.zero,
					AngularVelocity = Vector3.zero
				});
			}
			else
			{
				OnServer.MoveToWorld(ActiveHand.Slot.Occupant, PrecisionPlaceCursor.transform.position, PrecisionPlaceCursor.transform.rotation, Vector3.zero, Vector3.zero);
			}
			CancelPlacement();
		}
	}

	private void PlacementMode()
	{
		tooltip = new PassiveTooltip(true);
		bool flag = (bool)ActiveHand.Slot.Get<Constructor>() || ConstructionPanel.IsVisible;
		if (KeyManager.GetButtonDown(KeyMap._Drop.Key) || (!flag && !IsAuthoringMode) || KeyManager.GetMouseDown("Secondary"))
		{
			CancelPlacement();
			return;
		}
		MultiConstructor multiConstructor = ActiveHand.Slot.Get<MultiConstructor>();
		if (!ConstructionCursor)
		{
			return;
		}
		Vector3 normal;
		Vector3 cameraForwardGrid = InputHelpers.GetCameraForwardGrid((ConstructionCursor.GridSize > 0.5f) ? 0.3f : 0.6f, ConstructionCursor.GetCursorOffset, out normal);
		ConstructionCursor.Position = ConstructionCursor.ThingTransformPosition;
		ConstructionCursor.Rotation = ConstructionCursor.ThingTransformRotation;
		CursorManager.SetSelectionVisibility(ShowUi);
		CursorManager.Instance.CursorSelectionHighlighter.transform.rotation = Quaternion.identity;
		_cursorPosition = Vector3.zero;
		Vector3 vector = ((ConstructionCursor is IChute) ? (normal * 0.1f) : Vector3.zero);
		_worldGrid = ConstructionCursor.GetWorldGrid(cameraForwardGrid + vector);
		_worldMasterGrid = ConstructionCursor.GridController.ClampWorld(cameraForwardGrid);
		bool flag2 = !multiConstructor || multiConstructor.CanBuild(ConstructionPanel.BuildIndex);
		CanMountResult canMountResult = default(CanMountResult);
		Span<Vector3> span2;
		switch (ConstructionCursor.PlacementType)
		{
		case PlacementSnap.Grid:
			if (!KeyManager.GetButton(KeyMap.QuantityModifier))
			{
				_usingAutoplace = false;
				_cursorPosition = _worldGrid;
				ConstructionCursor.ThingTransformPosition = _cursorPosition;
				if ((ConstructionCursor.RotationAxis & RotationAxis.Y) != RotationAxis.None)
				{
					if (KeyManager.GetButtonUp(KeyMap.RotateLeft))
					{
						ConstructionCursor.ThingTransform.Rotate(Vector3.up, 90f, Space.World);
						UIAudioManager.Play(RotateBlueprintHash);
					}
					if (KeyManager.GetButtonUp(KeyMap.RotateRight))
					{
						ConstructionCursor.ThingTransform.Rotate(Vector3.up, -90f, Space.World);
						UIAudioManager.Play(RotateBlueprintHash);
					}
				}
				if ((ConstructionCursor.RotationAxis & RotationAxis.X) != RotationAxis.None)
				{
					if (KeyManager.GetButtonUp(KeyMap.RotateUp))
					{
						ConstructionCursor.ThingTransform.Rotate(Vector3.right, (ConstructionCursor is IMounted) ? 180f : 90f, Space.World);
						UIAudioManager.Play(RotateBlueprintHash);
					}
					if (KeyManager.GetButtonUp(KeyMap.RotateDown))
					{
						ConstructionCursor.ThingTransform.Rotate(Vector3.right, (ConstructionCursor is IMounted) ? (-180f) : (-90f), Space.World);
						UIAudioManager.Play(RotateBlueprintHash);
					}
				}
				if ((ConstructionCursor.RotationAxis & RotationAxis.Z) != RotationAxis.None)
				{
					if (KeyManager.GetButtonUp(KeyMap.RotateRollLeft))
					{
						ConstructionCursor.ThingTransform.Rotate(Vector3.forward, 90f, Space.World);
						UIAudioManager.Play(RotateBlueprintHash);
					}
					if (KeyManager.GetButtonUp(KeyMap.RotateRollRight))
					{
						ConstructionCursor.ThingTransform.Rotate(Vector3.forward, -90f, Space.World);
						UIAudioManager.Play(RotateBlueprintHash);
					}
				}
				break;
			}
			_worldGrid = ConstructionCursor.GetWorldGrid(cameraForwardGrid);
			_worldMasterGrid = ConstructionCursor.GridController.ClampWorld(cameraForwardGrid);
			ConstructionCursor.ThingTransformPosition = _worldGrid;
			_cursorPosition = ConstructionCursor.ThingTransformPosition;
			if (newScrollData > 0f)
			{
				SmartRotate.GetNext(ConstructionCursor as ISmartRotatable, Quaternion.identity);
			}
			else if (newScrollData < 0f)
			{
				SmartRotate.GetPrevious(ConstructionCursor as ISmartRotatable, Quaternion.identity);
			}
			if (!_usingAutoplace)
			{
				if (KeyManager.GetButton(KeyMap.MouseInspect))
				{
					SmartRotate.GetPrevious(ConstructionCursor as ISmartRotatable, Quaternion.identity);
				}
				else
				{
					SmartRotate.GetNext(ConstructionCursor as ISmartRotatable, Quaternion.identity);
				}
				_usingAutoplace = true;
			}
			break;
		case PlacementSnap.FaceMount:
		{
			if (CursorManager.CursorThing is Structure structure)
			{
				if (structure.AllowMounting)
				{
					Vector3 thingTransformPosition = structure.ThingTransformPosition;
					Vector3 target = Vector3.zero;
					switch (structure.GetPlacementType())
					{
					case PlacementSnap.Grid:
					{
						Span<Vector3> span5 = stackalloc Vector3[12];
						int count3 = 0;
						ConstructionCursor.GridController.PopulateWorldGridFaces(span5, ref count3, thingTransformPosition);
						span2 = span5;
						target = RocketGrid.GetClosest(span2.Slice(0, count3), cameraForwardGrid) - thingTransformPosition;
						break;
					}
					case PlacementSnap.Face:
					case PlacementSnap.FaceMount:
					{
						Vector3 forward = structure.ThingTransform.forward;
						Vector3 a = thingTransformPosition + forward;
						Vector3 a2 = thingTransformPosition - forward;
						target = ((!(Vector3.Distance(a, Parent.ThingTransform.position + Vector3.up) < Vector3.Distance(a2, Parent.ThingTransform.position + Vector3.up))) ? (-forward) : forward);
						break;
					}
					}
					_cursorPosition = _worldGrid;
					ConstructionCursor.ThingTransformPosition = _cursorPosition;
					ConstructionCursor.ThingTransform.RotateOnto(ConstructionCursor.ThingTransform.forward, target);
					ConstructionCursor.ThingTransform.RotateOnto(ConstructionCursor.ThingTransform.up, ConstructionCursor.ThingTransform.up.FindClosestLocalAxis(structure.ThingTransform), ConstructionCursor.ThingTransform.forward);
					if (!KeyManager.GetButton(KeyMap.QuantityModifier))
					{
						_usingAutoplace = false;
						if (KeyManager.GetButtonUp(KeyMap.RotateRollRight))
						{
							ConstructionCursor.ThingTransform.Rotate(Vector3.forward, 90f, Space.Self);
							UIAudioManager.Play(RotateBlueprintHash);
						}
						if (KeyManager.GetButtonUp(KeyMap.RotateRollLeft))
						{
							ConstructionCursor.ThingTransform.Rotate(Vector3.forward, -90f, Space.Self);
							UIAudioManager.Play(RotateBlueprintHash);
						}
					}
					else
					{
						if (newScrollData > 0f)
						{
							SmartRotate.GetNext(ConstructionCursor as ISmartRotatable);
						}
						else if (newScrollData < 0f)
						{
							SmartRotate.GetPrevious(ConstructionCursor as ISmartRotatable);
						}
						if (!_usingAutoplace)
						{
							SmartRotate.GetNext(ConstructionCursor as ISmartRotatable);
							_usingAutoplace = true;
						}
					}
					canMountResult = ConstructionCursor.CanMountOnWall();
					bool flag3 = canMountResult;
					flag2 = flag2 && flag3;
					break;
				}
				canMountResult.result = WallMountResult.InvalidNotMountable;
				canMountResult.support = structure;
				canMountResult.offending = structure;
			}
			else
			{
				canMountResult.result = WallMountResult.InvalidMissingSupport;
			}
			_cursorPosition = cameraForwardGrid;
			_worldGrid = cameraForwardGrid;
			ConstructionCursor.ThingTransformPosition = cameraForwardGrid;
			Vector3 vector2 = ((!(ParentHuman != null)) ? (Parent.ThingTransformPosition - ConstructionCursor.ThingTransformPosition).normalized : ((!ParentHuman.HasAuthority) ? (ParentHuman.HeadBone.position - ConstructionCursor.ThingTransformPosition).normalized : (-CameraController.Instance.MainCameraTransform.forward)));
			Vector3 vector3 = Vector3.Cross(Vector3.up, vector2);
			if (RocketMath.Approximately(vector3, Vector3.zero))
			{
				vector3 = Vector3.Cross(Vector3.right, vector2);
			}
			Vector3 vector4 = ((Mathf.Abs(Vector3.Dot(ConstructionCursor.ThingTransform.right, vector3)) > Mathf.Abs(Vector3.Dot(ConstructionCursor.ThingTransform.up, vector3))) ? ConstructionCursor.ThingTransform.right : ConstructionCursor.ThingTransform.up);
			ConstructionCursor.ThingTransform.RotateOnto(vector4, vector4.FindClosestLocalAxis(vector3));
			ConstructionCursor.ThingTransform.RotateOnto(ConstructionCursor.ThingTransform.forward, vector2, vector3);
			flag2 = false;
			if (KeyManager.GetButtonUp(KeyMap.RotateRollRight))
			{
				ConstructionCursor.ThingTransform.Rotate(Vector3.forward, 90f, Space.Self);
				UIAudioManager.Play(RotateBlueprintHash);
			}
			if (KeyManager.GetButtonUp(KeyMap.RotateRollLeft))
			{
				ConstructionCursor.ThingTransform.Rotate(Vector3.forward, -90f, Space.Self);
				UIAudioManager.Play(RotateBlueprintHash);
			}
			break;
		}
		case PlacementSnap.Face:
			if (!KeyManager.GetButton(KeyMap.QuantityModifier))
			{
				_usingAutoplace = false;
				ConstructionCursor.ThingTransform.rotation = CurrentRotation;
				Span<Vector3> span = stackalloc Vector3[12];
				int count = 0;
				ConstructionCursor.GridController.PopulateWorldGridFaces(span, ref count, _worldGrid);
				span2 = span;
				Span<Vector3> span3 = span2.Slice(0, count);
				ConstructionCursor.ThingTransformPosition = _worldGrid - ConstructionCursor.ThingTransform.forward * ConstructionCursor.GridSize / 2f;
				if ((ConstructionCursor.RotationAxis & RotationAxis.X) != RotationAxis.None)
				{
					if (CurrentFace != RocketGrid.FaceInt.Up && KeyManager.GetButtonUp(KeyMap.RotateUp))
					{
						if (RocketGrid.FaceInt.IsHorizontalFace(CurrentFace))
						{
							Vector3 axis = Vector3.Cross(Vector3.up, ConstructionCursor.ThingTransform.forward);
							ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, axis, 90f);
							CurrentFace = RocketGrid.FaceInt.Up;
						}
						else
						{
							Vector3 rhs = _worldMasterGrid - span3[_lastHorizontalFace];
							Vector3 axis2 = Vector3.Cross(Vector3.up, rhs);
							ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, axis2, 90f);
							CurrentFace = _lastHorizontalFace;
						}
						UIAudioManager.Play(RotateBlueprintHash);
					}
					if (CurrentFace != RocketGrid.FaceInt.Down && KeyManager.GetButtonUp(KeyMap.RotateDown))
					{
						if (RocketGrid.FaceInt.IsHorizontalFace(CurrentFace))
						{
							Vector3 axis3 = Vector3.Cross(Vector3.up, ConstructionCursor.ThingTransform.forward);
							ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, axis3, -90f);
							CurrentFace = RocketGrid.FaceInt.Down;
						}
						else
						{
							Vector3 rhs2 = _worldMasterGrid - span3[_lastHorizontalFace];
							Vector3 axis4 = Vector3.Cross(Vector3.up, rhs2);
							ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, axis4, -90f);
							CurrentFace = _lastHorizontalFace;
						}
						UIAudioManager.Play(RotateBlueprintHash);
					}
				}
				if ((ConstructionCursor.RotationAxis & RotationAxis.Y) != RotationAxis.None && RocketGrid.FaceInt.IsHorizontalFace(CurrentFace))
				{
					if (KeyManager.GetButtonUp(KeyMap.RotateRight))
					{
						UIAudioManager.Play(RotateBlueprintHash);
						ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, Vector3.up, 90f);
						_lastHorizontalFace = (CurrentFace = RocketGrid.FaceInt.GetNextClockwiseHorizontalFace(CurrentFace));
					}
					if (KeyManager.GetButtonUp(KeyMap.RotateLeft))
					{
						UIAudioManager.Play(RotateBlueprintHash);
						ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, Vector3.up, -90f);
						_lastHorizontalFace = (CurrentFace = RocketGrid.FaceInt.GetNextAnticlockwiseHorizontalFace(CurrentFace));
					}
				}
				if ((ConstructionCursor.RotationAxis & RotationAxis.Z) != RotationAxis.None)
				{
					if (!RocketGrid.FaceInt.IsHorizontalFace(CurrentFace))
					{
						if (KeyManager.GetButtonUp(KeyMap.RotateRight))
						{
							ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, Vector3.up, 90f);
							UIAudioManager.Play(RotateBlueprintHash);
						}
						if (KeyManager.GetButtonUp(KeyMap.RotateLeft))
						{
							ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, Vector3.up, -90f);
							UIAudioManager.Play(RotateBlueprintHash);
						}
					}
					if (KeyManager.GetButtonUp(KeyMap.RotateRollRight))
					{
						ConstructionCursor.ThingTransform.Rotate(Vector3.forward, 90f, Space.Self);
						UIAudioManager.Play(RotateBlueprintHash);
					}
					if (KeyManager.GetButtonUp(KeyMap.RotateRollLeft))
					{
						ConstructionCursor.ThingTransform.Rotate(Vector3.forward, -90f, Space.Self);
						UIAudioManager.Play(RotateBlueprintHash);
					}
				}
				_cursorPosition = span3[CurrentFace];
			}
			else
			{
				_worldGrid = ConstructionCursor.GetWorldGrid(cameraForwardGrid);
				_worldMasterGrid = ConstructionCursor.GridController.ClampWorld(cameraForwardGrid);
				ConstructionCursor.ThingTransformPosition = _worldGrid - ConstructionCursor.ThingTransform.forward * ConstructionCursor.GridSize / 2f;
				if (newScrollData > 0f)
				{
					SmartRotate.GetNext(ConstructionCursor as ISmartRotatable, Quaternion.identity, _worldGrid);
				}
				else if (newScrollData < 0f)
				{
					SmartRotate.GetPrevious(ConstructionCursor as ISmartRotatable, Quaternion.identity, _worldGrid);
				}
				if (!_usingAutoplace)
				{
					if (KeyManager.GetButton(KeyMap.MouseInspect))
					{
						SmartRotate.GetPrevious(ConstructionCursor as ISmartRotatable, Quaternion.identity, _worldGrid);
					}
					else
					{
						SmartRotate.GetNext(ConstructionCursor as ISmartRotatable, Quaternion.identity, _worldGrid);
					}
					_usingAutoplace = true;
				}
				CurrentFace = RocketGrid.FaceInt.FaceIntFromDir(RocketGrid.GetFaceDir(ConstructionCursor.ThingTransformPosition, _worldGrid, Quaternion.identity));
				Span<Vector3> span4 = stackalloc Vector3[12];
				int count2 = 0;
				ConstructionCursor.GridController.PopulateWorldGridFaces(span4, ref count2, _worldGrid);
				span2 = span4;
				_cursorPosition = span2.Slice(0, count2)[CurrentFace];
			}
			CurrentRotation = ConstructionCursor.ThingTransform.rotation;
			break;
		}
		SmallGrid smallGrid = ConstructionCursor as SmallGrid;
		switch (ConstructionCursor.SelectionDisplay)
		{
		case SelectionHighlightMethod.Grid:
		{
			Bounds bounds = ((!smallGrid || smallGrid.DualRegister || !(Math.Abs(smallGrid.GridSize - SmallGrid.SmallGridSize) < 0.1f)) ? ConstructionCursor.GridBounds.BoundsBig : ConstructionCursor.GridBounds.BoundsSmall);
			CursorManager.CursorSelectionTransform.localScale = bounds.size + Vector3.one * 0.05f;
			CursorManager.CursorSelectionTransform.position = _worldGrid + ConstructionCursor.ThingTransform.rotation * bounds.center;
			CursorManager.CursorSelectionTransform.rotation = ConstructionCursor.ThingTransform.rotation;
			break;
		}
		case SelectionHighlightMethod.Bounds:
			CursorManager.CursorSelectionTransform.localScale = ConstructionCursor.Bounds.size + Vector3.one * 0.05f;
			CursorManager.CursorSelectionTransform.rotation = ConstructionCursor.ThingTransform.rotation;
			CursorManager.CursorSelectionTransform.position = ConstructionCursor.ThingTransformPosition + ConstructionCursor.ThingTransform.rotation * ConstructionCursor.Bounds.center;
			break;
		}
		CanConstructInfo canConstructInfo = ConstructionCursor.CanConstruct();
		if (!canConstructInfo.CanConstruct)
		{
			if (!string.IsNullOrEmpty(tooltip.State))
			{
				tooltip.State += "\n";
			}
			ref string state = ref tooltip.State;
			state = state + "<color=red>" + canConstructInfo.ErrorMessage + "</color>";
		}
		flag2 = flag2 && canConstructInfo.CanConstruct;
		if (!IsAuthoringMode && ConstructionCursor is IGridMergeable gridMergeable)
		{
			Item inactiveHandItem = Parent.Slots[InactiveHand.SlotId].Occupant as Item;
			CanConstructInfo canConstructInfo2 = gridMergeable.CanReplace(multiConstructor, inactiveHandItem);
			if (flag2 && !canConstructInfo2.CanConstruct)
			{
				ref string state2 = ref tooltip.State;
				state2 = state2 + "<color=red>" + canConstructInfo2.ErrorMessage + "</color>";
			}
			flag2 &= canConstructInfo2.CanConstruct;
		}
		if (ConstructionCursor is IMounted mounted)
		{
			mounted.Mount(ConstructionCursor.ThingTransform.position.ToGrid(ConstructionCursor.GridSize, ConstructionCursor.GridOffset));
		}
		if (flag2 && ConstructionCursor.StructureCollisionType == CollisionType.BlockGrid)
		{
			flag2 = Vector3.SqrMagnitude(_worldGrid - Parent.RigidBody.worldCenterOfMass.GridCenter(ConstructionCursor.GridSize, ConstructionCursor.GridOffset)) > ConstructionCursor.GridSize / 2f;
			if (flag2)
			{
				foreach (DynamicThing dynamicObject in DynamicThing.DynamicObjects)
				{
					if (dynamicObject.ParentSlot == null && ConstructionCursor.BoundsIntersectWith(dynamicObject))
					{
						tooltip.State = GameStrings.ConstructionBlockedByDynamic.AsString(dynamicObject.ToTooltip());
						flag2 = false;
						break;
					}
				}
			}
		}
		if (KeyManager.GetMouseDown("Primary") && flag2)
		{
			_usePrimaryPosition = _cursorPosition;
			_usePrimaryRotation = ConstructionCursor.ThingTransform.rotation;
			if (ConstructionCursor.BuildPlacementTime > 0f)
			{
				float num = 1f;
				if (ParentHuman.Suit == null)
				{
					num += 0.2f;
				}
				num = Mathf.Clamp(num, 0.2f, 5f);
				ActionCoroutine = StartCoroutine(WaitUntilDone(UsePrimaryComplete, ConstructionCursor.BuildPlacementTime / num, ConstructionCursor));
			}
			else
			{
				UsePrimaryComplete();
			}
			return;
		}
		Color color = (flag2 ? Color.green : Color.red);
		if (ConstructionCursor is StructureFuselage structureFuselage)
		{
			if (structureFuselage.CanMerge())
			{
				color = Color.yellow;
			}
			if (!string.IsNullOrEmpty(tooltip.ConstructString))
			{
				tooltip.ConstructString += "\n";
			}
			string arg = Prefab.Find(Defines.Prefabs.ItemAngleGrinder).ToTooltip();
			tooltip.ConstructString += GameStrings.CanReplaceFuselage.AsString(arg);
		}
		if ((bool)smallGrid)
		{
			Span<ConnectionRef> span6 = stackalloc ConnectionRef[64];
			int connCount = 0;
			ConstructionCursor.WillJoinNetwork(span6, ref connCount);
			Span<ConnectionRef> span7 = span6;
			Span<ConnectionRef> span8 = span7.Slice(0, connCount);
			foreach (Connection openEnd in smallGrid.OpenEnds)
			{
				openEnd.Initialize();
				if (flag2)
				{
					openEnd.HelperRenderer.material.color = (span8.Contains(openEnd) ? Color.yellow.SetAlpha(CursorAlphaConstructionHelper) : Color.green.SetAlpha(CursorAlphaConstructionHelper));
				}
				else
				{
					openEnd.HelperRenderer.material.color = Color.red.SetAlpha(CursorAlphaConstructionHelper);
				}
			}
			color = ((flag2 && connCount > 0) ? Color.yellow : color);
		}
		tooltip.Action = ActionStrings.Build;
		tooltip.Title = ConstructionCursor.DisplayName;
		tooltip.color = color;
		tooltip.Slider = -1f;
		if ((bool)multiConstructor && !multiConstructor.CanBuild(ConstructionPanel.BuildIndex))
		{
			if (!string.IsNullOrEmpty(tooltip.ConstructString))
			{
				tooltip.ConstructString += "\n";
			}
			tooltip.ConstructString += string.Format(InterfaceStrings.NeedMoreKit, multiConstructor.ToTooltip());
		}
		if (Settings.CurrentData.ExtendedTooltips)
		{
			if (flag || (bool)multiConstructor)
			{
				tooltip.ShowScroll = !KeyManager.GetButton(KeyMap.QuantityModifier) && multiConstructor != null && multiConstructor.Constructables.Count > 1;
				tooltip.ShowRotate = !KeyManager.GetButton(KeyMap.QuantityModifier);
				tooltip.ShowConstructionRotate = KeyManager.GetButton(KeyMap.QuantityModifier);
				if (multiConstructor != null)
				{
					tooltip.BuildStateIndexMessage = string.Format(InterfaceStrings.TooltipNumberofBuildState, StringManager.Get(multiConstructor.LastSelectedIndex + 1), StringManager.Get(multiConstructor.Constructables.Count));
				}
			}
			if (ConstructionCursor.BuildStates.Count > 1)
			{
				BuildState buildState = ConstructionCursor.BuildStates[1];
				if ((bool)buildState.Tool.ToolEntry && (bool)buildState.Tool.ToolEntry2)
				{
					if (!string.IsNullOrEmpty(tooltip.ConstructString))
					{
						tooltip.ConstructString += "\n";
					}
					tooltip.ConstructString += string.Format(InterfaceStrings.TooltipUpgrade2, buildState.Tool.ToolEntry.ToTooltip(), buildState.Tool.ToolEntry2.ToTooltip());
				}
				else if ((bool)buildState.Tool.ToolEntry)
				{
					if (!string.IsNullOrEmpty(tooltip.ConstructString))
					{
						tooltip.ConstructString += "\n";
					}
					tooltip.ConstructString += string.Format(InterfaceStrings.TooltipUpgrade, buildState.Tool.ToolEntry.ToTooltip());
				}
			}
			switch (ConstructionCursor.PlacementType)
			{
			case PlacementSnap.Grid:
				if (!string.IsNullOrEmpty(tooltip.PlacementString))
				{
					tooltip.PlacementString += "\n";
				}
				tooltip.PlacementString += InterfaceStrings.TooltipPlacementSnapGrid;
				break;
			case PlacementSnap.Face:
				if (!string.IsNullOrEmpty(tooltip.PlacementString))
				{
					tooltip.PlacementString += "\n";
				}
				tooltip.PlacementString += InterfaceStrings.TooltipPlacementSnapFace;
				break;
			case PlacementSnap.FaceMount:
				if (!string.IsNullOrEmpty(tooltip.PlacementString))
				{
					tooltip.PlacementString += "\n";
				}
				tooltip.PlacementString += InterfaceStrings.TooltipPlacementSnapFaceMount;
				break;
			}
			if (!_usingAutoplace)
			{
				tooltip.ShowRotate = true;
			}
			else
			{
				if (!string.IsNullOrEmpty(tooltip.PlacementString))
				{
					tooltip.PlacementString += "\n";
				}
				tooltip.ShowScroll = false;
			}
			if (ConstructionCursor.AllowMounting)
			{
				if (!string.IsNullOrEmpty(tooltip.State))
				{
					tooltip.State += "\n";
				}
				tooltip.State += InterfaceStrings.TooltipAllowsMounting;
			}
			if (ConstructionCursor.RotationAxis != RotationAxis.None && !_usingAutoplace)
			{
				if ((ConstructionCursor.RotationAxis & RotationAxis.Y) != RotationAxis.None)
				{
					if (!string.IsNullOrEmpty(tooltip.PlacementString))
					{
						tooltip.PlacementString += "\n";
					}
					tooltip.PlacementString += InterfaceStrings.TooltipRotateLeftRight;
				}
				if ((ConstructionCursor.RotationAxis & RotationAxis.X) != RotationAxis.None)
				{
					if (!string.IsNullOrEmpty(tooltip.PlacementString))
					{
						tooltip.PlacementString += "\n";
					}
					tooltip.PlacementString += InterfaceStrings.TooltipRotateUpDown;
				}
				if ((ConstructionCursor.RotationAxis & RotationAxis.Z) != RotationAxis.None)
				{
					if (!string.IsNullOrEmpty(tooltip.PlacementString))
					{
						tooltip.PlacementString += "\n";
					}
					tooltip.PlacementString += InterfaceStrings.TooltipRollLeftRight;
				}
			}
		}
		color.a = CursorAlphaConstructionMesh;
		if ((bool)ConstructionCursor.Wireframe)
		{
			ConstructionCursor.Wireframe.BlueprintRenderer.material.color = color;
		}
		else
		{
			foreach (ThingRenderer renderer in ConstructionCursor.Renderers)
			{
				if (renderer.HasRenderer())
				{
					renderer.SetColor(color);
				}
			}
		}
		CursorManager.SetSelectionColor(color.SetAlpha(CursorAlphaConstructionGrid));
		TooltipRef.HandleToolTipDisplay(tooltip);
	}

	private void MineAsteroidComplete(float mineAmount)
	{
		if (!(CursorManager.GetCurrentVoxelWorld().y <= 0f) && CursorManager.CursorTerrain.IsValid && (object)ActiveHand.Slot.Occupant != null && ActiveHand.Slot.Occupant is IMiningTool miningTool)
		{
			miningTool.OnMinedVoxel(mineAmount);
			if (GameManager.RunSimulation)
			{
				OnServer.MineAsteroid(Parent.ReferenceId, CursorManager.GetCurrentVoxelWorld(), mineAmount, ActiveHand.Slot.Occupant.ReferenceId);
				return;
			}
			NetworkClient.SendToServer(new MineAsteroidMessage
			{
				ParentBrainId = Parent.ReferenceId,
				WorldVoxelPosition = CursorManager.GetCurrentVoxelWorld(),
				Amount = mineAmount,
				ToolId = ActiveHand.Slot.Occupant.ReferenceId
			});
		}
	}

	private void UsePrimaryComplete()
	{
		if (GameManager.RunSimulation)
		{
			if (ConstructionPanel.IsVisible)
			{
				OnServer.UseMultiConstructor(Parent, ActiveHand.SlotId, InactiveHand.SlotId, _usePrimaryPosition, _usePrimaryRotation, ConstructionPanel.BuildIndex, IsAuthoringMode, ParentBrain.ClientId, SpawnPrefab);
			}
			else
			{
				OnServer.UseItemPrimary(Parent, ActiveHand.SlotId, _usePrimaryPosition, _usePrimaryRotation, ParentBrain.ClientId, SpawnPrefab);
			}
			return;
		}
		NetworkClient.SendToServer(new CreateStructureMessage
		{
			ConstructorId = (ActiveHand.Slot.Occupant?.ReferenceId ?? 0),
			OffhandOccupantReferenceId = (InactiveHand.Slot.Occupant?.ReferenceId ?? 0),
			LocalPosition = _usePrimaryPosition.ToGridPosition(),
			Rotation = _usePrimaryRotation,
			CreatorSteamId = (ulong)ParentBrain.ReferenceId,
			OptionIndex = ConstructionPanel.BuildIndex,
			PrefabHash = (ActiveHand.Slot.Occupant?.PrefabHash ?? 0),
			AuthoringMode = IsAuthoringMode
		});
	}

	private void UseSecondaryComplete()
	{
		if (GameManager.RunSimulation)
		{
			OnServer.UseItemSecondary(Parent, ActiveHand.SlotId, LastCompletedRatio);
		}
		else
		{
			NetworkClient.UseItemSecondary(Parent, ActiveHand.SlotId, LastCompletedRatio);
		}
	}

	private void UseItemComplete()
	{
		if (!CursorManager.CursorThing || (LastCompletedRatio < 1f && !CursorManager.CursorThing.AttackWithAllowIncomplete))
		{
			return;
		}
		Attack attack = new Attack(ActiveHand.Slot, InactiveHand.Slot, CursorManager.CursorHit.point, CursorManager.CursorThing, 0f, CursorManager.Instance.CursorTargetCollider, (bool)(ActiveHand.Slot.Occupant as AuthoringTool) && Input.GetKey(KeyMap.QuantityModifier), (bool)(ActiveHand.Slot.Occupant as AuthoringTool) && Input.GetKey(KeyCode.F));
		if ((object)ActiveHand.Slot.Occupant != null)
		{
			CursorManager.CursorThing.AttackWithCompleteLocal(attack);
			if (ActiveHand.Slot.Occupant.AttackWithEvent == AttackWithEvent.Server)
			{
				OnServer.AttackWith(Parent, (byte)ActiveHand.SlotId, (byte)InactiveHand.SlotId, CursorManager.CursorThing.netId, CursorManager.CursorHit.point, LastCompletedRatio, (bool)(ActiveHand.Slot.Occupant as AuthoringTool) && Input.GetKey(KeyMap.QuantityModifier), (bool)(ActiveHand.Slot.Occupant as AuthoringTool) && Input.GetKey(KeyCode.F));
				return;
			}
			_lastActionedID = 0L;
			CursorManager.CursorThing.AttackWith(attack);
		}
	}

	private void PlayWaitUntilDoneSound(int actionSoundHash, bool playUiSounds)
	{
		if (!GameManager.IsBatchMode)
		{
			if (playUiSounds)
			{
				UIAudioManager.Play(_uiActionStartHash);
			}
			if (ActionUISoundAudio != null)
			{
				ActionUISoundAudio.Stop();
			}
			if (playUiSounds)
			{
				ActionUISoundAudio = UIAudioManager.Play(_uiActionActiveHash);
			}
			if (ActionSoundAudio != null)
			{
				ActionSoundAudio.Stop();
			}
			if (actionSoundHash != 0)
			{
				ActionSoundAudio = Singleton<AudioManager>.Instance.PlayAudioClipsData(ParentHuman, actionSoundHash, Vector3.zero);
			}
		}
	}

	private void StopWaitUntilDoneSound(bool success, bool playUiSounds)
	{
		if (!GameManager.IsBatchMode)
		{
			if (ActionSoundAudio != null)
			{
				ActionSoundAudio.Stop();
				ActionSoundAudio = null;
			}
			if (ActionUISoundAudio != null)
			{
				ActionUISoundAudio.Stop();
				ActionUISoundAudio = null;
			}
			if (success && playUiSounds)
			{
				UIAudioManager.Play(_uiActionFinishedHash);
			}
		}
	}

	public IEnumerator WaitUntilDone(DelegateEvent onFinished, float timeToWait, string actionName, Vector3 voxelWorldPositon, CursorTerrain cursorTerrain, byte minableIndex)
	{
		OnComplete = onFinished;
		float startTime = Time.time;
		CursorTerrain initialTarget = CursorManager.CursorTerrain;
		_uiProgressBarPanel.SetActionName("<b>" + actionName + "</b>\nAsteroid");
		if (actionName != "")
		{
			_uiProgressBarPanel.SetActive(active: true);
		}
		while (Time.time - startTime < timeToWait)
		{
			if (minableIndex < byte.MaxValue)
			{
				cursorTerrain.CursorVein?.Shake(minableIndex, (Time.time - startTime) / timeToWait);
			}
			bool button = KeyManager.GetButton(KeyMap.SwapHands);
			bool flag = ActiveHandSlot.Occupant == null && !button;
			bool flag2 = !initialTarget.Equals(CursorManager.CursorTerrain);
			Vector3 vector = voxelWorldPositon - CursorManager.GetCurrentVoxelWorld();
			if (!KeyManager.GetMouse("Primary") || flag || button || flag2 || vector.sqrMagnitude > 0.1f || VoxelTerrain.GetDensityWorldSpace(voxelWorldPositon) <= 0f)
			{
				_uiProgressBarPanel.SetActive(active: false);
				_parentAnimator.SetBool(MovementController.CastingHash, value: false);
				ActionCoroutine = null;
				VoxelTerrain.ResetDummyObjects();
				yield break;
			}
			_parentAnimator.SetBool(MovementController.CastingHash, ActiveHandSlot.Occupant.CastAnimation != CastingAnimation.None);
			_uiProgressBarPanel.SetProgress((Time.time - startTime) / timeToWait);
			yield return null;
		}
		_uiProgressBarPanel.SetActive(active: false);
		OnComplete();
		ActionCoroutine = null;
		_parentAnimator.SetBool(MovementController.CastingHash, value: false);
		VoxelTerrain.ResetDummyObjects();
	}

	private IEnumerator WaitUntilDone(Interactable interactable, Thing.DelayedActionInstance interactionInstance, Interaction interaction)
	{
		float timeToWait = interactionInstance.Duration;
		string actionMessage = interactionInstance.ActionMessage;
		int actionSoundHash = interactionInstance.ActionSoundHash;
		float startTime = Time.time;
		Thing initialTarget = CursorManager.CursorThing;
		Collider initialCollider = CursorManager.Instance.CursorTargetCollider;
		_uiProgressBarPanel.SetActionName(actionMessage);
		_uiProgressBarPanel.SetItemName(CursorManager.CursorThing.DisplayName);
		_uiProgressBarPanel.SetActive(active: true);
		if (ActiveHandSlot.Occupant != null)
		{
			ActiveHandSlot.Occupant.OnPrimaryUseStart();
		}
		PlayWaitUntilDoneSound(actionSoundHash, playUiSounds: false);
		while (Time.time - startTime < timeToWait)
		{
			if (!KeyManager.GetMouse("Primary") || initialCollider != CursorManager.Instance.CursorTargetCollider || initialTarget.ThingTransform == null)
			{
				_uiProgressBarPanel.SetActive(active: false);
				_parentAnimator.SetBool(MovementController.CastingHash, value: false);
				ActionCoroutine = null;
				StopWaitUntilDoneSound(success: false, playUiSounds: false);
				if (ActiveHandSlot.Occupant != null)
				{
					ActiveHandSlot.Occupant.OnPrimaryUseEnd();
				}
				yield break;
			}
			_parentAnimator.SetBool(MovementController.CastingHash, (bool)ActiveHandSlot.Occupant && ActiveHandSlot.Occupant.CastAnimation != CastingAnimation.None);
			_uiProgressBarPanel.SetProgress((Time.time - startTime) / timeToWait);
			yield return null;
		}
		_uiProgressBarPanel.SetActive(active: false);
		ActionCoroutine = null;
		_parentAnimator.SetBool(MovementController.CastingHash, value: false);
		StopWaitUntilDoneSound(success: true, playUiSounds: false);
		if (interactionInstance.ActionCompleteSoundHash != 0)
		{
			ParentHuman?.PlayNetworkSound(interactionInstance.ActionCompleteSoundHash);
		}
		if (ActiveHandSlot.Occupant != null)
		{
			ActiveHandSlot.Occupant.OnPrimaryUseEnd();
		}
		if (GameManager.RunSimulation)
		{
			OnServer.InteractWith(interactable, interaction);
		}
		else if (NetworkManager.IsClient)
		{
			NetworkClient.InteractWith(interactable, interaction);
		}
	}

	private IEnumerator WaitUntilDone(DelegateEvent onFinished, float timeToWait, Structure structure)
	{
		OnComplete = onFinished;
		if (!IsAuthoringMode)
		{
			float startTime = Time.time;
			int initialHand = ActiveHand.SlotId;
			DynamicThing initialTarget = ActiveHand.Slot.Occupant;
			Grid3 initialGrid = ConstructionCursor.GetLocalGrid();
			PlayWaitUntilDoneSound(Animator.StringToHash(initialTarget.UsingSound), initialTarget.UseDefaultUiUsingSounds());
			_uiProgressBarPanel.SetActionName(ActionStrings.Build);
			_uiProgressBarPanel.SetItemName(structure.DisplayName);
			_uiProgressBarPanel.SetActive(active: true);
			ActiveHand.Slot.Occupant.OnPrimaryUseStart();
			while (Time.time - startTime < timeToWait)
			{
				if (ConstructionCursor == null)
				{
					_uiProgressBarPanel.SetActive(active: false);
					_parentAnimator.SetBool(MovementController.CastingHash, value: false);
					ActionCoroutine = null;
					StopWaitUntilDoneSound(success: false, playUiSounds: false);
					yield break;
				}
				Grid3 localGrid = ConstructionCursor.GetLocalGrid();
				if (!KeyManager.GetMouse("Primary") || KeyManager.GetMouse("Secondary") || initialHand != ActiveHand.SlotId || initialTarget != ActiveHand.Slot.Occupant || initialTarget.ThingTransform == null || initialGrid != localGrid)
				{
					_uiProgressBarPanel.SetActive(active: false);
					_parentAnimator.SetBool(MovementController.CastingHash, value: false);
					ActionCoroutine = null;
					if ((bool)ActiveHand.Slot.Occupant)
					{
						ActiveHand.Slot.Occupant.OnPrimaryUseEnd();
					}
					StopWaitUntilDoneSound(success: false, playUiSounds: false);
					yield break;
				}
				_parentAnimator.SetBool(MovementController.CastingHash, ActiveHandSlot.Occupant.CastAnimation != CastingAnimation.None);
				_uiProgressBarPanel.SetProgress((Time.time - startTime) / timeToWait);
				yield return null;
			}
			StopWaitUntilDoneSound(success: true, KeyManager.GetMouse("Primary"));
			if (!GameManager.IsBatchMode && initialTarget != null && !string.IsNullOrEmpty(initialTarget.UseCompleteSound))
			{
				ParentHuman.PlayNetworkSound(Animator.StringToHash(initialTarget.UseCompleteSound));
			}
		}
		_uiProgressBarPanel.SetActive(active: false);
		OnComplete();
		ActionCoroutine = null;
		_parentAnimator.SetBool(MovementController.CastingHash, value: false);
		ActiveHand.Slot.Occupant.OnPrimaryUseEnd();
	}

	private IEnumerator WaitUntilDone(DelegateEvent onFinished, Thing.DelayedActionInstance attack, int actionSoundHash, int actionCompleteSoundHash)
	{
		OnComplete = onFinished;
		float startTime = Time.time;
		Thing initialTarget = CursorManager.CursorThing;
		int initialHand = ActiveHand.SlotId;
		string itemName = ((attack.OverrideTitle != string.Empty) ? attack.OverrideTitle : CursorManager.CursorThing.DisplayName);
		_uiProgressBarPanel.SetActionName(attack.ActionMessage);
		_uiProgressBarPanel.SetItemName(itemName);
		_uiProgressBarPanel.SetActive(active: true);
		DynamicThing startObject = ActiveHand.Slot.Occupant;
		LastCompletedRatio = 0f;
		_primaryAvailable = false;
		bool casting = false;
		if ((object)startObject != null)
		{
			casting = startObject.CastAnimation != CastingAnimation.None;
			startObject.OnPrimaryUseStart();
			PlayWaitUntilDoneSound(actionSoundHash, startObject.UseDefaultUiUsingSounds());
		}
		while (Time.time - startTime < attack.Duration)
		{
			float num = Time.time - startTime;
			if (!KeyManager.GetMouse("Primary") || initialHand != ActiveHand.SlotId || initialTarget != CursorManager.CursorThing || initialTarget.ThingTransform == null || ((object)startObject != null && ActiveHandSlot.Occupant != startObject))
			{
				_primaryAvailable = true;
				_uiProgressBarPanel.SetActive(active: false);
				_parentAnimator.SetBool(MovementController.CastingHash, value: false);
				ActionCoroutine = null;
				LastCompletedRatio = num / attack.Duration;
				if ((object)startObject != null)
				{
					StopWaitUntilDoneSound(success: false, startObject.UseDefaultUiUsingSounds());
				}
				if (onFinished != null)
				{
					OnComplete();
				}
				if ((bool)startObject)
				{
					startObject.OnPrimaryUseEnd();
				}
				yield break;
			}
			_parentAnimator.SetBool(MovementController.CastingHash, casting);
			_uiProgressBarPanel.SetProgress((Time.time - startTime) / attack.Duration);
			yield return null;
		}
		_primaryAvailable = true;
		LastCompletedRatio = 1f;
		_uiProgressBarPanel.SetActive(active: false);
		OnComplete();
		ActionCoroutine = null;
		_parentAnimator.SetBool(MovementController.CastingHash, value: false);
		if ((bool)startObject)
		{
			startObject.OnPrimaryUseEnd();
			StopWaitUntilDoneSound(success: true, startObject.UseDefaultUiUsingSounds());
		}
		if (actionCompleteSoundHash != 0 && (CursorManager.CursorThing == null || !CursorManager.CursorThing.AsStructure))
		{
			ParentHuman.PlayNetworkSound(actionCompleteSoundHash);
		}
	}

	private IEnumerator WaitUntilDone(DelegateEvent onFinished, float timeToWait, string actionName, Item item, int actionSoundHash, int actionCompleteSoundHash, string axis = "Secondary", bool keepAlive = false)
	{
		OnComplete = onFinished;
		float startTime = Time.time;
		int initialHand = ActiveHand.SlotId;
		_uiProgressBarPanel.SetActionName(actionName);
		_uiProgressBarPanel.SetItemName(item.DisplayName);
		_uiProgressBarPanel.SetActive(active: true);
		PlayWaitUntilDoneSound(actionSoundHash, playUiSounds: false);
		LastCompletedRatio = 0f;
		float elapsedTime;
		do
		{
			elapsedTime = Time.time - startTime;
			if (KeyManager.GetButtonUp(KeyMap.SecondaryAction) || initialHand != ActiveHand.SlotId || ActiveHand.Slot.Occupant != item)
			{
				_uiProgressBarPanel.SetActive(active: false);
				_parentAnimator.SetBool(MovementController.CastingHash, value: false);
				ActionCoroutine = null;
				LastCompletedRatio = elapsedTime / timeToWait;
				if (onFinished != null)
				{
					OnComplete();
				}
				StopWaitUntilDoneSound(success: false, playUiSounds: false);
				yield break;
			}
			_parentAnimator.SetBool(MovementController.CastingHash, item.CastAnimation != CastingAnimation.None);
			_uiProgressBarPanel.SetProgress((Time.time - startTime) / timeToWait);
			yield return null;
		}
		while (elapsedTime < timeToWait || keepAlive);
		LastCompletedRatio = 1f;
		_uiProgressBarPanel.SetActive(active: false);
		StopWaitUntilDoneSound(success: true, playUiSounds: false);
		if (actionCompleteSoundHash != 0)
		{
			ParentHuman.PlayNetworkSound(actionCompleteSoundHash);
		}
		if (onFinished != null)
		{
			OnComplete();
		}
		ActionCoroutine = null;
		_parentAnimator.SetBool(MovementController.CastingHash, value: false);
	}

	public static bool WillStackFromInteractable(Interactable interactable)
	{
		if (((interactable?.Slot != null) & (ActiveHandSlot != null)) && ActiveHandSlot.Contains<IMergeable>(out var occupant) && interactable.Slot.Contains<IMergeable>(out var occupant2))
		{
			return occupant.CanStack(occupant2);
		}
		return false;
	}
}
