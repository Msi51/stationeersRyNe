using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Objects;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class PanelToolTip : PanelToolTipScreenSpace
{
	public static PanelToolTip Instance;

	public Thing ToolTipThing;

	private readonly StringBuilder stringer = new StringBuilder();

	private const string _stringSpliter = ", ";

	public override void Initialize()
	{
		base.Initialize();
		Instance = this;
	}

	public void SetUpTooltip(string title, string description, IScreenSpaceTooltip screenSpaceTooltip)
	{
		_tooltipToUpdate = screenSpaceTooltip;
		SetUpTooltip(title, description);
	}

	public void SetUpTooltip(string title, string description)
	{
		SetVisible(isVisble: true);
		ToolTipThing = null;
		Information.gameObject.SetActive(!string.IsNullOrEmpty(description));
		ToolTipItemName.SetText(title);
		Information.SetText(description);
		LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
	}

	public void SetUpTooltip(Thing thing)
	{
		ToolTipThing = thing;
		RefreshText();
		GraphicRayCast.enabled = false;
		SetVisible(isVisble: true);
		LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
	}

	public override void ClearToolTip()
	{
		base.ClearToolTip();
		ToolTipThing = null;
	}

	public override void RefreshText()
	{
		base.RefreshText();
		if (ToolTipThing != null)
		{
			ToolTipItemName.SetText(ToolTipThing.DisplayName);
			string text = ToolTipThing.GetExtendedText().ToString();
			Information.SetText(text);
			Information.gameObject.SetActive(!string.IsNullOrEmpty(text));
		}
	}

	public void SetInfoText(string text)
	{
		Information.SetText(text);
	}

	public string StringListToString(List<string> list, bool isResearchName, string defaultString = "No Requirements")
	{
		if (list == null || list.Count <= 0)
		{
			return defaultString;
		}
		stringer.Clear();
		foreach (string item in list)
		{
			if (!item.Equals(string.Empty))
			{
				if (!isResearchName)
				{
					stringer.Append(Localization.GetThingName(item));
					stringer.Append(", ");
				}
				else
				{
					stringer.Append(item);
					stringer.Append(", ");
				}
			}
		}
		if (stringer.Length > 2)
		{
			stringer.Length -= 2;
		}
		else
		{
			stringer.Clear();
			stringer.Append(defaultString);
		}
		return stringer.ToString();
	}
}
