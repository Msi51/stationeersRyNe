using Networks;
using Trading;

namespace Objects.RoboticArm;

public interface INetworkedRoboticArm : INetworkedStructure, INetworkMember, IReferencable, IEvaluable
{
	RoboticArmNetwork RoboticArmNetwork { get; }
}
