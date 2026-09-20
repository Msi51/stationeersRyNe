using System;
using System.Collections.Generic;
using InputSystem;
using UnityEngine;

public static class KeyMap
{
	public static KeyCode Ascend;

	public static KeyCode Descend;

	public static KeyCode Left;

	public static KeyCode Right;

	public static KeyCode Forward;

	public static KeyCode Backward;

	public static KeyCode PrimaryAction;

	public static KeyCode SecondaryAction;

	public static KeyCode InventorySelect;

	public static KeyCode MoveAllOfType;

	public static KeyCode MoveAll;

	public static KeyCode HelmetSlot;

	public static KeyCode GlassesSlot;

	public static KeyCode SuitSlot;

	public static KeyCode BackSlot;

	public static KeyCode UniformSlot;

	public static KeyCode ToolBeltSlot;

	public static KeyCode ActiveHandSlot;

	public static KeyCode Grab;

	[Obsolete]
	public static KeyCode Drop;

	public static KeyCode OpenSeatScreen;

	public static KeyCode SwapHands;

	public static KeyCode SmartStow;

	public static KeyCode ShowControls;

	public static KeyCode ShowScoreBoard;

	public static KeyCode ShowDynamicPanel;

	public static KeyCode PreviousItem;

	public static KeyCode NextItem;

	public static KeyCode SpawnItem;

	public static KeyCode Jetpack;

	public static KeyCode ToggleHelperHints;

	public static KeyCode ToggleUi;

	public static KeyCode ToggleConsole;

	public static KeyCode ToggleInfo;

	public static KeyCode ScreenShot;

	public static KeyCode Internals;

	public static KeyCode Chatting;

	public static KeyCode PrecisionPlace;

	public static KeyCode Cancel;

	public static KeyCode RotateLeft;

	public static KeyCode RotateRight;

	public static KeyCode RotateUp;

	public static KeyCode RotateDown;

	public static KeyCode RotateRollLeft;

	public static KeyCode RotateRollRight;

	public static KeyCode InstantStop;

	public static KeyCode Teleport;

	public static KeyCode ToggleHandPower;

	public static KeyCode ToggleLight;

	public static KeyCode QuantityModifier;

	public static KeyCode FoVUp;

	public static KeyCode FoVDown;

	public static KeyCode FovReset;

	public static KeyCode EmoteWave;

	public static KeyCode MouseControl;

	public static KeyCode ThirdPersonControl;

	public static KeyCode HideAllWindows;

	public static KeyCode SuitPressureIncrease;

	public static KeyCode SuitPressureDecrease;

	public static KeyCode SuitTemperatureIncrease;

	public static KeyCode SuitTemperatureDecrease;

	public static KeyCode JetpackThrustIncrease;

	public static KeyCode JetpackThrustDecrease;

	public static KeyCode JetpackToggleStabilizer;

	public static KeyCode Help;

	public static KeyCode MouseInspect;

	public static KeyCode SmartTool;

	public static KeyCode PingHighlight;

	public static readonly KeyWrap _Ascend = new KeyWrap();

	public static readonly KeyWrap _Descend = new KeyWrap();

	public static readonly KeyWrap _Left = new KeyWrap();

	public static readonly KeyWrap _Right = new KeyWrap();

	public static readonly KeyWrap _Forward = new KeyWrap();

	public static readonly KeyWrap _Backward = new KeyWrap();

	public static readonly KeyWrap _PrimaryAction = new KeyWrap();

	public static readonly KeyWrap _SecondaryAction = new KeyWrap();

	public static readonly KeyWrap _InventorySelect = new KeyWrap();

	public static readonly KeyWrap _HelmetSlot = new KeyWrap();

	public static readonly KeyWrap _GlassesSlot = new KeyWrap();

	public static readonly KeyWrap _SuitSlot = new KeyWrap();

	public static readonly KeyWrap _BackSlot = new KeyWrap();

	public static readonly KeyWrap _UniformSlot = new KeyWrap();

	public static readonly KeyWrap _ToolBeltSlot = new KeyWrap();

	public static readonly KeyWrap _ActiveHandSlot = new KeyWrap();

	public static readonly KeyWrap _Grab = new KeyWrap();

	public static readonly KeyWrap _Drop = new KeyWrap();

