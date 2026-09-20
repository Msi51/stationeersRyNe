using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using InputSystem;
using UnityEngine;

public class KeyManager : ManagerBase
{
	public delegate void Event();

	public static KeyManager Instance;

	public UserInterfaceAnimated WarningControls;

	public List<ButtonReference> ButtonReferences = new List<ButtonReference>();

	public List<ButtonReference> ControllerButtonReferences = new List<ButtonReference>();

	private static readonly Dictionary<KeyCode, ButtonReference> _buttonReferenceLookup = new Dictionary<KeyCode, ButtonReference>();

	private static readonly Dictionary<string, KeyInputState> _inputStateMap = new Dictionary<string, KeyInputState>();

	private static string _currentKey;

	private readonly KeyWrap _debugGridCapture = new KeyWrap(KeyCode.G, KeyCode.LeftControl);

	private readonly KeyWrap _quickSave = new KeyWrap(KeyCode.F5);

	private static readonly Dictionary<string, ControlsGroup> _controlsGroupLookup = new Dictionary<string, ControlsGroup>();

	public static Dictionary<string, KeyItem> KeyItemLookup = new Dictionary<string, KeyItem>();

	public static List<KeyItem> AllKeys = new List<KeyItem>();

	public static string EveryKey = "EveryKey";

	public static Dictionary<string, List<string>> IgnoreConflictKeyMaps = new Dictionary<string, List<string>>
	{
		{
			"MoveAll",
			new List<string> { "ThirdPersonControl", "Descend", "Ascend", "MouseInspect", "MoveAllOfType" }
		},
		{
			"MoveAllOfType",
			new List<string> { "ThirdPersonControl", "Descend", "Ascend", "MouseInspect", "MoveAll" }
		},
		{
			"MouseInspect",
			new List<string> { "ThirdPersonControl", "Descend", "Ascend", "MoveAll", "MoveAllOfType" }
		},
		{
			"Descend",
			new List<string> { "MouseInspect", "MoveAll", "MoveAllOfType" }
		},
		{
			"Ascend",
			new List<string> { "MouseInspect", "MoveAll", "MoveAllOfType" }
		},
		{
			"ThirdPersonControl",
			new List<string> { "MouseInspect", "MoveAll", "MoveAllOfType" }
		}
	};

	public static List<string> NeverConflict = new List<string> { "FovRefresh", "MouseInspect" };

	public static KeyInputState InputState { get; private set; } = KeyInputState.Game;

	public static bool IsMenuInputAllowed
	{
		get
		{
			if (GameManager.GameState == GameState.Running)
			{
				return InventoryManager.EnablePlayerKeys;
			}
			return false;
		}
	}

