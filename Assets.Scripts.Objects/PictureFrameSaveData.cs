using System.Xml.Serialization;

namespace Assets.Scripts.Objects;

[XmlInclude(typeof(StructureSaveData))]
public class PictureFrameSaveData : StructureSaveData
{
	public int PictureIndex;
}
