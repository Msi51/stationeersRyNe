using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(StructureSaveData))]
public class DeviceImportExportSaveData : DeviceImportSaveData
{
	[XmlElement]
	public int ExportCount;

	[XmlElement]
	public int ExportState;
}
