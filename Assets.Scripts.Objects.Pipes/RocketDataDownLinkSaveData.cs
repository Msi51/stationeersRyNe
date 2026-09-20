using System.Collections.Generic;
using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(StructureSaveData))]
public class RocketDataDownLinkSaveData : StructureSaveData
{
	public List<long> ConnectedIds;
}
