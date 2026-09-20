using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;

namespace Objects.Electrical;

[XmlInclude(typeof(DeviceAtmosphericSaveData))]
public class LandingPadPumpSaveData : DeviceAtmosphericSaveData
{
	[XmlElement]
	public long PadNetworkId;
}
