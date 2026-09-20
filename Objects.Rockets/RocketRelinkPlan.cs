using System.Collections.Generic;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Objects.Rockets;

public readonly struct RocketRelinkPlan(RocketRelinkStatus status, Vector3 translation, int anchorsMatched, int anchorCount, int coverage, int occupiedTargets, List<Structure> moveSet)
{
	public readonly RocketRelinkStatus Status = status;

	public readonly Vector3 Translation = translation;

	public readonly int AnchorsMatched = anchorsMatched;

	public readonly int AnchorCount = anchorCount;

	public readonly int Coverage = coverage;

	public readonly int OccupiedTargets = occupiedTargets;

	public readonly List<Structure> MoveSet = moveSet;

	public bool IsReady => Status == RocketRelinkStatus.Ready;

	public static RocketRelinkPlan Failed(RocketRelinkStatus status, int anchorCount = 0)
	{
		return new RocketRelinkPlan(status, Vector3.zero, 0, anchorCount, 0, 0, null);
	}
}
