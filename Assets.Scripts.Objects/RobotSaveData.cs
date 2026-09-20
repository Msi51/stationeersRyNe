using System.Xml.Serialization;

namespace Assets.Scripts.Objects;

[XmlInclude(typeof(ThingSaveData))]
public class RobotSaveData : DynamicThingSaveData
{
	[XmlElement]
	public float TargetX;

	[XmlElement]
	public float TargetY;

	[XmlElement]
	public float TargetZ;
}
