using System.Collections.Generic;
using Objects.Rockets;
using Trading;

namespace Networks;

public interface INetworkedRocketPart : INetworkedStructure, INetworkMember, IReferencable, IEvaluable, IRocketComponent
{
	RocketNetwork RocketNetwork { get; }

	List<RocketInternalCellOffset> InternalCellOffsets { get; }

	void OnImGuiDraw();
}
