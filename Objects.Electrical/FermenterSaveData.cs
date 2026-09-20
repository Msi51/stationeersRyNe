using System.Xml.Serialization;
using Objects.Pipes;

namespace Objects.Electrical;

[XmlInclude(typeof(DeviceInputOutputImportCircuitSaveData))]
public class FermenterSaveData : DeviceInputOutputImportCircuitSaveData
{
	[XmlElement]
	public float CurrentProgress;
}
