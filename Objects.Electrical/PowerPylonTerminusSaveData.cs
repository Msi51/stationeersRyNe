using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class PowerPylonTerminusSaveData : StructureSaveData
{
	[XmlElement]
	public float PowerStored;
}
