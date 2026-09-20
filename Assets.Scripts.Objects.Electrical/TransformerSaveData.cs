using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class TransformerSaveData : StructureSaveData
{
	[XmlElement]
	public float OutputSetting;
}
