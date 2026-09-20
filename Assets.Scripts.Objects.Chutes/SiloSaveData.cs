using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Objects.Chutes;

[XmlInclude(typeof(StructureSaveData))]
public class SiloSaveData : DeviceImportExportSaveData
{
	[XmlElement]
	public List<StoredThings> AllStoredItems;
}
