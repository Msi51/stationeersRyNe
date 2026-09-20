using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Structures;

[XmlInclude(typeof(StructureSaveData))]
public class DockSaveData : StructureSaveData
{
	[XmlElement]
	public long LinkedDockReferenceID;
}
