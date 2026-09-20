using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Objects.Items;
using Reagents;
using Trading;

namespace Assets.Scripts;

[XmlType("SellItem")]
public class SellItem : TradableItem
{
	[XmlElement("Interaction", typeof(InteractionAction))]
	[XmlElement("MovePlayer", typeof(MovePlayerAction))]
	[XmlElement("Gene", typeof(GeneAction))]
	[XmlElement("Quantity", typeof(QuantityAction))]
	[XmlElement("Percent", typeof(PercentAction))]
	[XmlElement("Reagents", typeof(ReagentAction))]
	[XmlElement("Charge", typeof(ChargeAction))]
	[XmlElement("Gas", typeof(GasAction))]
	[XmlElement("SourceCode", typeof(SourceCodeAction))]
	public List<ActionData> Actions = new List<ActionData>();

	[XmlElement("Item")]
	public List<SellItem> Children = new List<SellItem>();

	public override int GetChecksum()
	{
		int checksum = base.GetChecksum();
		checksum = (checksum ^ Actions.Count) * 41;
		foreach (ActionData action in Actions)
		{
			checksum = (checksum ^ action.GetChecksum()) * 41;
		}
		foreach (SellItem child in Children)
		{
			checksum = (checksum ^ child.GetChecksum()) * 41;
		}
		return checksum;
	}

	public override void Initialize()
	{
		base.Initialize();
		foreach (ActionData action in Actions)
		{
			action.Initialize();
		}
		foreach (SellItem child in Children)
		{
			child.Initialize();
		}
	}

	public void ToolTip(StringBuilder stringBuilder, int generations)
	{
		float num = float.NaN;
		foreach (ActionData action in Actions)
		{
			if (action is QuantityAction quantityAction)
			{
				num = quantityAction.Value;
				break;
			}
		}
		if (float.IsNaN(num) && Children.Count == 0 && Actions.Count == 0)
		{
			if ((generations != 0 || Name != null) && (object)Prefab != null)
			{
				for (int i = 0; i < generations; i++)
				{
					stringBuilder.Append("    ");
				}
				stringBuilder.AppendLine(Prefab.ToTooltip());
			}
			return;
		}
		bool flag;
		if (Prefab is Item item)
		{
			for (int j = 0; j < generations; j++)
			{
				stringBuilder.Append("    ");
			}
			if (!string.IsNullOrEmpty(SlotId))
			{
				stringBuilder.AppendLine(GameStrings.ItemInSlot.AsString(item.ToTooltip(), SlotId.AsColor("yellow")));
			}
			else if (SlotIndex >= 0)
			{
				stringBuilder.AppendLine(GameStrings.ItemInSlot.AsString(item.ToTooltip(), SlotIndex.ToString().AsColor("yellow")));
			}
			if (!float.IsNaN(num))
			{
				if (item is Stackable stackable)
				{
					stringBuilder.AppendLine(GameStrings.ItemInSlotStack.AsString(stackable.ToTooltip(), StringManager.Get(num).AsColor("yellow")));
				}
				else if (item is Consumable consumable)
				{
					stringBuilder.AppendLine(GameStrings.ItemInSlotValue.AsString(consumable.ToTooltip(), StringManager.Get(num).AsColor("yellow")));
				}
				else
				{
					stringBuilder.AppendLine(GameStrings.TradeItemStackChild.AsString(item.ToTooltip(), StringManager.Get(num).AsColor("yellow")));
				}
			}
			else
			{
				stringBuilder.AppendLine(GameStrings.TradeItemParent.AsString(item.ToTooltip()));
			}
			generations++;
			if (item is Ore)
			{
				if (!(item is Slag))
				{
					goto IL_021b;
				}
			}
			else if (item is Ingot)
			{
				goto IL_021b;
			}
			flag = false;
			goto IL_0223;
		}
		goto IL_0419;
		IL_0419:
		foreach (ActionData action2 in Actions)
		{
			if (!(action2 is QuantityAction))
			{
				action2.ToolTip(stringBuilder, generations, Prefab);
			}
		}
		foreach (SellItem child in Children)
		{
			child.ToolTip(stringBuilder, generations);
		}
		return;
		IL_0223:
		if (flag)
		{
			string value = (float.IsNaN(num) ? item.CreatedReagentMixture.ToString() : item.CreatedReagentMixture.ToString(num));
			Ice ice = item as Ice;
			if (((object)ice != null && ice.SpawnContents.Count > 0) || !string.IsNullOrEmpty(value))
			{
				for (int k = 0; k < generations; k++)
				{
					stringBuilder.Append("    ");
				}
				stringBuilder.AppendLine(GameStrings.TradeItemContains.AsString(item.ToTooltip()).AsColor("white"));
			}
			if (!string.IsNullOrEmpty(value))
			{
				stringBuilder.Append(value);
			}
			if ((object)ice != null && ice.SpawnContents.Count > 0)
			{
				foreach (SpawnGas spawnContent in ice.SpawnContents)
				{
					for (int l = 0; l < generations; l++)
					{
						stringBuilder.Append("    ");
					}
					AtmosphericsManager.DisplayGas(stringBuilder, spawnContent, float.IsNaN(num) ? 1f : num);
				}
			}
		}
		else if (item is IMicrowaveIngredient microwaveIngredient)
		{
			string value2 = item.CreatedReagentMixture.ToString(num / microwaveIngredient.QuantityPerUse);
			if (!string.IsNullOrEmpty(value2))
			{
				stringBuilder.Append(value2);
			}
			else if (!string.IsNullOrEmpty(item.GetQuantityText()))
			{
				for (int m = 0; m < generations; m++)
				{
					stringBuilder.Append("    ");
				}
				stringBuilder.AppendLine(GameStrings.TradeItemQuantity.AsString(item.GetQuantityText().AsColor("yellow")));
			}
		}
		if (item is INutrition nutrition)
		{
			float nutritionalValue = nutrition.GetNutritionalValue();
			if (nutritionalValue > 0f)
			{
				for (int n = 0; n < generations; n++)
				{
					stringBuilder.Append("    ");
				}
				stringBuilder.AppendLine(GameStrings.TradeItemNutrition.AsString(nutritionalValue.ToStringRounded("yellow")));
			}
		}
		goto IL_0419;
		IL_021b:
		flag = true;
		goto IL_0223;
	}

	public void AssignToPrefab(DynamicThing dynamicThing)
	{
		if (colorSwatch != null)
		{
			dynamicThing.SetCustomColor(colorSwatch);
		}
		if (Name != null)
		{
			dynamicThing.RenameThing(Name);
		}
	}
}
