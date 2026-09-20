using Assets.Scripts.Objects.Clothing;
using Trading;

namespace Assets.Scripts.Objects.Items;

public interface IObstructsEating : IWearable, IReferencable, IEvaluable
{
	bool IsOpen { get; }
}
