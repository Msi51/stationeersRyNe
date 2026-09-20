using UnityEngine;
using UnityEngine.Events;

namespace TraderUI;

public class TabWell : GameBase
{
	[HideInInspector]
	public UnityEvent<int> TabSelected;

	[SerializeField]
	private Color _selectedColor;

	[SerializeField]
	private Color _hoveredColor;

	[SerializeField]
	private Color _unSelectedColor;

	[SerializeField]
	private Color _disabledColor;

	[Space(15f)]
	[SerializeField]
	private Color _disabledTextColor;

	[SerializeField]
	private Color _enabledTextColor;

	[Space(15f)]
	[SerializeField]
	private Vector2 _tabSize;

	[SerializeField]
	private float _tabUnderlineHeight;

	private TabWellTab[] _tabs;

	private int _selectedIndex;

	public int SelectedIndex => _selectedIndex;

	private void Awake()
	{
		Initialise();
	}

	public void Initialise()
	{
		_tabs = GetComponentsInChildren<TabWellTab>();
		if (_tabs != null && _tabs.Length != 0)
		{
			int num = 0;
			TabWellTab[] tabs = _tabs;
			foreach (TabWellTab obj in tabs)
			{
				obj.Initialise(num, this);
				obj.SetColor(_unSelectedColor);
				obj.SetTransform(_tabSize.x * (float)num, _tabSize.x, _tabSize.y - _tabUnderlineHeight);
				num++;
			}
			(Transform as RectTransform).sizeDelta = new Vector2(_tabSize.x * (float)_tabs.Length, _tabSize.y);
			_tabs[0].SetColor(_selectedColor);
		}
	}

	public void PointerEnter(int index)
	{
		if (_selectedIndex != index)
		{
			_tabs[index].SetColor(_hoveredColor);
		}
	}

	public void PointerExit(int index)
	{
		if (_selectedIndex != index)
		{
			_tabs[index].SetColor(_unSelectedColor);
		}
	}

	public void SetEnabled(int index, bool enabled)
	{
		TabWellTab tabWellTab = _tabs[index];
		if (tabWellTab.IsEnabled != enabled)
		{
			tabWellTab.SetEnabled(enabled);
			SetTabColors(tabWellTab, _selectedIndex == index);
		}
	}

	public void Select(int index, bool invokeEvent = true)
	{
		if (_tabs == null)
		{
			return;
		}
		TabWellTab tab = _tabs[_selectedIndex];
		TabWellTab tabWellTab = _tabs[index];
		if (tabWellTab.IsEnabled)
		{
			SetTabColors(tab, selected: false);
			SetTabColors(tabWellTab, selected: true);
			_selectedIndex = index;
			if (invokeEvent)
			{
				TabSelected?.Invoke(index);
			}
		}
	}

	public void EnsureEnabledTabIsSelected()
	{
		if (_tabs[_selectedIndex].IsEnabled)
		{
			Select(_selectedIndex);
			return;
		}
		for (int i = 1; i <= _tabs.Length; i++)
		{
			int num = (int)Mathf.Repeat(_selectedIndex - i, _tabs.Length);
			if (_tabs[num].IsEnabled)
			{
				Select(num);
				break;
			}
		}
	}

	private void SetTabColors(TabWellTab tab, bool selected)
	{
		if (tab.IsEnabled)
		{
			tab.SetColor(selected ? _selectedColor : _unSelectedColor);
			tab.SetTextColor(_enabledTextColor);
		}
		else
		{
			tab.SetColor(_disabledColor);
			tab.SetTextColor(_disabledTextColor);
		}
	}
}
