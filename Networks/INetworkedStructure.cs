using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Trading;

namespace Networks;

public interface INetworkedStructure : INetworkMember, IReferencable, IEvaluable
{
	StructureNetwork StructureNetwork { get; set; }

	bool IsStructureCompleted { get; }

	Thing GetAsThing { get; }

	WorldGrid WorldGrid { get; }

	List<INetworkedStructure> ConnectedStructures();

	bool IsConnected(Connection otherEnd);

	void OnStructureNetworkUpdated();
}
