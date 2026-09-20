using Assets.Scripts.Objects.Items;
using Audio;

namespace Assets.Scripts.Objects.Pipes;

public interface IHarvestable : IAudioParent
{
	Slot InputSlot { get; }

	Slot FertilizerSlot { get; }

	Plant GetPlant { get; }

	new bool IsBeingDestroyed { get; }

	Thing GetThing { get; }

	bool HasAnySlots { get; }

	Slot GetSlot(int slotIndex);
}
