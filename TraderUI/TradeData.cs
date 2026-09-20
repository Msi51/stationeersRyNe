using System.Collections.Generic;

namespace TraderUI;

public class TradeData
{
	public string PlayerName;

	public string TraderName;

	public float PlayerCredits;

	public List<TradeItemData> Buying = new List<TradeItemData>();

	public List<TradeItemData> Selling = new List<TradeItemData>();
}
