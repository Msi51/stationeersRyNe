using System.Collections.Generic;
using System.Xml.Serialization;

namespace Assets.Scripts;

public class SelectSellData : SelectData
{
	[XmlElement("Item", typeof(SellItem))]
	public List<TradableItem> SellItems;

	public override List<TradableItem> TradeItems => SellItems;
}
