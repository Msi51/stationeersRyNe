using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Rockets;

[XmlInclude(typeof(StructureSaveData))]
public class RocketCrewUmbilicalSaveData : StructureSaveData
{
	[XmlElement]
	public long PartnerUmbilicalId;

	[XmlElement]
	public long UmbilicalDoorId;
}
