using System.Collections.Generic;
using Assets.Scripts.Objects;

namespace Assets.Scripts.GridSystem;

public class AllCondition : RoomRuleCondition
{
	public List<RoomRuleCondition> Conditions = new List<RoomRuleCondition>();

	public AllCondition(AllConditionData data)
	{
		foreach (RoomRuleConditionData condition in data.Conditions)
		{
			Conditions.Add(condition.ToInstance());
		}
	}

	public override bool Evaluate(List<Thing> thingsInRoom, int roomSize)
	{
		foreach (RoomRuleCondition condition in Conditions)
		{
			if (!condition.Evaluate(thingsInRoom, roomSize))
			{
				return false;
			}
		}
		return true;
	}
}
