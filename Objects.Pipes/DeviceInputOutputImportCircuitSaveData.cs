using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;

namespace Objects.Pipes;

[XmlInclude(typeof(DeviceInputOutputImportSaveData))]
public class DeviceInputOutputImportCircuitSaveData : DeviceInputOutputImportSaveData
{
	[XmlElement]
	public long[] DeviceIDs;
}
