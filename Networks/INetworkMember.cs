using Trading;

namespace Networks;

public interface INetworkMember : IReferencable, IEvaluable
{
	ReferencableNetwork Network { get; set; }

	bool IsBeingDestroyed { get; }
}
