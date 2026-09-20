using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Structures;

[XmlInclude(typeof(StructureSaveData))]
public class DoorSaveData : StructureSaveData
{
	[XmlElement]
	public double Setting;
}
