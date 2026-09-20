using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

public class InputManager : Singleton<InputManager>
{
	public enum InputManagerState
	{
		Stop,
		Recording,
		Playing
	}

	public enum ButtonState
	{
		Normal,
		ButtonDown,
		Down,
		Up
	}

	public enum Buttons
	{
		Primary,
		Secondary,
		Ascend,
		Desend,
		Drop,
		SpawnPrefab,
		SwapHands,
		SwapWorld
	}

	public enum AxisRaws
	{
		Vertical,
		Horizontal,
		Ascend,
		Descend
	}

	public enum Axises
	{
		LookX,
		LookY
	}

	public class InventoryManagerState
	{
		public int SpawnPrefabIndex;

		public float ActionTime;

		public InventoryManagerState(int InSpawnPrefabIndex, float InActionTime)
		{
			SpawnPrefabIndex = InSpawnPrefabIndex;
			ActionTime = InActionTime;
		}
	}

	public class PositionAndRotation
	{
		public Vector3 Position;

		public float InputX;

		public float InputY;

		public float ActionTime;

		public PositionAndRotation(Vector3 InPosition, float InInputX, float InInputY, float InActionTime)
		{
			InputX = InInputX;
			InputY = InInputY;
			Position = InPosition;
			ActionTime = InActionTime;
		}
	}

	public class RecordButton
	{
		public Buttons Button;

		public ButtonState ButtonState;

		public float ActionTime;

		public bool FirstFrame;

		public RecordButton(Buttons InButton, ButtonState InButtonState, float InActionTime, bool InFirstFrame)
		{
			Button = InButton;
			ButtonState = InButtonState;
			ActionTime = InActionTime;
			FirstFrame = InFirstFrame;
		}

		public bool GetButtonDown()
		{
			if (FirstFrame && ButtonState == ButtonState.Down)
			{
				FirstFrame = false;
				return true;
			}
			return false;
		}

		public void ResetFirstFrame()
		{
			FirstFrame = true;
		}

		public bool GetButtonUp()
		{
			if (FirstFrame && ButtonState == ButtonState.Up)
			{
				FirstFrame = false;
				return true;
			}
			return false;
		}
	}

	public class RecordAxisRaw
	{
		public AxisRaws Axis;

		public float ActionTime;

		public float Value;

		public RecordAxisRaw(AxisRaws InAxis, float InValue, float InActionTime)
		{
			Axis = InAxis;
			Value = InValue;
			ActionTime = InActionTime;
		}
	}

	public class RecordAxis
	{
		public Axises Axis;

		public float ActionTime;

		public float Value;

		public RecordAxis(Axises InAxis, float InValue, float InActionTime)
		{
			Axis = InAxis;
			Value = InValue;
			ActionTime = InActionTime;
		}
	}

	public InputManagerState CurrentState;

	private Dictionary<Buttons, ButtonState> ButtonDictionary;

	private Dictionary<Buttons, ButtonState> ButtonUpDictionary;

	private Dictionary<Buttons, ButtonState> ButtonDownDictionary;

	private Dictionary<AxisRaws, float> AxisRawDictionary;

	private Dictionary<Axises, float> AxisDictionary;

	private List<RecordButton> RecordButtonList;

	private List<RecordButton> RecordButtonUpList;

	private List<RecordButton> RecordButtonDownList;

	private List<RecordAxisRaw> RecordAxisRawList;

	private List<RecordAxis> RecordAxisList;

	private List<PositionAndRotation> PositionAndRotationList;

	private List<InventoryManagerState> InventoryManagerStateList;

	private Vector3 LastPosition = Vector3.zero;

	private float LastRotationX;

	private float LastRotationY;

	private Vector3 StartPosition = Vector3.zero;

	private float Elipsed;

	private int RecordButtonIndex;

	private int RecordButtonUpIndex;

	private int RecordButtonDownIndex;

	private int RecordAxisRawIndex;

	private int RecordAxisesIndex;

	private int PositionAndRotationIndex;

	private int InventoryManagerStateIndex;

