using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(DeviceImportExportSaveData))]
public class RocketChuteStorageSaveData : DeviceImportExportSaveData
{
	public int CurrentIndex;
}
