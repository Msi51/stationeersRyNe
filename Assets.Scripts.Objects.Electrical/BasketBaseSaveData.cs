using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class BasketBaseSaveData : StructureSaveData
{
	[XmlElement]
	public int Setting;
}
