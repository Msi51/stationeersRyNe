using System;
using System.Collections.Generic;
using Assets.Scripts.Util;

namespace Assets.Scripts;

public abstract class SelectData : IChecksum
{
	public abstract List<TradableItem> TradeItems { get; }

	public TradableItem SelectItem(Random random)
	{
		List<TradableItem> tradeItems = TradeItems;
		if (tradeItems == null || tradeItems.Count <= 0)
		{
			return null;
		}
		return TradeItems.Pick(random);
	}

	public bool IsValid()
	{
		if (TradeItems.Count > 0)
		{
			foreach (TradableItem tradeItem in TradeItems)
			{
				if (tradeItem == null || !tradeItem.IsValid())
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public int GetChecksum()
	{
		int num = TradeItems.Count;
		foreach (TradableItem tradeItem in TradeItems)
		{
			num = (num ^ tradeItem.GetChecksum()) * 41;
		}
		return num;
	}
}
