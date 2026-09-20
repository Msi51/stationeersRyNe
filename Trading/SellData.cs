using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using Reagents;

namespace Trading;

public class SellData : TransactionData
{
	[XmlElement("Stock")]
	public StockData Stock;

	[XmlElement("Interaction", typeof(InteractionAction))]
	[XmlElement("MovePlayer", typeof(MovePlayerAction))]
	[XmlElement("Gene", typeof(GeneAction))]
	[XmlElement("Quantity", typeof(QuantityAction))]
	[XmlElement("Percent", typeof(PercentAction))]
	[XmlElement("Reagents", typeof(ReagentAction))]
	[XmlElement("Charge", typeof(ChargeAction))]
	[XmlElement("Gas", typeof(GasAction))]
	[XmlElement("SourceCode", typeof(SourceCodeAction))]
	public List<ActionData> TradeActions = new List<ActionData>();

	[XmlElement("Select")]
	public SelectSellData SelectData;

	[XmlElement("Item")]
	public SellItem SellingItem;

	public override int GetChecksum()
	{
		int checksum = base.GetChecksum();
		checksum = (checksum ^ (Stock?.GetChecksum() ?? 0)) * 41;
		checksum = (checksum ^ TradeActions.Count) * 41;
		foreach (ActionData tradeAction in TradeActions)
		{
			checksum = (checksum ^ (tradeAction?.GetChecksum() ?? 0)) * 41;
		}
		checksum = (checksum ^ (SellingItem?.GetChecksum() ?? 0)) * 41;
		return (checksum ^ (SelectData?.GetChecksum() ?? 0)) * 41;
	}

	public override void Apply(TransactionData transactionData)
	{
		base.Apply(transactionData);
		if (transactionData is SellData sellData)
		{
			if (Stock == null)
			{
				Stock = sellData.Stock;
			}
			TradeActions.AddRange(sellData.TradeActions);
			SellingItem = sellData.SellingItem;
			SelectData = sellData.SelectData;
		}
	}

	public override void Initialize(ModAbout mod)
	{
		SellingItem?.Initialize();
		if (SelectData?.TradeItems != null)
		{
			foreach (TradableItem tradeItem in SelectData.TradeItems)
			{
				tradeItem.Initialize();
			}
		}
		foreach (ActionData tradeAction in TradeActions)
		{
			tradeAction.Initialize();
		}
		base.Initialize(mod);
	}

	public override bool IsValid()
	{
		if (!base.IsValid())
		{
			return false;
		}
		if (SellingItem == null && TradeActions.Count > 0)
		{
			foreach (ActionData tradeAction in TradeActions)
			{
				if (tradeAction is GasAction)
				{
					return true;
				}
			}
		}
		if (Stock == null)
		{
			return false;
		}
		if (SelectData != null)
		{
			return SelectData.IsValid();
		}
		if (SellingItem != null)
		{
			return SellingItem.IsValid();
		}
		return false;
	}
}
