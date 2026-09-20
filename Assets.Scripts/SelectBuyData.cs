using System.Collections.Generic;
using System.Xml.Serialization;

namespace Assets.Scripts;

public class SelectBuyData : SelectData
{
	[XmlElement("Item", typeof(BuyItem))]
	public List<TradableItem> BuyItems;

	public override List<TradableItem> TradeItems => BuyItems;
}
