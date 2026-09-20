using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Rockets;

[XmlInclude(typeof(StructureSaveData))]
public class RocketChuteUmbilicalFemaleSaveData : StructureSaveData
{
	[XmlElement]
	public int PartnerDistance;
}
