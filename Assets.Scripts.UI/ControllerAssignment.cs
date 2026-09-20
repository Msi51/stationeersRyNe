using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts.UI;

public class ControllerAssignment
{
	public ControllerAxisItem SettingItem;

	private float _modifier = 1f;

	private Controller _controller;

	private ControllerAxis _axis;

	public string InputAxis;

	public string ControllerName;

	public Controller Controller
	{
		get
		{
			return _controller;
		}
		set
		{
			_controller = value;
			SetName();
		}
	}

	public ControllerAxis Axis
	{
		get
		{
			return _axis;
		}
		set
		{
			_axis = value;
		}
	}

	public bool IsValid
	{
		get
		{
			if (_controller != Controller.None)
			{
				return _axis != ControllerAxis.None;
			}
			return false;
		}
	}

	public float Output => KeyManager.GetAxis(this) * _modifier;

	public ControllerAssignment(float modifier)
	{
		_modifier = modifier;
	}

	public void Link(ControllerAxisItem item)
	{
		SettingItem = item;
		SettingItem.Load(this);
	}

	public void Set(Controller controller, ControllerAxis axis)
	{
		Controller = controller;
		Axis = axis;
		InputAxis = $"{_controller}{_axis}";
	}

	public ControllerData Serialize()
	{
		return new ControllerData
		{
			Controller = Controller,
			Axis = Axis,
			ControllerName = ControllerName
		};
	}

	public void SetName()
	{
		string[] joystickNames = Input.GetJoystickNames();
		int num = (int)(Controller - 1);
		if (num >= joystickNames.Length || num < 0)
		{
			ControllerName = string.Empty;
		}
		else
		{
			ControllerName = joystickNames[num];
		}
	}

	public void Deserialize(ControllerData currentData)
	{
		if (currentData == null)
		{
			return;
		}
		string[] joystickNames = Input.GetJoystickNames();
		for (int i = 0; i < joystickNames.Length; i++)
		{
			if (!(joystickNames[i] != currentData.ControllerName))
			{
				Controller controller = (Controller)(i + 1);
				Set(controller, currentData.Axis);
				if (SettingItem != null)
				{
					SettingItem.Load(this);
				}
			}
		}
	}
}
