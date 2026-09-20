using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(CryoTubeSaveData))]
public class CryoTubeSaveData : DeviceAtmosphericSaveData
{
}
