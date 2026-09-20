using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects;

[Serializable]
public class ToolBasic
{
	[Header("Construction")]
	public Item ToolEntry;

	public Item ToolEntry2;

	[Range(0f, 60f)]
	public float EntryTime = 2f;

	public int EntryQuantity;

	public int EntryQuantity2;

	public virtual string ConstructionString => GameStrings.RequiredContinueConstruction.DisplayString;

	public bool IsToolEntry(Item tool)
	{
		if (ToolEntry == null || tool == null)
		{
			return false;
		}
		if ((bool)tool.ReplacementOf && tool.ReplacementOf.PrefabHash == ToolEntry.PrefabHash)
		{
			return true;
		}
		return tool.PrefabHash == ToolEntry.PrefabHash;
	}

	public bool IsToolEntry2(Item tool)
	{
		if (ToolEntry2 == null || tool == null)
		{
			return false;
		}
		if ((bool)tool.ReplacementOf && tool.ReplacementOf.PrefabHash == ToolEntry2.PrefabHash)
		{
			return true;
		}
		return tool.PrefabHash == ToolEntry2.PrefabHash;
	}

	public string GetToolsAsString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (ToolEntry is Stackable)
		{
			stringBuilder.Append("<color=yellow>" + StringManager.Get(EntryQuantity) + "</color> x " + ToolEntry.ToTooltip());
			AppendReplacements(stringBuilder, ToolEntry);
		}
		else if ((bool)ToolEntry)
		{
			stringBuilder.Append(ToolEntry.ToTooltip());
		}
		Stackable stackable = ToolEntry2 as Stackable;
		if (stringBuilder.Length > 0 && ((bool)stackable || (bool)ToolEntry2))
		{
			stringBuilder.Append(GameStrings.And);
		}
		if ((bool)stackable)
		{
			stringBuilder.Append("<color=yellow>" + StringManager.Get(EntryQuantity2) + "</color> x " + ToolEntry2.ToTooltip());
			AppendReplacements(stringBuilder, ToolEntry2);
		}
		else if ((bool)ToolEntry2)
		{
			stringBuilder.Append(ToolEntry2.ToTooltip());
		}
		if (stringBuilder.Length > 0)
		{
			stringBuilder.Append(" ");
			stringBuilder.Append(ConstructionString);
		}
		return stringBuilder.ToString();
	}

	private static void AppendReplacements(StringBuilder str, Item tool)
	{
		IReadOnlyList<Item> replacements = Item.GetReplacements(tool.PrefabHash);
		if (replacements == null || replacements.Count == 0)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < replacements.Count; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(", ");
			}
			stringBuilder.Append(replacements[i].ToTooltip());
		}
		GameStrings.OrSubstitute.AppendFormat(str, stringBuilder.ToString());
	}

	public string GetRepairsAsString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (ToolEntry is Stackable)
		{
			stringBuilder.Append("<color=yellow>" + StringManager.Get(EntryQuantity) + "</color> x " + ToolEntry.ToTooltip());
		}
		else if ((bool)ToolEntry)
		{
			stringBuilder.Append(ToolEntry.ToTooltip());
		}
		Stackable stackable = ToolEntry2 as Stackable;
		if (stringBuilder.Length > 0 && ((bool)stackable || (bool)ToolEntry2))
		{
			stringBuilder.Append(" and ");
		}
		if ((bool)stackable)
		{
			stringBuilder.Append("<color=yellow>" + StringManager.Get(EntryQuantity2) + " x</color>  " + ToolEntry2.ToTooltip());
		}
		else if ((bool)ToolEntry2)
		{
			stringBuilder.Append(ToolEntry2.ToTooltip());
		}
		if (stringBuilder.Length > 0)
		{
			stringBuilder.Append(" ");
			stringBuilder.Append(GameStrings.RequiredToRepair.DisplayString);
		}
		return stringBuilder.ToString();
	}

	public bool IsValid()
	{
		if (!(ToolEntry != null))
		{
			return ToolEntry2 != null;
		}
		return true;
	}
}
