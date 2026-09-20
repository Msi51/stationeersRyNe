using Trading;

namespace Objects;

public interface IPowerGenerator : IReferencable, IEvaluable
{
	float GetMaxPowerGenerated();
}
