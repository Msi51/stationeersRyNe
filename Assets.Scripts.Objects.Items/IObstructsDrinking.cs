using Assets.Scripts.Objects.Clothing;
using Trading;

namespace Assets.Scripts.Objects.Items;

public interface IObstructsDrinking : IWearable, IReferencable, IEvaluable
{
	bool IsOpen { get; }
}
