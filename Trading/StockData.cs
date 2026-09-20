using System.Xml.Serialization;

namespace Trading;

public class StockData : IntRangeData
{
	[XmlAttribute("Bulk")]
	public bool Bulk;

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ Bulk.GetHashCode()) * 41;
	}
}
