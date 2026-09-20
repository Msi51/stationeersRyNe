using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.UI.CustomScrollPanel;
using Cysharp.Threading.Tasks;
using Objects.Rockets.UI.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Objects.Rockets.UI;

public class LogicValueListPanel : UserInterfaceBase, IScrollHandler, IEventSystemHandler
{
	[SerializeField]
	private TextMeshProUGUI _selectedDeviceTextMesh;

	[SerializeField]
	private LogicValueDisplay _logicValueDisplayPrefab;

	[Space(15f)]
	[SerializeField]
	private ScrollPanel _scrollPanel;

	[SerializeField]
	private VerticalLayoutGroup _logicValueListVerticalLayout;

	[Space(15f)]
	[SerializeField]
	private PinButton _pinButton;

	[SerializeField]
	private GameObject _pinSeparator;

	[SerializeField]
	private Transform _logicValuePinnedParent;

	[SerializeField]
	private Transform _logicValueUnpinnedParent;

	[SerializeField]
	private RectTransform _hoverOutline;

	private List<LogicValueDisplay> _logicValueDisplays = new List<LogicValueDisplay>();

	private RocketMotherboard _motherboard;

	private LogicValueDisplay _currentlyHoveringOver;

	private Device _selectedDevice;

	public void Initialize()
	{
		for (int i = 0; i < 200; i++)
		{
			LogicValueDisplay logicValueDisplay = UnityEngine.Object.Instantiate(_logicValueDisplayPrefab, _logicValueUnpinnedParent);
			logicValueDisplay.SetVisible(isVisble: false);
			_logicValueDisplays.Add(logicValueDisplay);
		}
	}

	public void Show(RocketMotherboard motherboard)
	{
		_motherboard = motherboard;
	}

	public void Refresh(RocketUIModel model)
	{
		int count = model.LogicControlModel.LogicValues.Count;
		if (count > _logicValueDisplays.Count)
		{
			ConsoleWindow.PrintError("Not enough game objects available to show all logic values!");
			return;
		}
		foreach (DeviceModel device in model.LogicControlModel.Devices)
		{
			if (device.ReferenceId == model.LogicControlModel.SelectedDeviceReferenceId)
			{
				_selectedDeviceTextMesh.text = device.DisplayName;
				break;
			}
		}
		for (int i = 0; i < _logicValueDisplays.Count; i++)
		{
			LogicValueDisplay logicValueDisplay = _logicValueDisplays[i];
			if (i < count)
			{
				logicValueDisplay.SetVisible(isVisble: true);
				LogicValueModel model2 = model.LogicControlModel.LogicValues[i];
				logicValueDisplay.Initialize(model2, this, _motherboard);
				Transform parent = (model2.Pinned ? _logicValuePinnedParent : _logicValueUnpinnedParent);
				logicValueDisplay.Transform.SetParent(parent, worldPositionStays: false);
				logicValueDisplay.Transform.localScale = Vector3.one;
			}
			else
			{
				logicValueDisplay.Transform.SetParent(_logicValueUnpinnedParent, worldPositionStays: false);
				logicValueDisplay.SetVisible(isVisble: false);
			}
		}
		RefreshPanelAfterFrame().Forget();
		ShowHidePinSeparator();
	}

	private async UniTaskVoid RefreshPanelAfterFrame()
	{
		await UniTask.WaitForEndOfFrame();
		RefreshScrollPanel();
	}

	private void RefreshScrollPanel()
	{
		_scrollPanel.SetContentHeight(_logicValueListVerticalLayout.preferredHeight);
	}

	public void LogicValueTextBeginHover(LogicValueDisplay logicValueDisplay)
	{
		_currentlyHoveringOver = logicValueDisplay;
		SetOutlinePosition(_hoverOutline, logicValueDisplay.Transform);
		_hoverOutline.gameObject.SetActive(value: true);
		_pinButton.SetColor(logicValueDisplay.LogicValueModel.Pinned);
	}

	public void LogicValueTextEndHover()
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

	private void LogicValuePinned()
	{
		if (!(_currentlyHoveringOver == null))
		{
			_motherboard.ToggleLogicValuePinState(_currentlyHoveringOver.LogicValueModel.DeviceReferenceId, _currentlyHoveringOver.LogicType);
		}
	}

	private void ShowHidePinSeparator()
	{
		bool activeInHierarchy = _pinSeparator.activeInHierarchy;
		bool flag = _logicValuePinnedParent.childCount > 0;
		if (activeInHierarchy != flag)
		{
			_pinSeparator.SetActive(flag);
			LayoutRebuilder.ForceRebuildLayoutImmediate(_logicValueListVerticalLayout.transform as RectTransform);
			RefreshScrollPanel();
		}
	}

	private new void OnEnable()
	{
		PinButton pinButton = _pinButton;
		pinButton.OnClick = (Action)Delegate.Combine(pinButton.OnClick, new Action(LogicValuePinned));
	}

	private new void OnDisable()
	{
		PinButton pinButton = _pinButton;
		pinButton.OnClick = (Action)Delegate.Remove(pinButton.OnClick, new Action(LogicValuePinned));
	}

	public void OnScroll(PointerEventData eventData)
	{
		_scrollPanel.OnScroll(eventData.scrollDelta);
	}
}
