using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Electrical;

[XmlInclude(typeof(PadModularSaveData))]
public class PadModularSaveData : StructureSaveData
{
	[XmlElement]
	public long PadNetworkId;
}
