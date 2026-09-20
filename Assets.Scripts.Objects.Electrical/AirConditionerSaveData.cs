using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(DeviceInputOutputCircuitSaveData))]
public class AirConditionerSaveData : DeviceInputOutputCircuitSaveData
{
}
