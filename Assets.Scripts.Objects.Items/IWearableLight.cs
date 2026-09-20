using Trading;

namespace Assets.Scripts.Objects.Items;

public interface IWearableLight : IReferencable, IEvaluable
{
	Thing GetAsThing { get; }

	bool OnOff { get; }

	long netId { get; }
}
