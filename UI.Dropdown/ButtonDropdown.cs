using System.Collections.Generic;
using Assets.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dropdown;

public class ButtonDropdown : UserInterfaceBase
{
	[Header("Button Dropdown")]
	[SerializeField]
	private ButtonDropdownItem _buttonPrefab;

	[SerializeField]
	private RectTransform _contentParentTransform;

	[SerializeField]
	private GameObject _mainPanelGameObject;

	[SerializeField]
	private RectTransform _mainPanelRectTransform;

	[SerializeField]
	private RectTransform _scrollPanelTransform;

	[SerializeField]
	private GameObject _scrollBarGameObject;

	[Space(20f)]
	[SerializeField]
	private TextMeshProUGUI _selectedTextMesh;

	[SerializeField]
	private Button _dropdownButton;

	[Space(20f)]
	[SerializeField]
	private Image _arrowImage;

	[SerializeField]
	private Sprite _arrowUp;

	[SerializeField]
	private Sprite _arrowDown;

	private List<ButtonDropdownItem> _items = new List<ButtonDropdownItem>(20);

	private bool _refreshOnExpand;

	public int SelectedIndex { get; private set; } = -1;

	private bool Expanded => _mainPanelGameObject.activeInHierarchy;

	public void SetSelectedIndex(int index)
	{
		SelectedIndex = index;
	}

	public void Clear()
	{
		foreach (ButtonDropdownItem item in _items)
		{
			Object.Destroy(item.GameObject);
		}
		_items.Clear();
		SetDropdownExpanded(expanded: false);
		SelectedIndex = -1;
		_selectedTextMesh.text = "No chip selected";
	}

	public void AddItem(string title)
	{
		ButtonDropdownItem buttonDropdownItem = Object.Instantiate(_buttonPrefab, _contentParentTransform);
		buttonDropdownItem.Initialize(title, _items.Count, this);
		_items.Add(buttonDropdownItem);
	}

	public void ItemsChanged()
	{
		_refreshOnExpand = true;
		if (_items.Count > 0 && SelectedIndex == -1)
		{
			ItemClicked(0);
		}
	}

	public void ItemClicked(int index)
	{
		if (index < _items.Count)
		{
			ButtonDropdownItem buttonDropdownItem = _items[index];
			_selectedTextMesh.text = buttonDropdownItem.Title;
			SelectedIndex = index;
			SetDropdownExpanded(expanded: false);
		}
	}

	private void RefreshSize()
	{
		int num = 50;
		int max = 250;
		int num2 = _items.Count * num;
		int num3 = Mathf.Clamp(num2, num, max);
		int num4 = ((num2 > num3) ? (-20) : 0);
		_scrollPanelTransform.offsetMax = new Vector2(num4, 0f);
		int num5 = Mathf.Clamp(_items.Count * 50, 50, 250);
		_mainPanelRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num5);
	}

	private void ToggleDropdown()
	{
		SetDropdownExpanded(!Expanded);
	}

	private void SetDropdownExpanded(bool expanded)
	{
		_mainPanelGameObject.SetActive(expanded);
		_arrowImage.sprite = (expanded ? _arrowUp : _arrowDown);
		if (expanded && _refreshOnExpand)
		{
			_refreshOnExpand = false;
			RefreshSize();
		}
	}

	private new void OnEnable()
	{
		_mainPanelGameObject.SetActive(value: false);
		_dropdownButton.onClick.AddListener(ToggleDropdown);
	}

	private new void OnDisable()
	{
		_dropdownButton.onClick.RemoveListener(ToggleDropdown);
	}
}
