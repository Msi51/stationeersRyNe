using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.RoboticArm;

[XmlInclude(typeof(StructureSaveData))]
public class RoboticArmDockSaveData : RoboticArmRailDeviceBaseSaveData
{
	[XmlElement]
	public float CurrentIndex;

	[XmlElement]
	public int TargetJunctionIndex;

	[XmlElement]
	public bool IsStartingDock;

	[XmlElement]
	public ArmState ArmState;
}