	public override void ManagerStart()
	{
		base.ManagerStart();
		RecordButtonList = new List<RecordButton>();
		RecordButtonUpList = new List<RecordButton>();
		RecordButtonDownList = new List<RecordButton>();
		RecordAxisRawList = new List<RecordAxisRaw>();
		RecordAxisList = new List<RecordAxis>();
		PositionAndRotationList = new List<PositionAndRotation>();
		InventoryManagerStateList = new List<InventoryManagerState>();
		InitInput();
	}

	public void InitInput()
	{
		if (ButtonDictionary == null)
		{
			ButtonDictionary = new Dictionary<Buttons, ButtonState>();
		}
		if (ButtonUpDictionary == null)
		{
			ButtonUpDictionary = new Dictionary<Buttons, ButtonState>();
		}
		if (ButtonDownDictionary == null)
		{
			ButtonDownDictionary = new Dictionary<Buttons, ButtonState>();
		}
		if (AxisRawDictionary == null)
		{
			AxisRawDictionary = new Dictionary<AxisRaws, float>();
		}
		if (AxisDictionary == null)
		{
			AxisDictionary = new Dictionary<Axises, float>();
		}
		InitButtonState();
		RecordButtonList.Clear();
		RecordButtonUpList.Clear();
		RecordButtonDownList.Clear();
		RecordAxisRawList.Clear();
		RecordAxisList.Clear();
		PositionAndRotationList.Clear();
		InventoryManagerStateList.Clear();
		CurrentState = InputManagerState.Stop;
		Elipsed = 0f;
		RecordButtonIndex = 0;
		RecordButtonUpIndex = 0;
		RecordButtonDownIndex = 0;
		RecordAxisRawIndex = 0;
		RecordAxisesIndex = 0;
		PositionAndRotationIndex = 0;
		InventoryManagerStateIndex = 0;
		if ((bool)StatusUpdates.MovementController)
		{
			StartPosition = StatusUpdates.MovementController.controllingBody.transform.position;
			AddPositionAndRotation();
		}
	}

	private void InitButtonState()
	{
		string[] names = Enum.GetNames(typeof(Buttons));
		foreach (string value in names)
		{
			Buttons key = (Buttons)Enum.Parse(typeof(Buttons), value);
			ButtonDictionary[key] = ButtonState.Normal;
			ButtonUpDictionary[key] = ButtonState.Normal;
			ButtonDownDictionary[key] = ButtonState.Normal;
		}
		names = Enum.GetNames(typeof(AxisRaws));
		foreach (string value2 in names)
		{
			AxisRaws key2 = (AxisRaws)Enum.Parse(typeof(AxisRaws), value2);
			AxisRawDictionary[key2] = 0f;
		}
		names = Enum.GetNames(typeof(Axises));
		foreach (string value3 in names)
		{
			Axises key3 = (Axises)Enum.Parse(typeof(Axises), value3);
			AxisDictionary[key3] = 0f;
		}
	}

	private void StopActions()
	{
		if (CurrentState == InputManagerState.Recording)
		{
			ConsoleWindow.PrintAction("Input Recording Stopped");
		}
		else if (CurrentState == InputManagerState.Playing)
		{
			ConsoleWindow.PrintAction("Input Playing Stopped");
		}
		CurrentState = InputManagerState.Stop;
	}

	public void ToggleRecord()
	{
		if (CurrentState == InputManagerState.Stop)
		{
			StartRecordActions();
		}
		else
		{
			StopActions();
		}
	}

	public void TogglePlay()
	{
		if (CurrentState == InputManagerState.Stop)
		{
			StartPlayActions();
		}
		else
		{
			StopActions();
		}
	}

	private void StartRecordActions()
	{
		InitInput();
		CurrentState = InputManagerState.Recording;
		ConsoleWindow.PrintAction("Input Recording started");
	}

