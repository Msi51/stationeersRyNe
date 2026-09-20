using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class DynamicSettingItem : MonoBehaviour
{
	public enum UIType
	{
		Dropdown,
		Toggle,
		Slider,
		InputField
	}

	[Header("References")]
	[SerializeField]
	private TMP_Dropdown _dropdown;

	[SerializeField]
	private Toggle _toggle;

	[SerializeField]
	private Slider _slider;

	[SerializeField]
	private TMP_InputField _inputField;

	[Header("Options")]
	public UIType _uiType;

	private void OnEnable()
	{
		SetUiObject();
	}

	public void SetUiObject()
	{
		_dropdown.gameObject.SetActive(_uiType == UIType.Dropdown);
		_toggle.gameObject.SetActive(_uiType == UIType.Toggle);
		_slider.gameObject.SetActive(_uiType == UIType.Slider);
		_inputField.gameObject.SetActive(_uiType == UIType.InputField);
	}

	public void SetValue(object value)
	{
		switch (_uiType)
		{
		case UIType.Dropdown:
			_dropdown.value = (int)value;
			break;
		case UIType.Toggle:
			_toggle.isOn = (bool)value;
			break;
		case UIType.Slider:
			_slider.value = (float)value;
			break;
		case UIType.InputField:
			_inputField.text = (string)value;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
