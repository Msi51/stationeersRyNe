using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(DeviceAtmosphericSaveData))]
public class ActiveVentSaveData : DeviceAtmosphericSaveData
{
	[XmlElement]
	public float ExternalPressure = 101.325f;

	[XmlElement]
	public float InternalPressure = 50662.5f;
}
