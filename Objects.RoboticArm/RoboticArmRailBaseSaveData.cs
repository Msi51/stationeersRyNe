using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.RoboticArm;

[XmlInclude(typeof(StructureSaveData))]
public class RoboticArmRailBaseSaveData : StructureSaveData
{
	[XmlElement]
	public long RoboticArmNetworkId;
}
