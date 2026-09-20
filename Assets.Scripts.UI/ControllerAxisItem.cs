using System.Collections.Generic;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class ControllerAxisItem : UserInterfaceBase
{
	public TextMeshProUGUI AssignmentText;

	public TMP_Dropdown ControllerDropdown;

	public Slider AxisSlider;

	public Button AxisButton;

	public TextMeshProUGUI AxisButtonText;

	[ReadOnly]
	public string AxisName;

	[ReadOnly]
	public Controller SelectedController;

	[ReadOnly]
	public string ControllerName;

	[ReadOnly]
	public ControllerAxis AxisKey;

	public ControllerAssignment Assignment;

	public static List<TMP_Dropdown.OptionData> ControllerOptionData = new List<TMP_Dropdown.OptionData>
	{
		new TMP_Dropdown.OptionData("None")
	};

	private bool _initialized;

	public override void SetVisible(bool isVisble)
	{
		base.SetVisible(isVisble);
		if (isVisble && !_initialized)
		{
			Initialize();
		}
	}

	public static void InitializeJoysticks()
	{
		ControllerOptionData = new List<TMP_Dropdown.OptionData>
		{
			new TMP_Dropdown.OptionData("None")
		};
		string[] joystickNames = Input.GetJoystickNames();
		foreach (string text in joystickNames)
		{
			ControllerOptionData.Add(new TMP_Dropdown.OptionData(text));
		}
	}

	public void Initialize()
	{
		_initialized = true;
		ControllerDropdown.ClearOptions();
		ControllerDropdown.AddOptions(ControllerOptionData);
		AxisSlider.gameObject.SetActive(value: false);
	}

	public void OnControllerChanged()
	{
		if (SelectedController != (Controller)ControllerDropdown.value)
		{
			SelectedController = (Controller)ControllerDropdown.value;
			AxisKey = ControllerAxis.None;
			string text = ControllerOptionData[ControllerDropdown.value].text;
			if (IsValid() && !string.IsNullOrEmpty(text))
			{
				AxisButton.interactable = true;
				ControllerName = ControllerOptionData[ControllerDropdown.value].text;
				Assignment.Controller = SelectedController;
			}
			else
			{
				AxisButton.interactable = false;
				ControllerName = string.Empty;
				Assignment.Controller = Controller.None;
			}
		}
	}

	public bool IsValid()
	{
		return SelectedController != Controller.None;
	}

	public void OnSetAxis()
	{
		if (InputAxisWindow.ShowInputPanel(this))
		{
			InputAxisWindow.OnSubmit += Set;
		}
	}

	private void Set(ControllerAxis result)
	{
		AxisKey = result;
		AxisButtonText.text = result.ToString();
		Assignment.Set(SelectedController, AxisKey);
		AxisSlider.gameObject.SetActive(AxisKey != ControllerAxis.None);
	}

	public void Load(ControllerAssignment assignment)
	{
		if (!_initialized)
		{
			Initialize();
		}
		ControllerDropdown.value = (int)assignment.Controller;
		Set(assignment.Axis);
	}

	private void Update()
	{
		if (IsVisible && IsValid() && AxisKey != ControllerAxis.None && !WorldManager.IsGamePaused)
		{
			AxisSlider.value = Assignment.Output * 100f;
		}
	}

	public void Created(string assignmentName, ControllerAssignment assignment)
	{
		AxisName = assignmentName;
		AssignmentText.text = assignmentName.ToProper();
		AxisButton.interactable = false;
		base.name = "Controller" + assignmentName;
		Assignment = assignment;
		assignment.Link(this);
	}
}
