using System;
using Assets.Scripts.Objects;

namespace Assets.Scripts.Vehicles;

[Serializable]
public class ContainerSlot
{
	public Thing self;

	public int[] ContainerSlots;

	public int[] TankSlots;

	public bool TryAttachTank(Thing thing)
	{
		Slot slot = CanPlaceTank();
		return Attach(thing, slot);
	}

	public bool TryAttachContainer(Thing thing)
	{
		Slot slot = CanPlaceContainer();
		return Attach(thing, slot);
	}

	public bool Attach(Thing thing, Slot slot)
	{
		if (slot != null && thing is DynamicThing childThing)
		{
			OnServer.MoveToSlot(childThing, slot);
			return true;
		}
		return false;
	}

	public Slot CanPlaceTank()
	{
		if (!HasAnyContainer())
		{
			return null;
		}
		return ReturnFirstOccupant(TankSlots);
	}

	public Slot CanPlaceContainer()
	{
		if (HasAnyTanks())
		{
			return null;
		}
		return ReturnFirstOccupant(ContainerSlots);
	}

	public bool HasAnyContainer()
	{
		if (ReturnFirstOccupant(ContainerSlots) != null)
		{
			return true;
		}
		return false;
	}

	public bool HasAnyTanks()
	{
		int[] tankSlots = TankSlots;
		foreach (int num in tankSlots)
		{
			if (num < self.Slots.Count)
			{
				Slot slot = self.Slots[num];
				if (slot == null || (bool)slot.Occupant)
				{
					return true;
				}
			}
		}
		return false;
	}

	public Slot ReturnFirstOccupant(int[] slotIndexs)
	{
		foreach (int num in slotIndexs)
		{
			if (num < self.Slots.Count)
			{
				Slot slot = self.Slots[num];
				if (slot != null && !slot.Occupant)
				{
					return slot;
				}
			}
		}
		return null;
	}
}
