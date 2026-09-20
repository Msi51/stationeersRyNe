using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;

namespace Objects.Electrical;

[XmlInclude(typeof(DroidSleeperSaveData))]
public class DroidSleeperSaveData : DeviceAtmosphericSaveData
{
}
