using Trading;

namespace Assets.Scripts.Objects.Items;

public interface IQuantity : ITradable, IEvaluable, IReferencable
{
	float GetMaxQuantity { get; }

	float GetRatioQuantity { get; }

	string ToTooltip();

	string GetQuantityText();
}
