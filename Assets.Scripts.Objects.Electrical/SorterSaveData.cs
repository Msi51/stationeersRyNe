using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class SorterSaveData : DeviceImportExportSaveData
{
	[XmlElement]
	public int CurrentOutput;

	[XmlArray("Filters")]
	[XmlArrayItem("Filter")]
	public List<FilterReference> FilterReferences = new List<FilterReference>();
}
