using System.Xml.Serialization;

namespace Trading;

[XmlInclude(typeof(TransactionSaveData))]
public class BuySaveData : TransactionSaveData
{
	public int Required;
}
