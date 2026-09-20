using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(StructureSaveData))]
public class DeviceImportSaveData : StructureSaveData
{
	[XmlElement]
	public int ImportCount;

	[XmlElement]
	public int ImportState;
}
