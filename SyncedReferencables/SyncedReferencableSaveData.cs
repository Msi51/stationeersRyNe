using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace SyncedReferencables;

public class SyncedReferencableSaveData : IReferencableSaveData
{
	[XmlAttribute("Id")]
	public long ReferenceId;

	[XmlAttribute("Type")]
	public int TypeId;

	[XmlIgnore]
	public long SavedReferenceId => ReferenceId;
}
