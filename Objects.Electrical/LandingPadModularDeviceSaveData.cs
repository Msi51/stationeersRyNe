using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class LandingPadModularDeviceSaveData : StructureSaveData
{
	[XmlElement]
	public long PadNetworkId;
}
