using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Rockets;

[XmlInclude(typeof(StructureSaveData))]
public class RocketChuteUmbilicalMaleSaveData : StructureSaveData
{
	[XmlElement]
	public long PartnerUmbilicalID;
}
