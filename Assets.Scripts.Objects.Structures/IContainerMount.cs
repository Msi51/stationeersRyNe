using Trading;

namespace Assets.Scripts.Objects.Structures;

public interface IContainerMount : IFastenedConnector, IReferencable, IEvaluable
{
	Slot ContainerSlot { get; }
}
