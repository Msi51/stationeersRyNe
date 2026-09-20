using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(PassthroughHeatExchangerSaveData))]
public class PassthroughHeatExchangerSaveData : HeatExchangerBaseSaveData
{
}