	public static event Event OnControlsChanged;

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		Instance = this;
		LoadButtonReferences();
		ResetKeyStateToDefault();
	}

	public override void ManagerStart()
	{
		base.ManagerStart();
		SetBindings();
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		KeyMap.PollInputs();
		ResetState();
	}

	private void ResetState()
	{
		if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Period))
		{
			SetBindings();
			ConsoleWindow.Print("KeyManager Reset");
			ResetKeyStateToDefault();
		}
	}

	public static void ResetBindingsOnRespawn()
	{
		Instance.SetBindings();
	}

	private void SetBindings()
	{
		KeyMap._Cancel.Bind(InputPhase.Up, InventoryManager.Instance.CancelKeyActions, KeyInputState.Game | KeyInputState.Paused);
		KeyMap._Cancel.Bind(InputPhase.Up, InventoryManager.Instance.CancelKeyActionsTypingState, KeyInputState.Typing);
		KeyMap._Help.Bind(InputPhase.Up, Stationpedia.HelpOnKeyDown);
		KeyMap._ShowScoreBoard.Bind(InputPhase.Down, ToggleScoreboard, KeyInputState.Game);
		KeyMap._FoVDown.Bind(InputPhase.Held, FovDecrease, KeyInputState.Game | KeyInputState.Cinematic);
		KeyMap._FoVUp.Bind(InputPhase.Held, FovIncrease, KeyInputState.Game | KeyInputState.Cinematic);
		KeyMap._FovReset.Bind(InputPhase.Held, FovReset, KeyInputState.Game | KeyInputState.Cinematic);
		KeyMap._ToggleLight.Bind(InputPhase.Down, ToggleHelmetLight, KeyInputState.Game);
		KeyMap._Teleport.Bind(InputPhase.Down, ToggleNightVision, KeyInputState.Game);
		KeyMap._SpawnItem.Bind(InputPhase.Down, SpawnDynamicThing, KeyInputState.Game);
		KeyMap._Internals.Bind(InputPhase.Down, ToggleInternals, KeyInputState.Game);
		KeyMap._SwapHands.Bind(InputPhase.Down, SwapHandsOnKeyUp, KeyInputState.Game);
		KeyMap._ToggleHandPower.Bind(InputPhase.Down, ToggleActiveHandTool, KeyInputState.Game);
		KeyMap._SecondaryAction.Bind(InputPhase.Up, ToggleActiveHandTool, KeyInputState.Game);
		KeyMap._SmartStow.Bind(InputPhase.Down, SmartStow, KeyInputState.Game);
		KeyMap._InventorySelect.Bind(InputPhase.Down, InventorySelect, KeyInputState.Game);
		KeyMap._Drop.Bind(InputPhase.Up, DropKeyUp, KeyInputState.Game);
		KeyMap._Drop.Bind(InputPhase.Down, DropKeyDown, KeyInputState.Game);
		KeyMap._Drop.Bind(InputPhase.Held, DropKeyHeld, KeyInputState.Game);
		_debugGridCapture.Bind(InputPhase.Up, CursorManager.DebugCaptureGridDataForBugReports);
		_quickSave.Bind(InputPhase.Up, XmlSaveLoad.QuickSaveCurrentWorld);
	}

	private static void ToggleScoreboard()
	{
		InventoryManager.Instance?.ToggleScoreboard();
	}

	private static void FovDecrease()
	{
		CameraController.Instance?.FovDecrease();
	}

	private static void FovIncrease()
	{
		CameraController.Instance?.FovIncrease();
	}

	private static void FovReset()
	{
		CameraController.Instance?.FovReset();
	}

	private static void ToggleHelmetLight()
	{
		Human.LocalHuman?.ToggleHelmetLight();
	}

	private static void ToggleNightVision()
	{
		Human.LocalHuman?.ToggleNightVision();
	}

	private static void SpawnDynamicThing()
	{
		Human.LocalHuman?.SpawnDynamicThing();
	}

	private static void ToggleInternals()
	{
		Human.LocalHuman?.ToggleInternals();
	}

	private static void ToggleActiveHandTool()
	{
		Human.LocalHuman?.HumanHandsBehaviour.ToggleActiveHandTool();
	}

	private static void SwapHandsOnKeyUp()
	{
		Human.LocalHuman?.HumanHandsBehaviour._SwapHandsOnKeyUp();
	}

	private static void DropKeyHeld()
	{
		Human.LocalHuman?.ThrowItemBehaviour.DropKeyHeld();
	}

	private static void DropKeyUp()
	{
		Human.LocalHuman?.ThrowItemBehaviour.DropKeyUp();
	}

	private static void DropKeyDown()
	{
		Human.LocalHuman?.ThrowItemBehaviour.DropKeyDown();
	}

	private static void SmartStow()
	{
		InventoryWindowManager.Instance?.SmartStow();
	}

	private static void InventorySelect()
	{
		InventoryWindowManager.Instance?.InventorySelect();
	}

	public static void ResetKeyStateToDefault()
	{
		_inputStateMap.Clear();
		SetInputState("KeyManager", KeyInputState.Game);
	}

	public static void SetInputState(string key, KeyInputState state)
	{
		_inputStateMap[key] = state;
		SetKeyInputStateInternal(key, state);
	}

	public static void RemoveInputState(string key)
	{
		if (_inputStateMap.ContainsKey(key))
		{
			_inputStateMap.Remove(key);
			if (!(_currentKey != key))
			{
				var (key2, state) = (KeyValuePair<string, KeyInputState>)(ref _inputStateMap.Last());
				SetKeyInputStateInternal(key2, state);
			}
		}
	}

	private static void SetKeyInputStateInternal(string key, KeyInputState state)
	{
		_currentKey = key;
		InputState = state;
	}

	public static string PrintKeyInputState()
	{
		string text = string.Format("{0} [owner, state] | Current: [{1}, {2}]\n\tStack: {3}", "KeyInputState", _currentKey, InputState, string.Join(", ", _inputStateMap.Reverse()));
		ConsoleWindow.Print(text);
		return text;
	}

	public void LoadButtonReferences()
	{
		_buttonReferenceLookup.Clear();
		foreach (ButtonReference buttonReference in ButtonReferences)
		{
			_buttonReferenceLookup.Add(buttonReference.Key, buttonReference);
		}
		if (!HasXboxController())
		{
			return;
		}
		foreach (ButtonReference controllerButtonReference in ControllerButtonReferences)
		{
			_buttonReferenceLookup.Add(controllerButtonReference.Key, controllerButtonReference);
		}
	}

	public static ButtonReference GetButtonReference(KeyCode key)
	{
		_buttonReferenceLookup.TryGetValue(key, out var value);
		return value;
	}

	public static ControlsGroup GetControlsGroup(string keyName)
	{
		_controlsGroupLookup.TryGetValue(keyName, out var value);
		return value;
	}

	private static void AddKey(string assignmentName, KeyCode keyCode, ControlsGroup controlsGroup, bool hidden = false)
	{
		_controlsGroupLookup[assignmentName] = controlsGroup;
		KeyItem keyItem = new KeyItem(assignmentName, keyCode, hidden);
		KeyItemLookup[assignmentName] = keyItem;
		if (!AllKeys.Contains(keyItem))
		{
			AllKeys.Add(keyItem);
		}
	}

	public static void AddGroupLookup(ControlsGroup controlsGroup)
	{
		_controlsGroupLookup[controlsGroup.Name] = controlsGroup;
	}

	public static KeyItem GetKeyitem(string keyName)
	{
		KeyItemLookup.TryGetValue(keyName, out var value);
		return value;
	}

	public static void AssignDefaultKeys()
	{
		foreach (KeyItem allKey in AllKeys)
		{
			ControlsAssignment.Deregister(allKey);
			allKey.Display.Assign(allKey.DefaultKey);
			allKey.Display.Refresh();
		}
		LoadKeyboardSetting();
		ControlsAssignment.RefreshState();
		if (KeyManager.OnControlsChanged != null)
		{
			KeyManager.OnControlsChanged();
		}
	}

	private static void SetDefaultKeys()
	{
		KeyMap.Ascend = KeyCode.Space;
		KeyMap.Descend = KeyCode.LeftControl;
		KeyMap.Left = KeyCode.A;
		KeyMap.Right = KeyCode.D;
		KeyMap.Forward = KeyCode.W;
		KeyMap.Backward = KeyCode.S;
		KeyMap.PrimaryAction = KeyCode.Mouse0;
		KeyMap.SecondaryAction = KeyCode.Mouse1;
		KeyMap.InventorySelect = KeyCode.F;
		KeyMap.HelmetSlot = KeyCode.Alpha1;
		KeyMap.GlassesSlot = KeyCode.Alpha2;
		KeyMap.SuitSlot = KeyCode.Alpha3;
		KeyMap.BackSlot = KeyCode.Alpha4;
		KeyMap.UniformSlot = KeyCode.Alpha5;
		KeyMap.ToolBeltSlot = KeyCode.Alpha6;
		KeyMap.ActiveHandSlot = KeyCode.R;
		KeyMap.Grab = KeyCode.H;
		KeyMap.Drop = KeyCode.Q;
		KeyMap.OpenSeatScreen = KeyCode.V;
		KeyMap.MoveAllOfType = KeyCode.LeftShift;
		KeyMap.MoveAll = KeyCode.LeftControl;
		KeyMap.SwapHands = KeyCode.E;
		KeyMap.SmartStow = KeyCode.G;
		KeyMap.ShowControls = KeyCode.None;
		KeyMap.ShowScoreBoard = KeyCode.Tab;
		KeyMap.ShowDynamicPanel = KeyCode.Slash;
		KeyMap.PreviousItem = KeyCode.LeftBracket;
		KeyMap.NextItem = KeyCode.RightBracket;
		KeyMap.SpawnItem = KeyCode.F9;
		KeyMap.Jetpack = KeyCode.J;
		KeyMap.ToggleUi = KeyCode.KeypadMultiply;
		KeyMap.ToggleHelperHints = KeyCode.F2;
		KeyMap.ToggleConsole = KeyCode.F3;
		KeyMap.ToggleInfo = KeyCode.F4;
		KeyMap.ScreenShot = KeyCode.P;
		KeyMap.Internals = KeyCode.I;
		KeyMap.ToggleHandPower = KeyCode.O;
		KeyMap.Chatting = KeyCode.Return;
		KeyMap.PrecisionPlace = KeyCode.T;
		KeyMap.Cancel = KeyCode.Escape;
		KeyMap.RotateLeft = KeyCode.Delete;
		KeyMap.RotateRight = KeyCode.PageDown;
		KeyMap.RotateUp = KeyCode.Home;
		KeyMap.RotateDown = KeyCode.End;
		KeyMap.RotateRollLeft = KeyCode.Insert;
		KeyMap.RotateRollRight = KeyCode.PageUp;
		KeyMap.InstantStop = KeyCode.B;
		KeyMap.Teleport = KeyCode.N;
		KeyMap.QuantityModifier = KeyCode.C;
		KeyMap.FoVUp = KeyCode.KeypadPlus;
		KeyMap.FoVDown = KeyCode.KeypadMinus;
		KeyMap.EmoteWave = KeyCode.Keypad1;
		KeyMap.ToggleLight = KeyCode.L;
		KeyMap.MouseControl = KeyCode.LeftAlt;
		KeyMap.ThirdPersonControl = KeyCode.LeftShift;
		KeyMap.HideAllWindows = KeyCode.BackQuote;
		KeyMap.SuitPressureIncrease = KeyCode.None;
		KeyMap.SuitPressureDecrease = KeyCode.None;
		KeyMap.SuitTemperatureIncrease = KeyCode.None;
		KeyMap.SuitTemperatureDecrease = KeyCode.None;
		KeyMap.JetpackThrustIncrease = KeyCode.None;
		KeyMap.JetpackThrustDecrease = KeyCode.None;
		KeyMap.JetpackToggleStabilizer = KeyCode.None;
		KeyMap.Help = KeyCode.F1;
		KeyMap.MouseInspect = KeyCode.LeftShift;
		KeyMap.FovReset = KeyCode.KeypadEnter;
		KeyMap.SmartTool = KeyCode.X;
		KeyMap.Grab = KeyCode.H;
		KeyMap.PingHighlight = KeyCode.Mouse2;
		KeyMap._Ascend.AssignKey(KeyCode.Space);
		KeyMap._Descend.AssignKey(KeyCode.LeftControl);
		KeyMap._Left.AssignKey(KeyCode.A);
		KeyMap._Right.AssignKey(KeyCode.D);
		KeyMap._Forward.AssignKey(KeyCode.W);
		KeyMap._Backward.AssignKey(KeyCode.S);
		KeyMap._PrimaryAction.AssignKey(KeyCode.Mouse0);
		KeyMap._SecondaryAction.AssignKey(KeyCode.Mouse1);
		KeyMap._InventorySelect.AssignKey(KeyCode.F);
		KeyMap._HelmetSlot.AssignKey(KeyCode.Alpha1);
		KeyMap._GlassesSlot.AssignKey(KeyCode.Alpha2);
		KeyMap._SuitSlot.AssignKey(KeyCode.Alpha3);
		KeyMap._BackSlot.AssignKey(KeyCode.Alpha4);
		KeyMap._UniformSlot.AssignKey(KeyCode.Alpha5);
		KeyMap._ToolBeltSlot.AssignKey(KeyCode.Alpha6);
		KeyMap._ActiveHandSlot.AssignKey(KeyCode.R);
		KeyMap._Grab.AssignKey(KeyCode.H);
		KeyMap._Drop.AssignKey(KeyCode.Q);
		KeyMap._OpenSeatScreen.AssignKey(KeyCode.V);
		KeyMap._SwapHands.AssignKey(KeyCode.E);
		KeyMap._SmartStow.AssignKey(KeyCode.G);
		KeyMap._MoveAllOfType.AssignKey(KeyCode.LeftShift);
		KeyMap._MoveAll.AssignKey(KeyCode.LeftControl);
		KeyMap._ShowControls.AssignKey(KeyCode.None);
		KeyMap._ShowScoreBoard.AssignKey(KeyCode.Tab);
		KeyMap._ShowDynamicPanel.AssignKey(KeyCode.Slash);
		KeyMap._PreviousItem.AssignKey(KeyCode.LeftBracket);
		KeyMap._NextItem.AssignKey(KeyCode.RightBracket);
		KeyMap._SpawnItem.AssignKey(KeyCode.F9);
		KeyMap._Jetpack.AssignKey(KeyCode.J);
		KeyMap._ToggleUi.AssignKey(KeyCode.F2);
		KeyMap._ToggleConsole.AssignKey(KeyCode.F3);
		KeyMap._ToggleInfo.AssignKey(KeyCode.F4);
		KeyMap._ScreenShot.AssignKey(KeyCode.P);
		KeyMap._Internals.AssignKey(KeyCode.I);
		KeyMap._ToggleHandPower.AssignKey(KeyCode.O);
		KeyMap._Chatting.AssignKey(KeyCode.Return);
		KeyMap._PrecisionPlace.AssignKey(KeyCode.T);
		KeyMap._Cancel.AssignKey(KeyCode.Escape);
		KeyMap._RotateLeft.AssignKey(KeyCode.Delete);
		KeyMap._RotateRight.AssignKey(KeyCode.PageDown);
		KeyMap._RotateUp.AssignKey(KeyCode.Home);
		KeyMap._RotateDown.AssignKey(KeyCode.End);
		KeyMap._RotateRollLeft.AssignKey(KeyCode.Insert);
		KeyMap._RotateRollRight.AssignKey(KeyCode.PageUp);
		KeyMap._InstantStop.AssignKey(KeyCode.B);
		KeyMap._Teleport.AssignKey(KeyCode.N);
		KeyMap._QuantityModifier.AssignKey(KeyCode.C);
		KeyMap._FoVUp.AssignKey(KeyCode.KeypadPlus);
		KeyMap._FoVDown.AssignKey(KeyCode.KeypadMinus);
		KeyMap._EmoteWave.AssignKey(KeyCode.Keypad1);
		KeyMap._ToggleLight.AssignKey(KeyCode.L);
		KeyMap._MouseControl.AssignKey(KeyCode.LeftAlt);
		KeyMap._ThirdPersonControl.AssignKey(KeyCode.LeftShift);
		KeyMap._ThirdPersonShoulderSwitch.AssignKey(KeyCode.None);
		KeyMap._HideAllWindows.AssignKey(KeyCode.BackQuote);
		KeyMap._SuitPressureIncrease.AssignKey(KeyCode.None);
		KeyMap._SuitPressureDecrease.AssignKey(KeyCode.None);
		KeyMap._SuitTemperatureIncrease.AssignKey(KeyCode.None);
		KeyMap._SuitTemperatureDecrease.AssignKey(KeyCode.None);
		KeyMap._JetpackThrustIncrease.AssignKey(KeyCode.None);
		KeyMap._JetpackThrustDecrease.AssignKey(KeyCode.None);
		KeyMap._JetpackToggleStabilizer.AssignKey(KeyCode.None);
		KeyMap._Help.AssignKey(KeyCode.F1);
		KeyMap._MouseInspect.AssignKey(KeyCode.LeftShift);
		KeyMap._FovReset.AssignKey(KeyCode.KeypadEnter);
		KeyMap._SmartTool.AssignKey(KeyCode.X);
		KeyMap._Grab.AssignKey(KeyCode.H);
		KeyMap._PingHighlight.AssignKey(KeyCode.Mouse2);
	}

	public static void SetupKeyBindings()
	{
		SetDefaultKeys();
		ControlsGroup controlsGroup = new ControlsGroup("General");
		ControlsGroup controlsGroup2 = new ControlsGroup("Interface");
		ControlsGroup controlsGroup3 = new ControlsGroup("Movement");
		ControlsGroup controlsGroup4 = new ControlsGroup("Inventory");
		ControlsGroup controlsGroup5 = new ControlsGroup("Interaction");
		ControlsGroup controlsGroup6 = new ControlsGroup("Construction");
		ControlsGroup controlsGroup7 = new ControlsGroup("Creative");
		ControlsGroup controlsGroup8 = new ControlsGroup("Camera");
		AddKey("Ascend", KeyMap.Ascend, controlsGroup3);
		AddKey("Descend", KeyMap.Descend, controlsGroup3);
		AddKey("Left", KeyMap.Left, controlsGroup3);
		AddKey("Right", KeyMap.Right, controlsGroup3);
		AddKey("Forward", KeyMap.Forward, controlsGroup3);
		AddKey("Backward", KeyMap.Backward, controlsGroup3);
		AddKey("PrimaryAction", KeyMap.PrimaryAction, controlsGroup);
		AddKey("SecondaryAction", KeyMap.SecondaryAction, controlsGroup);
		AddKey("InventorySelect", KeyMap.InventorySelect, controlsGroup4);
		AddKey("MoveAllOfType", KeyMap.MoveAllOfType, controlsGroup4);
		AddKey("MoveAll", KeyMap.MoveAll, controlsGroup4);
		AddKey("HelmetSlot", KeyMap.HelmetSlot, controlsGroup4);
		AddKey("GlassesSlot", KeyMap.GlassesSlot, controlsGroup4);
		AddKey("SuitSlot", KeyMap.SuitSlot, controlsGroup4);
		AddKey("BackSlot", KeyMap.BackSlot, controlsGroup4);
		AddKey("UniformSlot", KeyMap.UniformSlot, controlsGroup4);
		AddKey("ToolBeltSlot", KeyMap.ToolBeltSlot, controlsGroup4);
		AddKey("ActiveHandSlot", KeyMap.ActiveHandSlot, controlsGroup4);
		AddKey("Grab", KeyMap.Grab, controlsGroup3);
		AddKey("Drop", KeyMap.Drop, controlsGroup4);
		AddKey("OpenSeatScreen", KeyMap.OpenSeatScreen, controlsGroup4);
		AddKey("SmartStow", KeyMap.SmartStow, controlsGroup4);
		AddKey("SwapHands", KeyMap.SwapHands, controlsGroup4);
		AddKey("ShowControls", KeyMap.ShowControls, controlsGroup2, hidden: true);
		AddKey("ShowScoreBoard", KeyMap.ShowScoreBoard, controlsGroup2);
		AddKey("ShowDynamicPanel", KeyMap.ShowDynamicPanel, controlsGroup7);
		AddKey("PreviousItem", KeyMap.PreviousItem, controlsGroup7);
		AddKey("NextItem", KeyMap.NextItem, controlsGroup7);
		AddKey("SpawnItem", KeyMap.SpawnItem, controlsGroup7);
		AddKey("Jetpack", KeyMap.Jetpack, controlsGroup5);
		AddKey("Help", KeyMap.Help, controlsGroup2);
		AddKey("ToggleUIVisibility", KeyMap.ToggleUi, controlsGroup2);
		AddKey("ToggleHelperHints", KeyMap.ToggleHelperHints, controlsGroup2);
		AddKey("ToggleConsole", KeyMap.ToggleConsole, controlsGroup2);
		AddKey("ToggleInfo", KeyMap.ToggleInfo, controlsGroup2);
		AddKey("ScreenShot", KeyMap.ScreenShot, controlsGroup2);
		AddKey("Internals", KeyMap.Internals, controlsGroup5);
		AddKey("ToggleHandPower", KeyMap.ToggleHandPower, controlsGroup5);
		AddKey("Chatting", KeyMap.Chatting, controlsGroup2);
		AddKey("PrecisionPlace", KeyMap.PrecisionPlace, controlsGroup4);
		AddKey("Cancel", KeyMap.Cancel, controlsGroup2);
		AddKey("RotateLeft", KeyMap.RotateLeft, controlsGroup6);
		AddKey("RotateRight", KeyMap.RotateRight, controlsGroup6);
		AddKey("RotateUp", KeyMap.RotateUp, controlsGroup6);
		AddKey("RotateDown", KeyMap.RotateDown, controlsGroup6);
		AddKey("RotateRollLeft", KeyMap.RotateRollLeft, controlsGroup6);
		AddKey("RotateRollRight", KeyMap.RotateRollRight, controlsGroup6);
		AddKey("InstantStop", KeyMap.InstantStop, controlsGroup7);
		AddKey("NightVision", KeyMap.Teleport, controlsGroup);
		AddKey("QuantityModifier", KeyMap.QuantityModifier, controlsGroup);
		AddKey("MouseControl", KeyMap.MouseControl, controlsGroup4);
		AddKey("MouseInspect", KeyMap.MouseInspect, controlsGroup4);
		AddKey("FoVUp", KeyMap.FoVUp, controlsGroup8);
		AddKey("FoVDown", KeyMap.FoVDown, controlsGroup8);
		AddKey("FovReset", KeyMap.FovReset, controlsGroup8);
		AddKey("EmoteWave", KeyMap.EmoteWave, controlsGroup);
		AddKey("ToggleLight", KeyMap.ToggleLight, controlsGroup5);
		AddKey("ThirdPersonControl", KeyMap.ThirdPersonControl, controlsGroup8);
		AddKey("HideAllWindows", KeyMap.HideAllWindows, controlsGroup2);
		AddKey("SuitPressureIncrease", KeyMap.SuitPressureIncrease, controlsGroup5);
		AddKey("SuitPressureDecrease", KeyMap.SuitPressureDecrease, controlsGroup5);
		AddKey("SuitTemperatureIncrease", KeyMap.SuitTemperatureIncrease, controlsGroup5);
		AddKey("SuitTemperatureDecrease", KeyMap.SuitTemperatureDecrease, controlsGroup5);
		AddKey("JetpackThrustIncrease", KeyMap.JetpackThrustIncrease, controlsGroup5);
		AddKey("JetpackThrustDecrease", KeyMap.JetpackThrustDecrease, controlsGroup5);
		AddKey("JetpackToggleStabilizer", KeyMap.JetpackToggleStabilizer, controlsGroup5);
		AddKey("PingHighlight", KeyMap.PingHighlight, controlsGroup2);
		if (KeyManager.OnControlsChanged != null)
		{
			KeyManager.OnControlsChanged();
		}
		ControlsAssignment.RefreshState();
	}

	public static KeyCode GetKey(string _name)
	{
		KeyItemLookup.TryGetValue(_name, out var value);
		return value?.Key ?? KeyCode.None;
	}

	public static bool IsIgnoredConflict(KeyItem keyItem)
	{
		if (NeverConflict.Contains(keyItem.Name))
		{
			return true;
		}
		foreach (string item in NeverConflict)
		{
			if (keyItem.Key == GetKey(item))
			{
				return true;
			}
		}
		IgnoreConflictKeyMaps.TryGetValue(keyItem.Name, out var value);
		if (value == null)
		{
			return false;
		}
		foreach (string item2 in value)
		{
			if (keyItem.Key == GetKey(item2))
			{
				return true;
			}
		}
		return false;
	}

	public static void LoadControllers()
	{
		ControllerMap.VerticalMovement.Deserialize(Settings.CurrentData.VerticalMovementAxis);
		ControllerMap.HorizontalMovement.Deserialize(Settings.CurrentData.HorizontalMovementAxis);
		ControllerMap.ForwardMovement.Deserialize(Settings.CurrentData.ForwardMovementAxis);
		ControllerMap.HorizontalLook.Deserialize(Settings.CurrentData.HorizontalLookAxis);
		ControllerMap.VerticalLook.Deserialize(Settings.CurrentData.VerticalLookAxis);
	}

	public static void LoadKeyboardSetting()
	{
		KeyMap.Ascend = GetKey("Ascend");
		KeyMap.Descend = GetKey("Descend");
		KeyMap.Left = GetKey("Left");
		KeyMap.Right = GetKey("Right");
		KeyMap.Forward = GetKey("Forward");
		KeyMap.Backward = GetKey("Backward");
		KeyMap.PrimaryAction = GetKey("PrimaryAction");
		KeyMap.SecondaryAction = GetKey("SecondaryAction");
		KeyMap.InventorySelect = GetKey("InventorySelect");
		KeyMap.HelmetSlot = GetKey("HelmetSlot");
		KeyMap.GlassesSlot = GetKey("GlassesSlot");
		KeyMap.SuitSlot = GetKey("SuitSlot");
		KeyMap.BackSlot = GetKey("BackSlot");
		KeyMap.UniformSlot = GetKey("UniformSlot");
		KeyMap.ToolBeltSlot = GetKey("ToolBeltSlot");
		KeyMap.ActiveHandSlot = GetKey("ActiveHandSlot");
		KeyMap.Grab = GetKey("Grab");
		KeyMap.Drop = GetKey("Drop");
		KeyMap.OpenSeatScreen = GetKey("OpenSeatScreen");
		KeyMap.MoveAll = GetKey("MoveAll");
		KeyMap.MoveAllOfType = GetKey("MoveAllOfType");
		KeyMap.SwapHands = GetKey("SwapHands");
		KeyMap.ShowControls = GetKey("ShowControls");
		KeyMap.ShowScoreBoard = GetKey("ShowScoreBoard");
		KeyMap.ShowDynamicPanel = GetKey("ShowDynamicPanel");
		KeyMap.PreviousItem = GetKey("PreviousItem");
		KeyMap.NextItem = GetKey("NextItem");
		KeyMap.SpawnItem = GetKey("SpawnItem");
		KeyMap.Jetpack = GetKey("Jetpack");
		KeyMap.ToggleUi = GetKey("ToggleUIVisibility");
		KeyMap.ToggleHelperHints = GetKey("ToggleHelperHints");
		KeyMap.ToggleConsole = GetKey("ToggleConsole");
		KeyMap.ToggleInfo = GetKey("ToggleInfo");
		KeyMap.ScreenShot = GetKey("ScreenShot");
		KeyMap.Internals = GetKey("Internals");
		KeyMap.Chatting = GetKey("Chatting");
		KeyMap.PrecisionPlace = GetKey("PrecisionPlace");
		KeyMap.Cancel = GetKey("Cancel");
		KeyMap.RotateLeft = GetKey("RotateLeft");
		KeyMap.RotateRight = GetKey("RotateRight");
		KeyMap.RotateUp = GetKey("RotateUp");
		KeyMap.RotateDown = GetKey("RotateDown");
		KeyMap.RotateRollLeft = GetKey("RotateRollLeft");
		KeyMap.RotateRollRight = GetKey("RotateRollRight");
		KeyMap.InstantStop = GetKey("InstantStop");
		KeyMap.ToggleHandPower = GetKey("ToggleHandPower");
		KeyMap.QuantityModifier = GetKey("QuantityModifier");
		KeyMap.MouseControl = GetKey("MouseControl");
		KeyMap.Teleport = GetKey("NightVision");
		KeyMap.FoVUp = GetKey("FoVUp");
		KeyMap.FoVDown = GetKey("FoVDown");
		KeyMap.FovReset = GetKey("FovReset");
		KeyMap.EmoteWave = GetKey("EmoteWave");
		KeyMap.ToggleLight = GetKey("ToggleLight");
		KeyMap.ThirdPersonControl = GetKey("ThirdPersonControl");
		KeyMap.HideAllWindows = GetKey("HideAllWindows");
		KeyMap.SuitPressureIncrease = GetKey("SuitPressureIncrease");
		KeyMap.SuitPressureDecrease = GetKey("SuitPressureDecrease");
		KeyMap.SuitTemperatureIncrease = GetKey("SuitTemperatureIncrease");
		KeyMap.SuitTemperatureDecrease = GetKey("SuitTemperatureDecrease");
		KeyMap.JetpackThrustIncrease = GetKey("JetpackThrustIncrease");
		KeyMap.JetpackThrustDecrease = GetKey("JetpackThrustDecrease");
		KeyMap.JetpackToggleStabilizer = GetKey("JetpackToggleStabilizer");
		KeyMap.Help = GetKey("Help");
		KeyMap.MouseInspect = GetKey("MouseInspect");
		KeyMap.PingHighlight = GetKey("PingHighlight");
		KeyMap._Ascend.AssignKey(GetKey("Ascend"));
		KeyMap._Descend.AssignKey(GetKey("Descend"));
		KeyMap._Left.AssignKey(GetKey("Left"));
		KeyMap._Right.AssignKey(GetKey("Right"));
		KeyMap._Forward.AssignKey(GetKey("Forward"));
		KeyMap._Backward.AssignKey(GetKey("Backward"));
		KeyMap._PrimaryAction.AssignKey(GetKey("PrimaryAction"));
		KeyMap._SecondaryAction.AssignKey(GetKey("SecondaryAction"));
		KeyMap._InventorySelect.AssignKey(GetKey("InventorySelect"));
		KeyMap._HelmetSlot.AssignKey(GetKey("HelmetSlot"));
		KeyMap._GlassesSlot.AssignKey(GetKey("GlassesSlot"));
		KeyMap._SuitSlot.AssignKey(GetKey("SuitSlot"));
		KeyMap._BackSlot.AssignKey(GetKey("BackSlot"));
		KeyMap._UniformSlot.AssignKey(GetKey("UniformSlot"));
		KeyMap._ToolBeltSlot.AssignKey(GetKey("ToolBeltSlot"));
		KeyMap._ActiveHandSlot.AssignKey(GetKey("ActiveHandSlot"));
		KeyMap._Grab.AssignKey(GetKey("Grab"));
		KeyMap._Drop.AssignKey(GetKey("Drop"));
		KeyMap._OpenSeatScreen.AssignKey(GetKey("OpenSeatScreen"));
		KeyMap._SwapHands.AssignKey(GetKey("SwapHands"));
		KeyMap._SmartStow.AssignKey(GetKey("SmartStow"));
		KeyMap._MoveAll.AssignKey(GetKey("MoveAll"));
		KeyMap._MoveAllOfType.AssignKey(GetKey("MoveAllOfType"));
		KeyMap._ShowControls.AssignKey(GetKey("ShowControls"));
		KeyMap._ShowScoreBoard.AssignKey(GetKey("ShowScoreBoard"));
		KeyMap._ShowDynamicPanel.AssignKey(GetKey("ShowDynamicPanel"));
		KeyMap._PreviousItem.AssignKey(GetKey("PreviousItem"));
		KeyMap._NextItem.AssignKey(GetKey("NextItem"));
		KeyMap._SpawnItem.AssignKey(GetKey("SpawnItem"));
		KeyMap._Jetpack.AssignKey(GetKey("Jetpack"));
		KeyMap._ToggleUi.AssignKey(GetKey("ToggleUI"));
		KeyMap._ToggleConsole.AssignKey(GetKey("ToggleConsole"));
		KeyMap._ToggleInfo.AssignKey(GetKey("ToggleInfo"));
		KeyMap._ScreenShot.AssignKey(GetKey("ScreenShot"));
		KeyMap._Internals.AssignKey(GetKey("Internals"));
		KeyMap._Chatting.AssignKey(GetKey("Chatting"));
		KeyMap._PrecisionPlace.AssignKey(GetKey("PrecisionPlace"));
		KeyMap._Cancel.AssignKey(GetKey("Cancel"));
		KeyMap._RotateLeft.AssignKey(GetKey("RotateLeft"));
		KeyMap._RotateRight.AssignKey(GetKey("RotateRight"));
		KeyMap._RotateUp.AssignKey(GetKey("RotateUp"));
		KeyMap._RotateDown.AssignKey(GetKey("RotateDown"));
		KeyMap._RotateRollLeft.AssignKey(GetKey("RotateRollLeft"));
		KeyMap._RotateRollRight.AssignKey(GetKey("RotateRollRight"));
		KeyMap._InstantStop.AssignKey(GetKey("InstantStop"));
		KeyMap._ToggleHandPower.AssignKey(GetKey("ToggleHandPower"));
		KeyMap._QuantityModifier.AssignKey(GetKey("QuantityModifier"));
		KeyMap._MouseControl.AssignKey(GetKey("MouseControl"));
		KeyMap._Teleport.AssignKey(GetKey("NightVision"));
		KeyMap._FoVUp.AssignKey(GetKey("FoVUp"));
		KeyMap._FoVDown.AssignKey(GetKey("FoVDown"));
		KeyMap._FovReset.AssignKey(GetKey("FovReset"));
		KeyMap._EmoteWave.AssignKey(GetKey("EmoteWave"));
		KeyMap._ToggleLight.AssignKey(GetKey("ToggleLight"));
		KeyMap._ThirdPersonControl.AssignKey(GetKey("ThirdPersonControl"));
		KeyMap._ThirdPersonShoulderSwitch.AssignKey(GetKey("TPShoulderSwitch"));
		KeyMap._HideAllWindows.AssignKey(GetKey("HideAllWindows"));
		KeyMap._SuitPressureIncrease.AssignKey(GetKey("SuitPressureIncrease"));
		KeyMap._SuitPressureDecrease.AssignKey(GetKey("SuitPressureDecrease"));
		KeyMap._SuitTemperatureIncrease.AssignKey(GetKey("SuitTemperatureIncrease"));
		KeyMap._SuitTemperatureDecrease.AssignKey(GetKey("SuitTemperatureDecrease"));
		KeyMap._JetpackThrustIncrease.AssignKey(GetKey("JetpackThrustIncrease"));
		KeyMap._JetpackThrustDecrease.AssignKey(GetKey("JetpackThrustDecrease"));
		KeyMap._JetpackToggleStabilizer.AssignKey(GetKey("JetpackToggleStabilizer"));
		KeyMap._Help.AssignKey(GetKey("Help"));
		KeyMap._MouseInspect.AssignKey(GetKey("MouseInspect"));
		KeyMap._PingHighlight.AssignKey(GetKey("PingHighlight"));
		if (KeyManager.OnControlsChanged != null)
		{
			KeyManager.OnControlsChanged();
		}
	}

	public static float GetRightAxis()
	{
		float num = 0f;
		if (Input.GetKey(KeyMap.Left))
		{
			num -= 1f;
		}
		else if (Input.GetKey(KeyMap.Right))
		{
			num += 1f;
		}
		if (HasAxis(ControllerMap.HorizontalMovement))
		{
			num = Mathf.Clamp(num + ControllerMap.HorizontalMovement.Output, -1f, 1f);
		}
		return num;
	}

	public static float GetForwardAxis()
	{
		float num = 0f;
		if (Input.GetKey(KeyMap.Backward))
		{
			num -= 1f;
		}
		else if (Input.GetKey(KeyMap.Forward))
		{
			num += 1f;
		}
		if (HasAxis(ControllerMap.ForwardMovement))
		{
			num = Mathf.Clamp(num + ControllerMap.ForwardMovement.Output, -1f, 1f);
		}
		return num;
	}

	public static float GetAscend()
	{
		if (ConsoleWindow.IsOpen)
		{
			return 0f;
		}
		float num = 0f;
		if (Input.GetKey(KeyMap.Ascend))
		{
			num = 1f;
		}
		if (HasAxis(ControllerMap.VerticalMovement) && ControllerMap.VerticalMovement.Output > 0f)
		{
			num = Mathf.Clamp(num + ControllerMap.VerticalMovement.Output, 0f, 1f);
		}
		return num;
	}

	public static float GetDescend()
	{
		if (ConsoleWindow.IsOpen)
		{
			return 0f;
		}
		float num = 0f;
		if (Input.GetKey(KeyMap.Descend))
		{
			num = 1f;
		}
		if (HasAxis(ControllerMap.VerticalMovement) && ControllerMap.VerticalMovement.Output < 0f)
		{
			num = Mathf.Clamp(num - ControllerMap.VerticalMovement.Output, 0f, 1f);
		}
		return num;
	}

	public static bool GetButtonDown(KeyCode key)
	{
		if (key != KeyMap.ToggleConsole && ConsoleWindow.IsOpen)
		{
			return false;
		}
		return Input.GetKeyDown(key);
	}

	public static bool GetButtonUp(KeyCode key)
	{
		if (key != KeyMap.ToggleConsole && ConsoleWindow.IsOpen)
		{
			return false;
		}
		return Input.GetKeyUp(key);
	}

	public static bool GetButton(KeyCode key)
	{
		if (key != KeyMap.ToggleConsole && ConsoleWindow.IsOpen)
		{
			return false;
		}
		return Input.GetKey(key);
	}

	public static float GetAxis(ControllerAssignment assignment)
	{
		return Input.GetAxis(assignment.InputAxis);
	}

	public static bool HasAxis(ControllerAssignment assignment)
	{
		return assignment.IsValid;
	}

	public static bool GetMouseDown(string key)
	{
		if (key.Equals("Primary"))
		{
			return GetButtonDown(KeyMap.PrimaryAction);
		}
		return GetButtonDown(KeyMap.SecondaryAction);
	}

	public static bool GetMouseUp(string key)
	{
		if (key.Equals("Primary"))
		{
			return GetButtonUp(KeyMap.PrimaryAction);
		}
		return GetButtonUp(KeyMap.SecondaryAction);
	}

	public static bool GetMouse(string key)
	{
		if (key.Equals("Primary"))
		{
			return GetButton(KeyMap.PrimaryAction);
		}
		return GetButton(KeyMap.SecondaryAction);
	}

	public static bool HasXboxController()
	{
		if (Input.GetJoystickNames() != null && Input.GetJoystickNames().Length != 0)
		{
			string[] joystickNames = Input.GetJoystickNames();
			for (int i = 0; i < joystickNames.Length; i++)
			{
				if (joystickNames[i].ToLower().Contains("xbox"))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void FetchKey()
	{
		foreach (KeyCode value in Enum.GetValues(typeof(KeyCode)))
		{
			if (Input.GetKeyDown(value))
			{
				Debug.Log(Enum.GetName(typeof(KeyCode), value));
			}
		}
	}
}
