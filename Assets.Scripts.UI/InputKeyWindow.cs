using System;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class InputKeyWindow : InputControlBase, IModal
{
	public Button AssignmentButton;

	public Image Background;

	public static InputKeyWindow Instance;

	public static InputPanelState InputState = InputPanelState.None;

	public static Array KeyCodes = Enum.GetValues(typeof(KeyCode));

	private static KeyCode _currentKey;

	private static ControlsAssignment _currentAssignment;

	public bool UnlockCursor => true;

	public static event InputEvent OnSubmit;

	public static event Event OnCancel;

	public override void ButtonInputCancel()
	{
		InputState = InputPanelState.Cancelled;
		Instance.SetVisible(isVisble: false);
		MouseModeController.RemoveModal(Instance);
		InputKeyWindow.OnCancel = null;
		InputKeyWindow.OnSubmit = null;
		_currentKey = KeyCode.None;
		_currentAssignment = null;
		InputState = InputPanelState.None;
		base.ButtonInputCancel();
	}

	public void ButtonBeginRecordKey()
	{
		AssignmentAnimator.SetBool("Input", value: true);
		InputControlBase.IsRecording = true;
	}

	private void Update()
	{
		if (!InputControlBase.IsRecording)
		{
			return;
		}
		foreach (KeyCode keyCode in KeyCodes)
		{
			if (Input.GetKeyDown(keyCode))
			{
				_currentKey = keyCode;
				Instance.AssignmentText.text = Localization.GetKeyName(_currentKey).ToUpper();
				InputControlBase.IsRecording = false;
				AssignmentAnimator.SetBool("Input", value: false);
				break;
			}
		}
	}

	public override void ButtonInputSubmit()
	{
		InputState = InputPanelState.Submitted;
		Instance.SetVisible(isVisble: false);
		MouseModeController.RemoveModal(Instance);
		if (InputKeyWindow.OnSubmit != null)
		{
			InputKeyWindow.OnSubmit(_currentKey);
		}
		InputKeyWindow.OnCancel = null;
		InputKeyWindow.OnSubmit = null;
		ControlsAssignment.Deregister(_currentAssignment.KeyItem);
		_currentAssignment.Assign(_currentKey);
		Instance.AssignmentText.text = Localization.GetKeyName(_currentAssignment.KeyItem.Key).ToUpper();
		InputState = InputPanelState.None;
		_currentAssignment = null;
		base.ButtonInputSubmit();
	}

	public void ButtonInputDefault()
	{
		InputControlBase.IsRecording = false;
		_currentKey = KeyCode.None;
		Instance.AssignmentText.text = Localization.GetKeyName(_currentKey).ToUpper();
	}

	public static void CancelInput()
	{
		Instance.ButtonInputCancel();
	}

	public static void SubmitInput()
	{
		Instance.ButtonInputSubmit();
	}

	public static bool ShowInputPanel(ControlsAssignment controlsAssignment)
	{
		if (InputState != InputPanelState.None || InputControlBase.IsRecording)
		{
			return false;
		}
		_currentAssignment = controlsAssignment;
		InputState = InputPanelState.Waiting;
		Instance.SetVisible(isVisble: true);
		MouseModeController.AddModal(Instance);
		Instance.TitleText.text = controlsAssignment.KeyItem.Name.ToProper();
		Instance.AssignmentText.text = Localization.GetKeyName(controlsAssignment.KeyItem.Key).ToUpper();
		return true;
	}

	public override void Initialize()
	{
		base.Initialize();
		Instance = this;
		SetVisible(isVisble: false);
	}
}
