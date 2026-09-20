using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class LogicBaseSaveData : StructureSaveData
{
	[XmlElement]
	public double Setting;
}
