using System;
using UnityEngine;

namespace ColorBlindUtility.UGUI;

[AddComponentMenu("UI/Color-Blind Support/Color-Blind Utility")]
public class ColorBlindUtility : MonoBehaviour
{
	public ColorBlindMode colorBlindMode;

	public Action<ColorBlindMode> OnColorBlindModeChangeEvent;

	private void OnEnable()
	{
		ColorBlindBase[] componentsInChildren = GetComponentsInChildren<ColorBlindBase>();
		foreach (ColorBlindBase colorBlindBase in componentsInChildren)
		{
			OnColorBlindModeChangeEvent = (Action<ColorBlindMode>)Delegate.Combine(OnColorBlindModeChangeEvent, new Action<ColorBlindMode>(colorBlindBase.Apply));
		}
	}

	private void OnDisable()
	{
		ColorBlindBase[] componentsInChildren = GetComponentsInChildren<ColorBlindBase>();
		foreach (ColorBlindBase colorBlindBase in componentsInChildren)
		{
			OnColorBlindModeChangeEvent = (Action<ColorBlindMode>)Delegate.Remove(OnColorBlindModeChangeEvent, new Action<ColorBlindMode>(colorBlindBase.Apply));
		}
	}

	public void SetColorBlindMode(int index)
	{
		SetColorBlindMode((ColorBlindMode)index);
	}

	public void SetColorBlindMode(ColorBlindMode mode)
	{
		colorBlindMode = mode;
		if (OnColorBlindModeChangeEvent != null)
		{
			OnColorBlindModeChangeEvent(colorBlindMode);
		}
	}
}
