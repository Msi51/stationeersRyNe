using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Assets.Scripts;

[XmlInclude(typeof(ThingSaveData))]
public class ElevatorCarrageSaveData : DynamicThingSaveData
{
	[XmlElement]
	public int LevelTarget;

	[XmlElement]
	public float Speed = 1.5f;
}
