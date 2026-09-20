using Assets.Scripts.Objects.Pipes;
using Trading;

namespace Assets.Scripts.Objects.Items;

public interface ISetable : ILogicable, IReferencable, IEvaluable
{
	double Setting { get; set; }

	new string DisplayName { get; }

	long NetworkId { get; }
}
