using Assets.Scripts.Objects.Motherboards;
using Objects.Rockets.UI.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class DeviceOnOffButton : UserInterfaceBase, IPointerDownHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler
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
	private Color _onUnpoweredColor;

	[SerializeField]
	private Color _offColor;

	[SerializeField]
	private Color _errorColor;

	[Space(15f)]
	[SerializeField]
	private float _borderSize;

	[SerializeField]
	private float _switchInsetSize;

	private RocketMotherboard _motherboard;

	private DeviceModel _deviceModel;

	public void SetDevice(DeviceModel deviceModel, RocketMotherboard motherboard)
	{
		_deviceModel = deviceModel;
		_motherboard = motherboard;
	}

	public void Refresh()
	{
		SetSwitchState();
	}

	private void SetSwitchState()
	{
		if (_deviceModel.OnOff)
		{
			_switch.localPosition = Vector3.left * Mathf.Abs(_switch.localPosition.x);
			if (_deviceModel.Powered || !_deviceModel.HasPowerState)
			{
				_switchImage.color = (_deviceModel.Error ? _errorColor : _onColor);
			}
			else
			{
				_switchImage.color = _onUnpoweredColor;
			}
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
		_motherboard.LogicValueChanged((!_deviceModel.OnOff) ? 1 : 0, LogicType.On, _deviceModel.ReferenceId);
	}

	public new void OnPointerEnter(PointerEventData eventData)
	{
	}

	public new void OnPointerExit(PointerEventData eventData)
	{
	}
}
