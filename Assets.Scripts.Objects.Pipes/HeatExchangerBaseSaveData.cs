using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(HeatExchangerBaseSaveData))]
public class HeatExchangerBaseSaveData : DeviceAtmosphericSaveData
{
}
