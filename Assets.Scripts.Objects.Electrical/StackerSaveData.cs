using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class StackerSaveData : SlotHandlerBaseSaveData
{
	[XmlElement]
	public double Setting;
}
