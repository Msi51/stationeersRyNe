using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Structures;

[XmlInclude(typeof(GeyserSaveData))]
public class GeyserSaveData : StructureSaveData
{
	[XmlElement]
	public bool Tapped;
}
