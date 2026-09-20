using System.Xml.Serialization;

namespace Assets.Scripts.GridSystem;

public class RoomTypeRuleData
{
	[XmlAttribute("RoomType")]
	public string RoomType;

	[XmlElement("Any", typeof(AnyConditionData))]
	[XmlElement("All", typeof(AllConditionData))]
	[XmlElement("Contains", typeof(ContainsConditionData))]
	[XmlElement("SmallerThan", typeof(SmallerThanConditionData))]
	public RoomRuleConditionData Root;
}
