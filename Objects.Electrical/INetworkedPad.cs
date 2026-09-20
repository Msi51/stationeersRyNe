using Assets.Scripts.Objects;
using Networks;
using Trading;

namespace Objects.Electrical;

public interface INetworkedPad : INetworkedStructure, INetworkMember, IReferencable, IEvaluable
{
	new bool IsConnected(Connection connection);
}
