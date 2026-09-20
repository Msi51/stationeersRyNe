using Trading;

namespace Assets.Scripts;

public interface IFastenedConnector : IReferencable, IEvaluable
{
	string ToTooltip();
}
