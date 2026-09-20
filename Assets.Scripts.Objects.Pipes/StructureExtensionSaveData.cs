using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(StructureSaveData))]
public class StructureExtensionSaveData : StructureSaveData
{
	public long ExtendableParentId;
}