	private void StartPlayActions()
	{
		CameraController.Instance.UnitTest_SetRotation(0f, 0f);
		Elipsed = 0f;
		RecordAxisRawIndex = 0;
		RecordButtonIndex = 0;
		RecordButtonUpIndex = 0;
		RecordButtonDownIndex = 0;
		RecordAxisesIndex = 0;
		PositionAndRotationIndex = 0;
		InventoryManagerStateIndex = 0;
		CurrentState = InputManagerState.Playing;
		StartPosition = StatusUpdates.MovementController.controllingBody.transform.position;
		ConsoleWindow.PrintAction("Input Playing started");
	}

	private void CheckPlayOver()
	{
		if (RecordAxisesIndex >= RecordAxisList.Count && RecordAxisRawIndex >= RecordAxisRawList.Count && RecordButtonIndex >= RecordButtonList.Count && RecordButtonDownIndex >= RecordButtonDownList.Count && RecordButtonUpIndex >= RecordButtonUpList.Count && PositionAndRotationIndex >= PositionAndRotationList.Count && InventoryManagerStateIndex >= InventoryManagerStateList.Count)
		{
			Elipsed = 0f;
			RecordAxisesIndex = 0;
			RecordAxisRawIndex = 0;
			RecordButtonIndex = 0;
			RecordButtonUpIndex = 0;
			RecordButtonDownIndex = 0;
			PositionAndRotationIndex = 0;
			InventoryManagerStateIndex = 0;
			StartPosition = StatusUpdates.MovementController.controllingBody.transform.position;
			InitButtonState();
		}
	}

	private void LateUpdate()
	{
		if (!WorldManager.IsGamePaused)
		{
			Elipsed += Time.deltaTime;
			switch (CurrentState)
			{
			case InputManagerState.Playing:
				PlayActions();
				break;
			case InputManagerState.Recording:
				RecordActions();
				break;
			}
		}
	}

	private void RecordActions()
	{
		if (LastPosition != StatusUpdates.MovementController.controllingBody.transform.position || LastRotationX != CameraController.Instance.RotationX || LastRotationY != CameraController.Instance.RotationY)
		{
			AddPositionAndRotation();
		}
	}

	private void AddPositionAndRotation()
	{
		PositionAndRotationList.Add(new PositionAndRotation(StatusUpdates.MovementController.controllingBody.transform.position - StartPosition, CameraController.Instance.RotationX, CameraController.Instance.RotationY, Elipsed));
		LastPosition = StatusUpdates.MovementController.controllingBody.transform.position;
		LastRotationX = CameraController.Instance.RotationX;
		LastRotationY = CameraController.Instance.RotationY;
	}

	private void PlayActions()
	{
		if (CurrentState != InputManagerState.Playing)
		{
			return;
		}
		while (PositionAndRotationIndex < PositionAndRotationList.Count)
		{
			PositionAndRotation positionAndRotation = PositionAndRotationList[PositionAndRotationIndex];
			if (!(Elipsed >= positionAndRotation.ActionTime))
			{
				break;
			}
			StatusUpdates.MovementController.UnitTest_SetPos(StartPosition + positionAndRotation.Position);
			CameraController.Instance.UnitTest_SetRotation(positionAndRotation.InputX, positionAndRotation.InputY);
			PositionAndRotationIndex++;
		}
		while (RecordAxisRawIndex < RecordAxisRawList.Count)
		{
			RecordAxisRaw recordAxisRaw = RecordAxisRawList[RecordAxisRawIndex];
			if (!(Elipsed >= recordAxisRaw.ActionTime))
			{
				break;
			}
			AxisRawDictionary[recordAxisRaw.Axis] = recordAxisRaw.Value;
			RecordAxisRawIndex++;
		}
		while (RecordAxisesIndex < RecordAxisList.Count)
		{
			RecordAxis recordAxis = RecordAxisList[RecordAxisesIndex];
			if (!(Elipsed >= recordAxis.ActionTime))
			{
				break;
			}
			AxisDictionary[recordAxis.Axis] = recordAxis.Value;
			RecordAxisesIndex++;
		}
		while (RecordButtonIndex < RecordButtonList.Count)
		{
			RecordButton recordButton = RecordButtonList[RecordButtonIndex];
			if (!(Elipsed >= recordButton.ActionTime))
			{
				break;
			}
			Debug.Log($"{recordButton.ActionTime}: Button {recordButton.Button} {recordButton.ButtonState}");
			ButtonDictionary[recordButton.Button] = recordButton.ButtonState;
			RecordButtonIndex++;
		}
		CheckPreviousFrameButtonState();
		while (RecordButtonUpIndex < RecordButtonUpList.Count)
		{
			RecordButton recordButton2 = RecordButtonUpList[RecordButtonUpIndex];
			if (!(Elipsed >= recordButton2.ActionTime))
			{
				break;
			}
			Debug.Log($"{recordButton2.ActionTime}: ButtonUp {recordButton2.Button} {recordButton2.ButtonState}");
			ButtonUpDictionary[recordButton2.Button] = recordButton2.ButtonState;
			RecordButtonUpIndex++;
		}
		while (RecordButtonDownIndex < RecordButtonDownList.Count)
		{
			RecordButton recordButton3 = RecordButtonDownList[RecordButtonDownIndex];
			if (!(Elipsed >= recordButton3.ActionTime))
			{
				break;
			}
			Debug.Log($"{recordButton3.ActionTime}: ButtonDown{recordButton3.Button} {recordButton3.ButtonState}");
			ButtonDownDictionary[recordButton3.Button] = recordButton3.ButtonState;
			RecordButtonDownIndex++;
		}
		CheckPlayOver();
	}

