using System.Collections.Generic;
using System.Xml.Serialization;

namespace Assets.Scripts.GridSystem;

public class AnyConditionData : RoomRuleConditionData
{
	[XmlElement("Any", typeof(AnyConditionData))]
	[XmlElement("All", typeof(AllConditionData))]
	[XmlElement("Contains", typeof(ContainsConditionData))]
	[XmlElement("SmallerThan", typeof(SmallerThanConditionData))]
	public List<RoomRuleConditionData> Conditions = new List<RoomRuleConditionData>();

	public override RoomRuleCondition ToInstance()
	{
		return new AnyCondition(this);
	}
}
