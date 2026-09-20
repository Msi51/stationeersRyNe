using Networks;
using Trading;

namespace Objects.Rockets.Scanning;

public interface IRocketActionProgressableTarget : IReferencable, IEvaluable
{
	RocketNetwork RocketNetwork { get; }
}
