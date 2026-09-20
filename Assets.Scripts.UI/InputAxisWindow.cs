using System;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI;

public class InputAxisWindow : InputControlBase, IModal
{
	public static InputAxisWindow Instance;

	public static InputPanelState InputState = InputPanelState.None;

	public TextMeshProUGUI ControllerText;

	private static ControllerAxisItem _currentAssignment;

	private ControllerAxis _currentAxis;

	public static float MinToAssign = 0.1f;

	public bool UnlockCursor => true;

	public static event InputAxisEvent OnSubmit;

	public static event Event OnCancel;

	public static void CancelInput()
	{
		Instance.ButtonInputCancel();
	}

	public static void SubmitInput()
	{
		Instance.ButtonInputSubmit();
	}

	public override void Initialize()
	{
		base.Initialize();
		Instance = this;
		SetVisible(isVisble: false);
	}

	public static bool ShowInputPanel(ControllerAxisItem controllerAxis)
	{
		if (InputState != InputPanelState.None || InputControlBase.IsRecording)
		{
			return false;
		}
		_currentAssignment = controllerAxis;
		InputState = InputPanelState.Waiting;
		Instance.SetVisible(isVisble: true);
		MouseModeController.AddModal(Instance);
		Instance.TitleText.text = controllerAxis.AxisName;
		Instance.ControllerText.text = controllerAxis.ControllerName;
		Instance.AssignmentText.text = controllerAxis.AxisKey.ToString().ToUpper();
		return true;
	}

	public void ButtonBeginRecordKey()
	{
		AssignmentAnimator.SetBool("Input", value: true);
		InputControlBase.IsRecording = true;
	}

	public void ButtonInputDefault()
	{
		InputControlBase.IsRecording = false;
		_currentAxis = ControllerAxis.None;
		Instance.AssignmentText.text = _currentAxis.ToString();
	}

	public override void ButtonInputCancel()
	{
		InputState = InputPanelState.Cancelled;
		Instance.SetVisible(isVisble: false);
		MouseModeController.RemoveModal(Instance);
		InputAxisWindow.OnCancel = null;
		InputAxisWindow.OnSubmit = null;
		_currentAxis = ControllerAxis.None;
		_currentAssignment = null;
		InputState = InputPanelState.None;
		base.ButtonInputCancel();
	}

	private void Update()
	{
		if (!InputControlBase.IsRecording || WorldManager.IsGamePaused)
		{
			return;
		}
		float num = 0f;
		ControllerAxis controllerAxis = ControllerAxis.None;
		foreach (ControllerAxis value in Enum.GetValues(typeof(ControllerAxis)))
		{
			if (value != ControllerAxis.None)
			{
				float num2 = Math.Abs(Input.GetAxis($"{_currentAssignment.SelectedController}{value}"));
				if (num2 > MinToAssign && num < num2)
				{
					num = num2;
					controllerAxis = value;
				}
			}
		}
		if (controllerAxis != ControllerAxis.None)
		{
			_currentAxis = controllerAxis;
			Instance.AssignmentText.text = controllerAxis.ToString().ToUpper();
			InputControlBase.IsRecording = false;
			AssignmentAnimator.SetBool("Input", value: false);
		}
	}

	public override void ButtonInputSubmit()
	{
		InputState = InputPanelState.Submitted;
		Instance.SetVisible(isVisble: false);
		MouseModeController.RemoveModal(Instance);
		if (InputAxisWindow.OnSubmit != null)
		{
			InputAxisWindow.OnSubmit(_currentAxis);
		}
		InputAxisWindow.OnCancel = null;
		InputAxisWindow.OnSubmit = null;
		Instance.AssignmentText.text = _currentAxis.ToString().ToUpper();
		InputState = InputPanelState.None;
		_currentAssignment = null;
		base.ButtonInputSubmit();
	}
}
