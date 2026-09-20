using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class OnOffButton : UserInterfaceBase, IPointerDownHandler, IEventSystemHandler
{
	[SerializeField]
	private RectTransform _background;

	[SerializeField]
	private RectTransform _switch;

	[SerializeField]
	private Image _switchImage;

	[Space(15f)]
	[SerializeField]
	private Color _onColor;

	[SerializeField]
	private Color _offColor;

	[Space(15f)]
	[SerializeField]
	private float _borderSize;

	[SerializeField]
	private float _switchInsetSize;

	private bool _isOn;

	public Action<bool> OnClick;

	public bool IsOn
	{
		get
		{
			return _isOn;
		}
		set
		{
			_isOn = value;
			UpdateDisplay();
		}
	}

	public void Toggle()
	{
		IsOn = !IsOn;
	}

	public void Refresh()
	{
		UpdateDisplay();
	}

	private void UpdateDisplay()
	{
		if (IsOn)
		{
			_switch.localPosition = Vector3.left * Mathf.Abs(_switch.localPosition.x);
			_switchImage.color = _onColor;
		}
		else
		{
			_switch.localPosition = Vector3.right * Mathf.Abs(_switch.localPosition.x);
			_switchImage.color = _offColor;
		}
	}

	public void Autosize()
	{
		float num = RectTransform.sizeDelta.x / 2f - _borderSize + _switchInsetSize;
		float y = RectTransform.sizeDelta.y - _borderSize * 2f + _switchInsetSize * 2f;
		_switch.localPosition = new Vector2((0f - num) / 2f, 0f);
		_switch.sizeDelta = new Vector2(num, y);
		_background.offsetMax = new Vector2(0f - _borderSize, 0f - _borderSize);
		_background.offsetMin = new Vector2(_borderSize, _borderSize);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		IsOn = !IsOn;
		if (OnClick != null)
		{
			OnClick(IsOn);
		}
	}
}
