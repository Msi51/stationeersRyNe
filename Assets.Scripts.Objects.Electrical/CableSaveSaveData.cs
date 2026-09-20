using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class CableSaveSaveData : StructureSaveData
{
	[XmlElement]
	public long CableNetworkId;
}
