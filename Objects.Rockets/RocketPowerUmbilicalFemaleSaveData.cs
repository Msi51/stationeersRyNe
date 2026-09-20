using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Rockets;

[XmlInclude(typeof(StructureSaveData))]
public class RocketPowerUmbilicalFemaleSaveData : StructureSaveData
{
	[XmlElement]
	public float PowerStored;

	[XmlElement]
	public int PartnerDistance;

	[XmlElement]
	public long PartnerUmbilicalId;
}
