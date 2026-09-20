using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UI.Tooltips;

public static class UITooltipManager
{
	private static readonly List<UITooltip> _tooltips = new List<UITooltip>();

	private static string _anonymousTooltip;

	public static string GetAnonymousTooltip()
	{
		return _anonymousTooltip;
	}

	public static void SetTooltip(string text)
	{
		_anonymousTooltip = text;
	}

	public static void ClearTooltip()
	{
		_anonymousTooltip = string.Empty;
	}

	public static void Register(UITooltip tooltip)
	{
		_tooltips.Add(tooltip);
	}

	public static void UnRegister(UITooltip tooltip)
	{
		_tooltips.Remove(tooltip);
	}

	public static bool Current(Vector2 cursorPosition, out UITooltip tooltip)
	{
		foreach (UITooltip tooltip2 in _tooltips)
		{
			if (RectTransformUtility.RectangleContainsScreenPoint(tooltip2.RectTransform, cursorPosition))
			{
				tooltip = tooltip2;
				return true;
			}
		}
		tooltip = null;
		return false;
	}

	public static void SetTooltip(StringBuilder text)
	{
		SetTooltip(text.ToString());
	}
}
