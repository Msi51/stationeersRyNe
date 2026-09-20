using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(StructureSaveData))]
public class DeviceInputOutputImportExportSaveData : DeviceInputOutputImportSaveData
{
	[XmlElement]
	public int ExportCount;

	[XmlElement]
	public int ExportState;
}
