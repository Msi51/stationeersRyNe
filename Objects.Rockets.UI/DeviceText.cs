using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Objects.Rockets.UI.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Objects.Rockets.UI;

public class DeviceText : UserInterfaceBase, IPointerDownHandler, IEventSystemHandler
{
	[SerializeField]
	private DeviceOnOffButton _onOffButton;

	[SerializeField]
	private TextMeshProUGUI _textMesh;

	private IDeviceTextActionHandler _actionHandler;

	private DeviceModel _deviceModel;

	public DeviceModel DeviceModel => _deviceModel;

	public void Initialize(DeviceModel deviceModel, IDeviceTextActionHandler actionHandler, RocketMotherboard motherboard)
	{
		_deviceModel = deviceModel;
		_actionHandler = actionHandler;
		_onOffButton.SetDevice(deviceModel, motherboard);
		_textMesh.text = deviceModel.DisplayName;
		_onOffButton.SetActive(deviceModel.CanWriteOnOff);
	}

	public void Refresh()
	{
		_onOffButton.Refresh();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		_actionHandler.DeviceTextClicked(this);
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		_actionHandler.DeviceTextBeginHover(this);
		Device device = Thing.Find<Device>(_deviceModel.ReferenceId);
		if ((object)device != null)
		{
			PassiveUITooltip passiveUITooltip = device.GetPassiveUITooltip();
			PanelToolTip.Instance.SetUpTooltip(device.DisplayName, passiveUITooltip.Extended);
		}
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		_actionHandler.DeviceTextEndHover();
		PanelToolTip.Instance.ClearToolTip();
	}

	private new void OnDisable()
	{
		_actionHandler.DeviceTextEndHover();
	}
}
