using Assets.Scripts.Objects;

namespace Trading;

public interface ITradable : IEvaluable
{
	float GetQuantity { get; }

	float GetTradableQuantity { get; }

	void SetQuantity(float value);

	int GetPrefabHash();

	Slot GetNextFreeSlot();

	Slot GetSlot(int slotIndex);

	Slot GetNextFreeSlot(string slotId);
}
