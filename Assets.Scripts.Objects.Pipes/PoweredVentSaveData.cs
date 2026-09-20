using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(DeviceAtmosphericSaveData))]
public class PoweredVentSaveData : DeviceAtmosphericSaveData
{
	[XmlElement]
	public float ExternalPressure;

	[XmlElement]
	public float InternalPressure;
}
