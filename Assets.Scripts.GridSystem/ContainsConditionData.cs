using System.Xml.Serialization;

namespace Assets.Scripts.GridSystem;

public class ContainsConditionData : RoomRuleConditionData
{
	[XmlAttribute("Prefab")]
	public string PrefabName;

	public override RoomRuleCondition ToInstance()
	{
		return new ContainsCondition(this);
	}
}
