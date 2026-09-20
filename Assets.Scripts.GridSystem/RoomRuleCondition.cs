using System.Collections.Generic;
using Assets.Scripts.Objects;

namespace Assets.Scripts.GridSystem;

public abstract class RoomRuleCondition
{
	public abstract bool Evaluate(List<Thing> thingsInRoom, int roomSize);
}
