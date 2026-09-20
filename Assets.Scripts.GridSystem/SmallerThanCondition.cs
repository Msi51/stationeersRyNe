using System.Collections.Generic;
using Assets.Scripts.Objects;

namespace Assets.Scripts.GridSystem;

public class SmallerThanCondition : RoomRuleCondition
{
	public int RoomSize;

	public SmallerThanCondition(SmallerThanConditionData data)
	{
		RoomSize = data.RoomSize;
	}

	public override bool Evaluate(List<Thing> thingsInRoom, int roomSize)
	{
		return roomSize < RoomSize;
	}
}
