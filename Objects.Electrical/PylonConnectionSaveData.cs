using System.Xml.Serialization;
using SyncedReferencables;

namespace Objects.Electrical;

public class PylonConnectionSaveData : SyncedReferencableSaveData
{
	[XmlAttribute]
	public long FromRefId;

	[XmlAttribute]
	public byte FromIndex;

	[XmlAttribute]
	public long ToRefId;

	[XmlAttribute]
	public byte ToIndex;
}
