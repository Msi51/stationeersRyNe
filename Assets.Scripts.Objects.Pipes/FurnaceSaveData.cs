using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(DeviceInputOutputImportSaveData))]
public class FurnaceSaveData : DeviceInputOutputImportExportSaveData
{
}
