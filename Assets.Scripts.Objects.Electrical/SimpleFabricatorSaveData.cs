using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class SimpleFabricatorSaveData : DeviceImportExportSaveData
{
	public FabricatorJob FabricatorJob = new FabricatorJob();

	public int CurrentIndex;
}
