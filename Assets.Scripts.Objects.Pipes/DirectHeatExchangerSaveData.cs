using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(DirectHeatExchangerSaveData))]
public class DirectHeatExchangerSaveData : HeatExchangerBaseSaveData
{
}
