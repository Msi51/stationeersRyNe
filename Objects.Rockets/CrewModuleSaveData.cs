using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Rockets;

[XmlInclude(typeof(StructureSaveData))]
public class CrewModuleSaveData : StructureFuselageSaveData
{
	[XmlElement]
	public int PartnerDistance;
}
