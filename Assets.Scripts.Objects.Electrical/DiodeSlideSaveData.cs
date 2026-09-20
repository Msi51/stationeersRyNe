using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class DiodeSlideSaveData : StructureSaveData
{
	[XmlElement]
	public double Setting;
}
