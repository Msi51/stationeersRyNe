using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(DeviceInputOutputImportExportSaveData))]
public class DeviceInputOutputImportExportCircuitSaveData : DeviceInputOutputImportExportSaveData
{
	[XmlElement]
	public long[] DeviceIDs;
}
