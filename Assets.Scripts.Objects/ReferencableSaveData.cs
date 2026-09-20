using System.Xml.Serialization;

namespace Assets.Scripts.Objects;

public class ReferencableSaveData : IReferencableSaveData
{
	[XmlElement]
	public long ReferenceId;

	public long SavedReferenceId => ReferenceId;
}
