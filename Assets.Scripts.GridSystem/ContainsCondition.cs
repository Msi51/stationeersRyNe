using System.Collections.Generic;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.GridSystem;

public class ContainsCondition : RoomRuleCondition
{
	public int PrefabHash;

	public ContainsCondition(ContainsConditionData data)
	{
		PrefabHash = Animator.StringToHash(data.PrefabName);
		RoomManager.AddContributingPrefab(PrefabHash);
	}

	public override bool Evaluate(List<Thing> thingsInRoom, int roomSize)
	{
		foreach (Thing item in thingsInRoom)
		{
			if (item.PrefabHash == PrefabHash)
			{
				return true;
			}
		}
		return false;
	}
}