	private void CheckPreviousFrameButtonState()
	{
		string[] names = Enum.GetNames(typeof(Buttons));
		foreach (string value in names)
		{
			Buttons key = (Buttons)Enum.Parse(typeof(Buttons), value);
			ButtonUpDictionary[key] = ButtonState.Normal;
			ButtonDownDictionary[key] = ButtonState.Normal;
		}
	}

	public void SetButtonState(Buttons button, ButtonState State)
	{
		ButtonDictionary[button] = State;
	}

	public float GetAxisRaw(string axisName)
	{
		switch (CurrentState)
		{
		case InputManagerState.Recording:
		{
			float axisRaw = Input.GetAxisRaw(axisName);
			AxisRaws axisRaws = (AxisRaws)Enum.Parse(typeof(AxisRaws), axisName);
			if (AxisRawDictionary.TryGetValue(axisRaws, out var value2) && axisRaw != value2)
			{
				AxisRawDictionary[axisRaws] = axisRaw;
				RecordAxisRawList.Add(new RecordAxisRaw(axisRaws, axisRaw, Elipsed));
			}
			return axisRaw;
		}
		case InputManagerState.Stop:
			return Input.GetAxisRaw(axisName);
		case InputManagerState.Playing:
		{
			AxisRaws key = (AxisRaws)Enum.Parse(typeof(AxisRaws), axisName);
			if (AxisRawDictionary.TryGetValue(key, out var value))
			{
				return value;
			}
			Debug.Log("Error : Not exist " + axisName);
			break;
		}
		}
		return 0f;
	}

	public float GetAxis(string axisName)
	{
		switch (CurrentState)
		{
		case InputManagerState.Recording:
		{
			float axis = Input.GetAxis(axisName);
			Axises axises = (Axises)Enum.Parse(typeof(Axises), axisName);
			if (AxisDictionary.TryGetValue(axises, out var value2) && axis != value2)
			{
				AxisDictionary[axises] = axis;
				RecordAxisList.Add(new RecordAxis(axises, axis, Elipsed));
			}
			return axis;
		}
		case InputManagerState.Stop:
			return Input.GetAxis(axisName);
		case InputManagerState.Playing:
		{
			Axises key = (Axises)Enum.Parse(typeof(Axises), axisName);
			if (AxisDictionary.TryGetValue(key, out var value))
			{
				return value;
			}
			Debug.Log("Error : Not exist " + axisName);
			break;
		}
		}
		return 0f;
	}

