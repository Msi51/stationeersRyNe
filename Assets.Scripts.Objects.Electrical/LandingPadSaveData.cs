using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class LandingPadSaveData : StructureSaveData
{
	[XmlElement]
	public long TraderReferenceID;

	[XmlElement]
	public long ParentMotherboardID;
}
