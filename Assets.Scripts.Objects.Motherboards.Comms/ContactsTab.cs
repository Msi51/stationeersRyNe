using System;
using System.Collections.Generic;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI.CustomScrollPanel;
using Assets.Scripts.UI.Motherboard;
using TMPro;
using UI.Motherboard;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards.Comms;

public class ContactsTab : MonoBehaviour, IScrollHandler, IEventSystemHandler
{
	[SerializeField]
	private ScrollPanel _scrollPanel;

	[SerializeField]
	private TextMeshProUGUI _satelliteText;

	[SerializeField]
	private RocketToggleGroup _satelliteToggleGroup;

	[SerializeField]
	private TextMeshProUGUI _landingPadText;

	[SerializeField]
	private RocketToggleGroup _padToggleGroup;

	[SerializeField]
	private VerticalLayoutGroup _contactListVerticalLayout;

	[SerializeField]
	private ScreenContact _contactItemPrefab;

	[SerializeField]
	private RectTransform _contactItemParent;

	private List<ScreenContact> _contactItems;

	private CommsMotherboard _motherboard;

	public List<ScreenContact> ContactItems => _contactItems;

	public void Initialise(CommsMotherboard motherboard)
	{
		_motherboard = motherboard;
		InitialiseToggleGroups();
		_contactItems = new List<ScreenContact>();
		for (int i = 0; i < ContactSlot.ContactSlots.Count; i++)
		{
			ScreenContact screenContact = UnityEngine.Object.Instantiate(_contactItemPrefab, _contactItemParent);
			screenContact.Assign(_motherboard);
			_contactItems.Add(screenContact);
		}
		RefreshScrollPanel();
	}

	public void Show()
	{
		base.gameObject.SetActive(value: true);
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}

	public void Clear()
	{
		_contactItems.Clear();
	}

	public void AddContactItem()
	{
		ScreenContact screenContact = UnityEngine.Object.Instantiate(_contactItemPrefab, _contactItemParent);
		screenContact.Assign(_motherboard);
		_contactItems.Add(screenContact);
	}

	public void ClearAllContactItems()
	{
		foreach (ScreenContact contactItem in _contactItems)
		{
			contactItem.Clear();
		}
	}

	public void UpdateSatelliteToggleGroup(int count, int index)
	{
		_satelliteToggleGroup.EnableCount(count);
		_satelliteToggleGroup.Select(index);
	}

	public void UpdatePadToggleGroup(int count, int index)
	{
		_padToggleGroup.EnableCount(count);
		_padToggleGroup.Select(index);
	}

	public void SelectSatellite(int index)
	{
		_satelliteToggleGroup.Select(index);
	}

	public void SelectPad(int index)
	{
		_padToggleGroup.Select(index);
	}

	public void UpdateSatelliteText(string text)
	{
		_satelliteText.text = text;
	}

	public void UpdatePadText(string text)
	{
		_landingPadText.text = text;
	}

	public void InitialiseToggleGroups()
	{
		_satelliteToggleGroup.Init();
		_satelliteToggleGroup.Select(0, force: true);
		_padToggleGroup.Init();
		_padToggleGroup.Select(0, force: true);
	}

	private void DishToggleClicked(int index)
	{
		if (_motherboard.SelectedDishIndex != index)
		{
			Motherboard.UseComputer(19, _motherboard.netId, _motherboard.netId, index, sendToAll: true);
		}
	}

	private void PadToggleClicked(int index)
	{
		if (_motherboard.SelectedPadIndex != index)
		{
			Motherboard.UseComputer(18, _motherboard.netId, _motherboard.netId, index, sendToAll: true);
		}
	}

	public void RefreshScrollPanel()
	{
		if ((object)_scrollPanel != null)
		{
			_scrollPanel.SetContentHeight(_contactListVerticalLayout.preferredHeight);
		}
	}

	private void OnEnable()
	{
		RocketToggleGroup padToggleGroup = _padToggleGroup;
		padToggleGroup.OnIndexSelected = (Action<int>)Delegate.Combine(padToggleGroup.OnIndexSelected, new Action<int>(PadToggleClicked));
		RocketToggleGroup satelliteToggleGroup = _satelliteToggleGroup;
		satelliteToggleGroup.OnIndexSelected = (Action<int>)Delegate.Combine(satelliteToggleGroup.OnIndexSelected, new Action<int>(DishToggleClicked));
	}

	private void OnDisable()
	{
		RocketToggleGroup padToggleGroup = _padToggleGroup;
		padToggleGroup.OnIndexSelected = (Action<int>)Delegate.Remove(padToggleGroup.OnIndexSelected, new Action<int>(PadToggleClicked));
		RocketToggleGroup satelliteToggleGroup = _satelliteToggleGroup;
		satelliteToggleGroup.OnIndexSelected = (Action<int>)Delegate.Remove(satelliteToggleGroup.OnIndexSelected, new Action<int>(DishToggleClicked));
	}

	public void OnScroll(PointerEventData eventData)
	{
		_scrollPanel.OnScroll(eventData.scrollDelta);
	}
}
