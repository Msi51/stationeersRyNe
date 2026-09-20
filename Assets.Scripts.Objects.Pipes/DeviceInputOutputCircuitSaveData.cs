using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(DeviceInputOutputCircuitSaveData))]
public class DeviceInputOutputCircuitSaveData : DeviceAtmosphericSaveData
{
	[XmlElement]
	public long[] DeviceIDs;
}
