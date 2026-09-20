using Assets.Scripts.Objects;

namespace Objects.Electrical;

public struct PlantToFertiliserSlotMapping(Slot plantSlot, Slot fertiliserSlot)
{
	public readonly Slot PlantSlot = plantSlot;

	public readonly Slot FertiliserSlot = fertiliserSlot;
}
