using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Rockets;

[XmlInclude(typeof(StructureSaveData))]
public class StructureFuselageSaveData : StructureSaveData
{
	[XmlElement]
	public long RocketNetworkId;
}