	public static readonly KeyWrap _OpenSeatScreen = new KeyWrap();

	public static readonly KeyWrap _SwapHands = new KeyWrap();

	public static readonly KeyWrap _ShowControls = new KeyWrap();

	public static readonly KeyWrap _ShowScoreBoard = new KeyWrap();

	public static readonly KeyWrap _ShowDynamicPanel = new KeyWrap();

	public static readonly KeyWrap _PreviousItem = new KeyWrap();

	public static readonly KeyWrap _NextItem = new KeyWrap();

	public static readonly KeyWrap _SpawnItem = new KeyWrap();

	public static readonly KeyWrap _Jetpack = new KeyWrap();

	public static readonly KeyWrap _ToggleUi = new KeyWrap();

	public static readonly KeyWrap _ToggleConsole = new KeyWrap();

	public static readonly KeyWrap _ToggleInfo = new KeyWrap();

	public static readonly KeyWrap _ScreenShot = new KeyWrap();

	public static readonly KeyWrap _Internals = new KeyWrap();

	public static readonly KeyWrap _Chatting = new KeyWrap();

	public static readonly KeyWrap _PrecisionPlace = new KeyWrap();

	public static readonly KeyWrap _Cancel = new KeyWrap();

	public static readonly KeyWrap _RotateLeft = new KeyWrap();

	public static readonly KeyWrap _RotateRight = new KeyWrap();

	public static readonly KeyWrap _RotateUp = new KeyWrap();

	public static readonly KeyWrap _RotateDown = new KeyWrap();

	public static readonly KeyWrap _RotateRollLeft = new KeyWrap();

	public static readonly KeyWrap _RotateRollRight = new KeyWrap();

	public static readonly KeyWrap _InstantStop = new KeyWrap();

	public static readonly KeyWrap _Teleport = new KeyWrap();

	public static readonly KeyWrap _ToggleHandPower = new KeyWrap();

	public static readonly KeyWrap _ToggleLight = new KeyWrap();

	public static readonly KeyWrap _QuantityModifier = new KeyWrap();

	public static readonly KeyWrap _FoVUp = new KeyWrap();

	public static readonly KeyWrap _FoVDown = new KeyWrap();

	public static readonly KeyWrap _FovReset = new KeyWrap();

	public static readonly KeyWrap _EmoteWave = new KeyWrap();

	public static readonly KeyWrap _MouseControl = new KeyWrap();

	public static readonly KeyWrap _ThirdPersonControl = new KeyWrap();

	public static readonly KeyWrap _ThirdPersonShoulderSwitch = new KeyWrap();

	public static readonly KeyWrap _HideAllWindows = new KeyWrap();

	public static readonly KeyWrap _SuitPressureIncrease = new KeyWrap();

	public static readonly KeyWrap _SuitPressureDecrease = new KeyWrap();

	public static readonly KeyWrap _SuitTemperatureIncrease = new KeyWrap();

	public static readonly KeyWrap _SuitTemperatureDecrease = new KeyWrap();

	public static readonly KeyWrap _JetpackThrustIncrease = new KeyWrap();

	public static readonly KeyWrap _JetpackThrustDecrease = new KeyWrap();

	public static readonly KeyWrap _JetpackToggleStabilizer = new KeyWrap();

	public static readonly KeyWrap _Help = new KeyWrap();

	public static readonly KeyWrap _MouseInspect = new KeyWrap();

	public static readonly KeyWrap _SmartTool = new KeyWrap();

	public static readonly KeyWrap _PingHighlight = new KeyWrap();

	public static readonly KeyWrap _SmartStow = new KeyWrap();

	public static readonly KeyWrap _MoveAllOfType = new KeyWrap();

	public static readonly KeyWrap _MoveAll = new KeyWrap();

	private static HashSet<KeyWrap> PollingSet;

	internal static void AddToPolling(KeyWrap keyWrap)
	{
		if (PollingSet == null)
		{
			PollingSet = new HashSet<KeyWrap>();
		}
		PollingSet.Add(keyWrap);
	}

	internal static void PollInputs()
	{
		if (PollingSet == null)
		{
			return;
		}
		foreach (KeyWrap item in PollingSet)
		{
			item.PollForInput();
		}
	}
}
