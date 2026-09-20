using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace TraderUI;

public static class TraderDebugData
{
	public static TradeData BuildAll()
	{
		System.Random random = new System.Random(0);
		TradeData tradeData = new TradeData
		{
			PlayerName = "DEBUG",
			TraderName = "ALL TRADER ITEMS",
			PlayerCredits = 0f
		};
		foreach (TraderData allTraderDatum in TraderData.AllTraderData)
		{
			foreach (SellData sellDatum in allTraderDatum.SellData)
			{
				AddSellRows(allTraderDatum, sellDatum, tradeData.Buying, random);
			}
			foreach (BuyData buyDatum in allTraderDatum.BuyData)
			{
				AddBuyRows(allTraderDatum, buyDatum, tradeData.Selling, random);
			}
		}
		return tradeData;
	}

	private static void AddSellRows(TraderData trader, SellData sellData, List<TradeItemData> rows, System.Random random)
	{
		SellDataInstance sellDataInstance = ((sellData.Stock != null) ? new SellDataInstance(sellData, random) : null);
		int stock = sellDataInstance?.Stock ?? 0;
		List<TradableItem> list = sellData.SelectData?.TradeItems;
		if (list != null && list.Count > 0)
		{
			for (int i = 0; i < list.Count; i++)
			{
				rows.Add(BuildSellRow(trader, sellData, sellDataInstance, stock, list[i] as SellItem, i, list.Count));
			}
		}
		else
		{
			rows.Add(BuildSellRow(trader, sellData, sellDataInstance, stock, sellData.SellingItem, -1, 0));
		}
	}

	private static void AddBuyRows(TraderData trader, BuyData buyData, List<TradeItemData> rows, System.Random random)
	{
		BuyDataInstance buyDataInstance = ((buyData.Required != null) ? new BuyDataInstance(buyData, random) : null);
		int required = buyDataInstance?.Required ?? 0;
		List<TradableItem> list = buyData.SelectData?.TradeItems;
		if (list != null && list.Count > 0)
		{
			for (int i = 0; i < list.Count; i++)
			{
				rows.Add(BuildBuyRow(trader, buyData, buyDataInstance, required, list[i] as BuyItem, i, list.Count));
			}
		}
		else
		{
			rows.Add(BuildBuyRow(trader, buyData, buyDataInstance, required, buyData.BuyingItem, -1, 0));
		}
	}

	private static TradeItemData BuildSellRow(TraderData trader, SellData sellData, SellDataInstance instance, int stock, SellItem item, int selectIndex, int selectCount)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendHeader(stringBuilder, trader, sellData, "Stock " + Range(sellData.Stock), selectIndex, selectCount);
		foreach (ActionData tradeAction in sellData.TradeActions)
		{
			tradeAction.ToolTip(stringBuilder, 0, item?.Prefab);
		}
		item?.ToolTip(stringBuilder, 0);
		return new TradeItemData
		{
			ItemName = ItemName(sellData, item),
			ItemImage = ItemImage(item, instance),
			Cost = sellData.GetCost(),
			NumberAvailable = stock,
			NumberWanted = 9999,
			TooltipText = TrimToInfo(stringBuilder),
			DataInstance = instance
		};
	}

	private static TradeItemData BuildBuyRow(TraderData trader, BuyData buyData, BuyDataInstance instance, int required, BuyItem item, int selectIndex, int selectCount)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendHeader(stringBuilder, trader, buyData, "Required " + Range(buyData.Required), selectIndex, selectCount);
		foreach (ConditionData condition in buyData.Conditions)
		{
			condition.ToolTip(stringBuilder, 0);
		}
		foreach (ConditionDataCollection conditionCollection in buyData.ConditionCollections)
		{
			conditionCollection.ToolTip(stringBuilder, 0);
		}
		item?.ToolTip(stringBuilder);
		return new TradeItemData
		{
			ItemName = ItemName(buyData, item),
			ItemImage = ItemImage(item, instance),
			Cost = buyData.GetCost(),
			NumberWanted = required,
			NumberAvailable = 0,
			TooltipText = TrimToInfo(stringBuilder),
			DataInstance = instance
		};
	}

	private static void AppendHeader(StringBuilder sb, TraderData trader, TransactionData data, string stockOrRequired, int selectIndex, int selectCount)
	{
		sb.Append("Trader: ").AppendLine(trader.Id.AsColor("orange"));
		if (!string.IsNullOrEmpty(data.Id))
		{
			sb.Append("Trade: ").AppendLine(data.Id);
		}
		if (selectIndex >= 0)
		{
			sb.AppendLine($"[Select {selectIndex + 1} of {selectCount}]".AsColor("cyan"));
		}
		sb.Append($"Value €{data.GetCost()}    {stockOrRequired}");
		if (data.ChanceData != null)
		{
			sb.Append($"    Chance {data.ChanceData.Value}");
		}
		if (data.ItemPoolData != null)
		{
			sb.Append($"    Pool #{data.ItemPoolData.Index} (w{data.ItemPoolData.Weight})");
		}
		sb.AppendLine();
		if (data.WorldCondition != null)
		{
			sb.AppendLine("World-restricted".AsColor("yellow"));
		}
		if (data.SlotTypes.Count > 0)
		{
			sb.Append("Types: ");
			for (int i = 0; i < data.SlotTypes.Count; i++)
			{
				if (i > 0)
				{
					sb.Append(", ");
				}
				sb.Append(data.SlotTypes[i].SlotId);
			}
			sb.AppendLine();
		}
		sb.AppendLine();
	}

	private static string Range(IntRangeData range)
	{
		if (range == null)
		{
			return "?";
		}
		if (range.Max >= 1 && range.Min >= 1 && range.Min <= range.Max)
		{
			return $"{range.Min}-{range.Max}";
		}
		return range.Value.ToString();
	}

	private static string ItemName(TransactionData data, TradableItem item)
	{
		if (!string.IsNullOrEmpty(data.Name))
		{
			return data.Name;
		}
		if (item?.Prefab != null && !string.IsNullOrEmpty(item.Prefab.DisplayName))
		{
			return item.Prefab.DisplayName;
		}
		return "???";
	}

	private static Sprite ItemImage(TradableItem item, TransactionDataInstance instance)
	{
		DynamicThing dynamicThing = item?.Prefab;
		if (dynamicThing != null)
		{
			if (item.colorSwatch != null)
			{
				return Thing.GetThumbnail(dynamicThing, item.colorSwatch.GetIndex());
			}
			return dynamicThing.GetThumbnail();
		}
		Texture2D texture2D = instance?.Thumbnail;
		if (texture2D != null)
		{
			return Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), Vector2.one * 0.5f);
		}
		return null;
	}

	private static string TrimToInfo(StringBuilder sb)
	{
		string text = sb.ToString().TrimEnd();
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return "(no extra info)";
	}
}
