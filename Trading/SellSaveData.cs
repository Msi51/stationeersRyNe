using System.Xml.Serialization;

namespace Trading;

[XmlInclude(typeof(TransactionSaveData))]
public class SellSaveData : TransactionSaveData
{
	public int Stock;
}
