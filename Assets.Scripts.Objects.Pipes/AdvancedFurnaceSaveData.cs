using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(StructureSaveData))]
public class AdvancedFurnaceSaveData : DeviceAtmosphericSaveData
{
	[XmlElement]
	public float OutputSetting2;
}
