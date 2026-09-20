using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(DynamicThingSaveData))]
public class AdvancedTabletSaveData : DynamicThingSaveData
{
}
