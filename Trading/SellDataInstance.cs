using System;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;

namespace Trading;

public class SellDataInstance : TransactionDataInstance
{
	public readonly SellData SellData;

	public int Stock;

	public readonly SellItem SellingItem;

	public override string DisplayName
	{
		get
		{
			if (!string.IsNullOrEmpty(SellData.Name))
			{
				return SellData.Name;
			}
			if (SellingItem?.Prefab != null && !string.IsNullOrEmpty(SellingItem.Prefab.DisplayName))
			{
				return SellingItem.Prefab.DisplayName;
			}
			return string.Empty;
		}
	}

	public override bool IsGasTransaction()
	{
		foreach (ActionData tradeAction in SellData.TradeActions)
		{
			if (tradeAction is GasAction)
			{
				return true;
			}
		}
		return false;
	}

	public SellDataInstance(SellData sellData, Random random)
	{
		SellData = sellData;
		Stock = sellData.Stock.GenerateValue(random);
		SellingItem = SellData.SellingItem;
		if (sellData.SelectData?.SelectItem(random) is SellItem sellingItem)
		{
			SellingItem = sellingItem;
		}
		SetThumbnail(sellData).Forget();
	}

	public void ExecuteActions(ref GasMixture gasMixture, int totalMolesSold)
	{
		foreach (ActionData tradeAction in SellData.TradeActions)
		{
			tradeAction.Execute(ref gasMixture, totalMolesSold);
		}
	}

	public void ExecuteActions(ITradable tradable)
	{
		foreach (ActionData tradeAction in SellData.TradeActions)
		{
			tradeAction.Execute(tradable);
		}
		if (SellingItem == null)
		{
			return;
		}
		foreach (ActionData action in SellingItem.Actions)
		{
			action.Execute(tradable);
		}
		foreach (SellItem child in SellingItem.Children)
		{
			Slot slot = ((child.SlotIndex >= 0) ? tradable.GetSlot(child.SlotIndex) : ((child.SlotIdHash == 0) ? tradable.GetNextFreeSlot() : tradable.GetNextFreeSlot(child.SlotId)));
			if (slot == null || (object)child.Prefab == null)
			{
				continue;
			}
			DynamicThing dynamicThing = Thing.Create<DynamicThing>(child.Prefab, slot.Location.position, slot.Location.rotation, 0L);
			child.AssignToPrefab(dynamicThing);
			OnServer.MoveToSlot(dynamicThing, slot);
			if (!(dynamicThing is ITradable tradable2))
			{
				continue;
			}
			foreach (ActionData action2 in child.Actions)
			{
				action2.Execute(tradable2);
			}
		}
	}

	public override Thing GetItemPrefab()
	{
		return SellingItem?.Prefab;
	}

	public string ToolTip()
	{
		if (HasCustomToolTip(SellData.CustomToolTipGameStringKey, out var toolTip))
		{
			return toolTip;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (ActionData tradeAction in SellData.TradeActions)
		{
			tradeAction.ToolTip(stringBuilder, 0, SellingItem?.Prefab);
		}
		SellingItem?.ToolTip(stringBuilder, 0);
		string text = stringBuilder.ToString();
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return GameStrings.TradingToolTipNoInfo.DisplayString;
	}

	public override void Write(RocketBinaryWriter writer)
	{
		base.Write(writer);
		writer.WriteInt32(Stock);
	}

	public override void Read(RocketBinaryReader reader)
	{
		base.Read(reader);
		Stock = reader.ReadInt32();
	}

	public void ToTreeString(TreeString contactString)
	{
		TreeString.Node($"{DisplayName} €{SellData.Value} x {Stock}", contactString);
	}
}
