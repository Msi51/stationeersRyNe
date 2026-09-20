using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Rockets;

[XmlInclude(typeof(StructureSaveData))]
public class RocketGasUmbilicalMaleSaveData : StructureSaveData
{
	[XmlElement]
	public long PartnerUmbilicalId;
}
