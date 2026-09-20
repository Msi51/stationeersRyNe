using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.RoboticArm;

[XmlInclude(typeof(StructureSaveData))]
public class RoboticArmDockAtmosSaveData : RoboticArmDockSaveData
{
	[XmlAttribute]
	public float ExternalPressure = 101.325f;

	[XmlAttribute]
	public float InternalPressure = 50662.5f;
}
