using Assets.Scripts.Objects;
using Networks;
using Trading;

namespace Assets.Scripts.Networks;

public interface INetworkedPipe : INetworkedAtmospherics, INetworkedStructure, INetworkMember, IReferencable, IEvaluable, ISmallGrid, ITooltip
{
	PipeNetwork PipeNetwork { get; }

	bool ProhibitConnection(SmallGrid potentialConnection);
}
