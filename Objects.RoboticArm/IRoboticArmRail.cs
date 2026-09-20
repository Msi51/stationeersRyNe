using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Trading;
using UnityEngine;

namespace Objects.RoboticArm;

public interface IRoboticArmRail : ISmallGrid, ITooltip, IReferencable, IEvaluable
{
	List<RailNode> RailNodes { get; }

	SmallCell SmallCell { get; set; }

	Vector3 Pivot { get; }

	SmallGrid AsSmallGrid { get; }

	Connection OtherEnd(Connection end);

	void RailNetworkUpdated();
}
