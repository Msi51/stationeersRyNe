using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class VendingMachineSaveData : DeviceImportExportSaveData
{
	public int CurrentIndex;
}
