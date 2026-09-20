using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(DeviceImportExportSaveData))]
public class CentrifugeSaveData : DeviceImportExportSaveData
{
	[XmlElement]
	public float Rpm;
}
