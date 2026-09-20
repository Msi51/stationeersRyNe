using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Rockets;

[XmlInclude(typeof(StructureSaveData))]
public class RocketPowerUmbilicalMaleSaveData : StructureSaveData
{
	[XmlElement]
	public float PowerStored;

	[XmlElement]
	public long PartnerUmbilicalId;
}
