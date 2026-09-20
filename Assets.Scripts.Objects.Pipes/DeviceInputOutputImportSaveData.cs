using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(DeviceAtmosphericSaveData))]
public class DeviceInputOutputImportSaveData : DeviceAtmosphericSaveData
{
	[XmlElement]
	public int ImportCount;

	[XmlElement]
	public int ImportState;
}
