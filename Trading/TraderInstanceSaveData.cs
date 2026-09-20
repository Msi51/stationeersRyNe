using System.Collections.Generic;
using System.Xml.Serialization;

namespace Trading;

[XmlRoot]
public class TraderInstanceSaveData
{
	public long ReferenceId;

	public int TraderDataId;

	public int Seed;

	[XmlArray("BuySaveData")]
	[XmlArrayItem("Buy")]
	public List<BuySaveData> BuySaveData = new List<BuySaveData>();

	[XmlArray("SellSaveData")]
	[XmlArrayItem("Sell")]
	public List<SellSaveData> SellSaveData = new List<SellSaveData>();

	public string Checksum;
}
