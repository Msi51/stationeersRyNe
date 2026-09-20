using System;
using System.Collections.Generic;
using Assets.Scripts.Objects;

namespace Assets.Scripts.GridSystem;

public class RoomTypeRule
{
	public RoomType RoomType;

	public RoomRuleCondition RootCondition;

	public RoomTypeRule(RoomTypeRuleData data)
	{
		RoomType = (Enum.TryParse<RoomType>(data.RoomType, out var result) ? result : RoomType.Default);
		RootCondition = data.Root.ToInstance();
	}

	public bool Evaluate(List<Thing> thingsInRoom, int roomSize)
	{
		return RootCondition.Evaluate(thingsInRoom, roomSize);
	}
}
