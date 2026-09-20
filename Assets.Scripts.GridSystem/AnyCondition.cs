using System.Collections.Generic;
using Assets.Scripts.Objects;

namespace Assets.Scripts.GridSystem;

public class AnyCondition : RoomRuleCondition
{
	public List<RoomRuleCondition> Conditions = new List<RoomRuleCondition>();

	public AnyCondition(AnyConditionData data)
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
			if (condition.Evaluate(thingsInRoom, roomSize))
			{
				return true;
			}
		}
		return false;
	}
}
