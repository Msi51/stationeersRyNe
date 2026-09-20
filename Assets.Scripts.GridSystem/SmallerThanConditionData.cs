using System.Xml.Serialization;

namespace Assets.Scripts.GridSystem;

public class SmallerThanConditionData : RoomRuleConditionData
{
	[XmlAttribute("Size")]
	public int RoomSize;

	public override RoomRuleCondition ToInstance()
	{
		return new SmallerThanCondition(this);
	}
}
