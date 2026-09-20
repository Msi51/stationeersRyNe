using Trading;
using UnityEngine;

namespace Objects.RoboticArm;

public interface IRoboticArmBypass : IRoboticArmJunction, IRoboticArmRail, ISmallGrid, ITooltip, IReferencable, IEvaluable
{
	Vector3 BypassPosition { get; }

	bool CanOpen { get; }

	bool CanClose { get; }

	void SetOpen(int state);
}
