using System.Collections.Generic;
using Assets.Scripts.Objects;
using Trading;

public interface ITradableInventory : IReferencable, IEvaluable
{
	List<DynamicThing> GetContents();

	List<Slot> GetSlots();

	bool IsSlotTradable(Slot slot);
}
