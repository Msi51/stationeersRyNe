using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Trading;

namespace Assets.Scripts;

public interface IPortablesConnector : IFastenedConnector, IReferencable, IEvaluable
{
	Slot TankSlot { get; }

	Pipe.ContentType PipeContentType { get; }
}
