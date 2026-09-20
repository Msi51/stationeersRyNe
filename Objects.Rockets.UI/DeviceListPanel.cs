using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.UI.CustomScrollPanel;
using Objects.Rockets.UI.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Objects.Rockets.UI;

public class DeviceListPanel : UserInterfaceBase, IScrollHandler, IEventSystemHandler, IDeviceTextActionHandler
{
	[Header("Device List Panel")]
	[SerializeField]
	private DeviceText _deviceTextPrefab;

	[SerializeField]
	private Transform _deviceListUnpinnedParent;

	[SerializeField]
	private Transform _deviceListPinnedParent;

	[SerializeField]
	private RectTransform _selectionOutline;

	[SerializeField]
	private RectTransform _hoverOutline;

	[Space(15f)]
	[SerializeField]
	private PinButton _pinButton;

	[SerializeField]
	private GameObject _pinSeparator;

	[Space(15f)]
	[SerializeField]
	private ScrollPanel _scrollPanel;

	[SerializeField]
	private VerticalLayoutGroup _deviceListVerticalLayout;

	private List<DeviceText> _deviceTextList = new List<DeviceText>();

	private DeviceText _currentlyHoveringOver;

	private RocketMotherboard _motherboard;

	public void Initialize()
	{
		for (int i = 0; i < 100; i++)
		{
			DeviceText deviceText = UnityEngine.Object.Instantiate(_deviceTextPrefab, _deviceListUnpinnedParent);
			deviceText.SetVisible(isVisble: false);
			_deviceTextList.Add(deviceText);
		}
	}

	public void Show(RocketMotherboard motherboard)
	{
		_motherboard = motherboard;
	}

	public void DeviceTextClicked(DeviceText deviceText)
	{
		_motherboard.DeviceSelected(deviceText.DeviceModel.ReferenceId);
	}

	public void DeviceTextBeginHover(DeviceText deviceText)
	{
		_currentlyHoveringOver = deviceText;
		SetOutlinePosition(_hoverOutline, deviceText.Transform);
		_hoverOutline.gameObject.SetActive(value: true);
		_pinButton.SetColor(deviceText.DeviceModel.Pinned);
	}

	public void DeviceTextEndHover()
	{
		_currentlyHoveringOver = null;
		_hoverOutline.gameObject.SetActive(value: false);
	}

	private void SetOutlinePosition(RectTransform rectTransform, Transform parent)
	{
		rectTransform.SetParent(parent, worldPositionStays: false);
		rectTransform.offsetMax = Vector2.zero;
		rectTransform.offsetMin = Vector2.zero;
	}

	private void SetSelection()
	{
		foreach (DeviceText deviceText in _deviceTextList)
		{
			if (deviceText.DeviceModel.ReferenceId != 0L && deviceText.DeviceModel.ReferenceId == _motherboard.SelectedLogicable().ReferenceId)
			{
				SetOutlinePosition(_selectionOutline, deviceText.Transform);
				break;
			}
		}
	}

	public void Refresh(RocketUIModel model)
	{
		int count = model.LogicControlModel.Devices.Count;
		if (count > _deviceTextList.Count)
		{
			ConsoleWindow.PrintError("Not enough game objects available to show all devices!");
			return;
		}
		for (int i = 0; i < _deviceTextList.Count; i++)
		{
			DeviceText deviceText = _deviceTextList[i];
			if (i < count)
			{
				deviceText.SetVisible(isVisble: true);
				DeviceModel deviceModel = model.LogicControlModel.Devices[i];
				deviceText.Initialize(deviceModel, this, _motherboard);
				deviceText.Refresh();
				Transform parent = (deviceModel.Pinned ? _deviceListPinnedParent : _deviceListUnpinnedParent);
				deviceText.Transform.SetParent(parent, worldPositionStays: false);
				deviceText.Transform.localScale = Vector3.one;
			}
			else
			{
				deviceText.Transform.SetParent(_deviceListUnpinnedParent, worldPositionStays: false);
				deviceText.SetVisible(isVisble: false);
			}
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(_deviceListVerticalLayout.transform as RectTransform);
		RefreshScrollPanel();
		SetSelection();
		ShowHidePinSeparator();
	}

	private void RefreshScrollPanel()
	{
		_scrollPanel.SetContentHeight(_deviceListVerticalLayout.preferredHeight);
	}

	private void DevicePinned()
	{
		if (!(_currentlyHoveringOver == null))
		{
			_motherboard.ToggleDevicePinState(_currentlyHoveringOver.DeviceModel.ReferenceId);
		}
	}

	private void ShowHidePinSeparator()
	{
		bool activeInHierarchy = _pinSeparator.activeInHierarchy;
		bool flag = _deviceListPinnedParent.childCount > 0;
		if (activeInHierarchy != flag)
		{
			_pinSeparator.SetActive(flag);
			LayoutRebuilder.ForceRebuildLayoutImmediate(_deviceListVerticalLayout.transform as RectTransform);
			RefreshScrollPanel();
		}
	}

	private new void OnEnable()
	{
		PinButton pinButton = _pinButton;
		pinButton.OnClick = (Action)Delegate.Combine(pinButton.OnClick, new Action(DevicePinned));
	}

	private new void OnDisable()
	{
		PinButton pinButton = _pinButton;
		pinButton.OnClick = (Action)Delegate.Remove(pinButton.OnClick, new Action(DevicePinned));
	}

	public void OnScroll(PointerEventData eventData)
	{
		_scrollPanel.OnScroll(eventData.scrollDelta);
	}
}
