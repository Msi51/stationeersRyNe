using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(StructureSaveData))]
public class RocketDataLinkSaveData : StructureSaveData
{
	public long ConnectedId;
}
