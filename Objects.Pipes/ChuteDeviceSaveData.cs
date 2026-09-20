using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Pipes;

[XmlInclude(typeof(StructureSaveData))]
public class ChuteDeviceSaveData : StructureSaveData
{
}
