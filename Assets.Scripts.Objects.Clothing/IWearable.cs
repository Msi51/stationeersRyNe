using Trading;

namespace Assets.Scripts.Objects.Clothing;

public interface IWearable : IReferencable, IEvaluable
{
	Slot ParentSlot { get; }

	void SetWearableVisibility(bool clothingOn);

	void RefreshVisibility();
}
