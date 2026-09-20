using Assets.Scripts.Objects.Pipes;
using Trading;

namespace Assets.Scripts.Objects.Electrical;

public interface ITransmitable : ILogicable, IReferencable, IEvaluable
{
	void OnTransmitterCreated();
}
