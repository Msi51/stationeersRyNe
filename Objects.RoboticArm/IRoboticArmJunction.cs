using Trading;

namespace Objects.RoboticArm;

public interface IRoboticArmJunction : IRoboticArmRail, ISmallGrid, ITooltip, IReferencable, IEvaluable
{
	int JunctionIndex { get; set; }
}
