using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(StructureSaveData))]
public class ChuteSaveData : StructureSaveData
{
	[XmlElement]
	public long ChuteNetworkId;

	[XmlElement]
	public long NextNeighborId = -1L;
}
