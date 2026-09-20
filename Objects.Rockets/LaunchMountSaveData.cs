using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Rockets;

[XmlInclude(typeof(StructureSaveData))]
public class LaunchMountSaveData : StructureSaveData
{
	public long LinkedNodeId;
}
