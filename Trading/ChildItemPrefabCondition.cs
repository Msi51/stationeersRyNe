using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;
using UnityEngine;

namespace Trading;

[XmlType("Item")]
public class ChildItemPrefabCondition : ThingPrefabCondition
{
	[XmlAttribute("SlotId")]
	public string SlotId;

	[XmlAttribute("SlotIndex")]
	public int SlotIndex = -1;

	[XmlIgnore]
	public int SlotIdHash;

	public override string DebugName
	{
		get
		{
			if (SlotIndex > 0)
			{
				return $"Item {PrefabName} in slot {SlotIndex}";
			}
			if (!string.IsNullOrEmpty(SlotId))
			{
				return "Item " + PrefabName + " in " + SlotId;
			}
			return "Item " + PrefabName;
		}
	}

	public override int GetChecksum()
	{
		return (((base.GetChecksum() ^ ((!string.IsNullOrEmpty(SlotId)) ? Animator.StringToHash(SlotId) : 0)) * 41) ^ SlotIndex) * 41;
	}

	public override void Initialise()
	{
		base.Initialise();
		if (!string.IsNullOrEmpty(PrefabName))
		{
			PrefabNameHash = Animator.StringToHash(PrefabName);
		}
		if (!string.IsNullOrEmpty(SlotId))
		{
			SlotIdHash = Animator.StringToHash(SlotId);
		}
	}

	public override bool Evaluate<T>(T t)
	{
		bool result = false;
		DynamicThing childItem = null;
		if (t is Thing thing)
		{
			if (thing.PrefabHash == PrefabNameHash)
			{
				result = true;
			}
			else
			{
				result = (string.IsNullOrEmpty(SlotId) ? thing.HasChild<DynamicThing>(PrefabNameHash, out childItem) : thing.HasChild<DynamicThing>(PrefabNameHash, SlotIdHash, out childItem));
				result = ((SlotIndex > 0) ? thing.HasChild<DynamicThing>(PrefabNameHash, SlotIndex, out childItem) : result);
			}
		}
		foreach (ConditionData condition in Conditions)
		{
			if (!condition.Evaluate(childItem))
			{
				return false;
			}
		}
		foreach (ConditionDataCollection conditionCollection in ConditionCollections)
		{
			if (!conditionCollection.Evaluate(childItem))
			{
				return false;
			}
		}
		return result;
	}

	protected override void AppendToolTip(StringBuilder stringBuilder, int generations)
	{
		if (!Prefab.TryFind(PrefabNameHash, out var thing))
		{
			ConsoleWindow.PrintError("Cannot find prefab " + PrefabName + " for child condition.");
			return;
		}
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		if (SlotIndex > 0)
		{
			stringBuilder.AppendLine(GameStrings.TradeChildItemInSlot.AsString(thing.ToTooltip(), SlotIndex.ToString().AsColor("orange")));
		}
		else if (!string.IsNullOrEmpty(SlotId))
		{
			stringBuilder.AppendLine(GameStrings.TradeChildItemInSlot.AsString(thing.ToTooltip(), Localization.GetSlotName(SlotId).AsColor("orange")));
		}
		else
		{
			stringBuilder.AppendLine(GameStrings.TradeChildItem.AsString(thing.ToTooltip()));
		}
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
