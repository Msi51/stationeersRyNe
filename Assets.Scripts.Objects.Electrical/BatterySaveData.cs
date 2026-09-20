using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class BatterySaveData : StructureSaveData
{
	[XmlElement]
	public float PowerStored;
}
