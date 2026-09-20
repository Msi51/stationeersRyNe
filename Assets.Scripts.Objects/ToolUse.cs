using System;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects;

[Serializable]
public class ToolUse : ToolBasic
{
	[Tooltip("If true the entry tool will always be shown to player, regardless of if they have a tool or not")]
	public ToolUseType ToolUseType;

	[Header("Deconstruction")]
	public Item ToolExit;

	[Range(0f, 60f)]
	public float ExitTime = 2f;

	public int ExitQuantity;

	public override string ConstructionString
	{
		get
		{
			if (ToolUseType == ToolUseType.Upgrade)
			{
				return GameStrings.RequiredUpgradeDevice.DisplayString;
			}
			return GameStrings.RequiredContinueConstruction.DisplayString;
		}
	}

	public bool IsToolExit(Item tool)
	{
		if (ToolExit == null || tool == null)
		{
			return false;
		}
		if ((bool)tool.ReplacementOf && tool.ReplacementOf.PrefabHash == ToolExit.PrefabHash)
		{
			return true;
		}
		return tool.PrefabHash == ToolExit.PrefabHash;
	}

	private void SpawnItem(ConstructionEventInstance eventInstance, Item tool)
	{
		if (tool == null)
		{
			return;
		}
		int num = ((tool == ToolEntry) ? EntryQuantity : EntryQuantity2);
		if (tool is Tool || num == 0)
		{
			return;
		}
		Item item = OnServer.CreateOrStack(tool, num, eventInstance.Position, eventInstance.Rotation, eventInstance.OtherHandSlot);
		if (!item)
		{
			return;
		}
		if (item.ParentSlot == null && eventInstance.OtherHandSlot?.Parent is Human human)
		{
			for (int i = 0; i < human.Slots.Count && (!(human.Slots[i].Get() is WearableItem wearableItem) || !wearableItem.TryCollect(item)); i++)
			{
			}
		}
		if (item is IConstructionKit && (eventInstance.Parent is DraggableThing || eventInstance.Parent is Structure { CurrentBuildStateIndex: <=0 }) && (object)eventInstance.Parent.PaintableMaterial != null && eventInstance.Parent.CustomColor.Index != item.CustomColor.Index)
		{
			OnServer.SetCustomColor(item, eventInstance.Parent.CustomColor.Index);
		}
	}

	public void Deconstruct(ConstructionEventInstance eventInstance)
	{
		SpawnItem(eventInstance, ToolEntry);
		SpawnItem(eventInstance, ToolEntry2);
	}

	public string GetExitToolAsString()
	{
		if (!ToolExit)
		{
			return string.Empty;
		}
		return GameStrings.ToolRequiredToDeconstruct.AsString(ToolExit.ToTooltip());
	}
}
