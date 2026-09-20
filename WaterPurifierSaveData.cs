using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(WaterPurifierSaveData))]
public class WaterPurifierSaveData : DeviceAtmosphericSaveData
{
}
