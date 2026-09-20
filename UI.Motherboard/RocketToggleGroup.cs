using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Motherboard;

public class RocketToggleGroup : GameBase
{
	public Action<int> OnIndexSelected;

	[SerializeField]
	private List<RocketToggle> toggles = new List<RocketToggle>();

	public int SelectedIndex { get; private set; }

	public void Select(int index, bool force = false, bool mute = false)
	{
		if (!force && SelectedIndex == index)
		{
			return;
		}
		foreach (RocketToggle toggle in toggles)
		{
			toggle.IsSelected = toggle.Index == index;
		}
		SelectedIndex = index;
		if (!mute)
		{
			OnIndexSelected?.Invoke(index);
		}
	}

	public void Enable(int index, string tooltipText)
	{
		toggles[index].SetActive(active: true);
		toggles[index].SetTooltip(tooltipText);
	}

	public void DisableAll()
	{
		foreach (RocketToggle toggle in toggles)
		{
			toggle.SetActive(active: false);
		}
	}

	public void EnableCount(int toEnable)
	{
		for (int i = 0; i < toggles.Count; i++)
		{
			toggles[i].SetActive(i < toEnable);
		}
	}

	public void Init()
	{
		foreach (RocketToggle toggle in toggles)
		{
			toggle.Init(this);
		}
	}
}
