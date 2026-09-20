using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(DeviceAtmosphericSaveData))]
public class NitrolyzerSaveData : DeviceInputOutputCircuitSaveData
{
}
