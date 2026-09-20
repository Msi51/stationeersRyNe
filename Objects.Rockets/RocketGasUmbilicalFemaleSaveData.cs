using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;

namespace Objects.Rockets;

[XmlInclude(typeof(DeviceAtmosphericSaveData))]
public class RocketGasUmbilicalFemaleSaveData : DeviceAtmosphericSaveData
{
	[XmlElement]
	public int PartnerDistance;

	[XmlElement]
	public long PartnerUmbilicalId;
}