	public bool GetButtonUp(string buttonName)
	{
		switch (CurrentState)
		{
		case InputManagerState.Recording:
		{
			bool buttonUp = Input.GetButtonUp(buttonName);
			Buttons buttons = (Buttons)Enum.Parse(typeof(Buttons), buttonName);
			if (ButtonUpDictionary.TryGetValue(buttons, out var value2))
			{
				bool flag = ((ButtonState.Up == value2) ? true : false);
				if (buttonUp != flag)
				{
					if (buttonUp)
					{
						ButtonUpDictionary[buttons] = ButtonState.Up;
						RecordButtonUpList.Add(new RecordButton(buttons, ButtonState.Up, Elipsed, InFirstFrame: true));
					}
					else
					{
						ButtonUpDictionary[buttons] = ButtonState.Normal;
						RecordButtonUpList.Add(new RecordButton(buttons, ButtonState.Normal, Elipsed, InFirstFrame: true));
					}
				}
			}
			return buttonUp;
		}
		case InputManagerState.Stop:
			return Input.GetButtonUp(buttonName);
		case InputManagerState.Playing:
		{
			Buttons key = (Buttons)Enum.Parse(typeof(Buttons), buttonName);
			if (ButtonUpDictionary.TryGetValue(key, out var value))
			{
				if (ButtonState.Up != value)
				{
					return false;
				}
				return true;
			}
			Debug.Log("Error : Not exist " + buttonName);
			break;
		}
		}
		return false;
	}

	public bool GetButton(string buttonName)
	{
		switch (CurrentState)
		{
		case InputManagerState.Recording:
		{
			bool button = Input.GetButton(buttonName);
			Buttons buttons = (Buttons)Enum.Parse(typeof(Buttons), buttonName);
			if (ButtonDictionary.TryGetValue(buttons, out var value2))
			{
				bool flag = ((ButtonState.Down == value2) ? true : false);
				if (button != flag)
				{
					if (button)
					{
						ButtonDictionary[buttons] = ButtonState.Down;
						RecordButtonList.Add(new RecordButton(buttons, ButtonState.Down, Elipsed, InFirstFrame: true));
					}
					else
					{
						ButtonDictionary[buttons] = ButtonState.Normal;
						RecordButtonList.Add(new RecordButton(buttons, ButtonState.Normal, Elipsed, InFirstFrame: true));
					}
				}
			}
			return button;
		}
		case InputManagerState.Stop:
			return Input.GetButton(buttonName);
		case InputManagerState.Playing:
		{
			Buttons key = (Buttons)Enum.Parse(typeof(Buttons), buttonName);
			if (ButtonDictionary.TryGetValue(key, out var value))
			{
				if (ButtonState.Down != value)
				{
					return false;
				}
				return true;
			}
			Debug.Log("Error : Not exist " + buttonName);
			break;
		}
		}
		return false;
	}

	public bool GetButtonDown(string buttonName)
	{
		switch (CurrentState)
		{
		case InputManagerState.Recording:
		{
			bool buttonDown = Input.GetButtonDown(buttonName);
			Buttons buttons = (Buttons)Enum.Parse(typeof(Buttons), buttonName);
			if (ButtonDownDictionary.TryGetValue(buttons, out var value2))
			{
				bool flag = ((ButtonState.ButtonDown == value2) ? true : false);
				if (buttonDown != flag)
				{
					if (buttonDown)
					{
						ButtonDownDictionary[buttons] = ButtonState.ButtonDown;
						RecordButtonDownList.Add(new RecordButton(buttons, ButtonState.ButtonDown, Elipsed, InFirstFrame: true));
					}
					else
					{
						ButtonDownDictionary[buttons] = ButtonState.Normal;
						RecordButtonDownList.Add(new RecordButton(buttons, ButtonState.Normal, Elipsed, InFirstFrame: true));
					}
				}
			}
			return buttonDown;
		}
		case InputManagerState.Stop:
			return Input.GetButtonDown(buttonName);
		case InputManagerState.Playing:
		{
			Buttons key = (Buttons)Enum.Parse(typeof(Buttons), buttonName);
			if (ButtonDownDictionary.TryGetValue(key, out var value))
			{
				if (ButtonState.ButtonDown != value)
				{
					return false;
				}
				return true;
			}
			Debug.Log("Error : Not exist " + buttonName);
			break;
		}
		}
		return false;
	}
}
