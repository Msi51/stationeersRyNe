using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(StructureSaveData))]
public class DeviceImportExport2SaveData : DeviceImportExportSaveData
{
	[XmlElement]
	public int Export2State;
}
