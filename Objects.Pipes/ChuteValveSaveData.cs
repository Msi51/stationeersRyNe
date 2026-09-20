using System.Xml.Serialization;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;

namespace Objects.Pipes;

[XmlInclude(typeof(StructureSaveData))]
public class ChuteValveSaveData : ChuteSaveData
{
}
