using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(StructureSaveData))]
public class DeviceAtmosphericSaveData : StructureSaveData
{
	[XmlElement]
	public float OutputSetting;
}
