using Assets.Scripts.Objects.Items;
using Objects;

namespace Assets.Scripts.Objects;

public class MiningBelt : ToolBelt
{
	private static bool IsOreSlot(Slot slot)
	{
		Slot.Class type = slot.Type;
		return type == Slot.Class.None || type == Slot.Class.Ore;
	}

	private static bool CanMergeOreInto(Ore ore, Stackable target)
	{
		if ((object)target != null && target.PrefabHash == ore.PrefabHash)
		{
			return !target.IsStackFull;
		}
		return false;
	}

	public bool CanAddOre(Ore ore)
	{
		foreach (Slot slot in Slots)
		{
			DynamicThing dynamicThing = slot.Get();
			if (IsOreSlot(slot) && ((object)dynamicThing == null || CanMergeOreInto(ore, dynamicThing as Stackable)))
			{
				return true;
			}
		}
		return false;
	}

	public bool TryAddOre(Ore ore)
	{
		Slot slot = null;
		foreach (Slot slot2 in Slots)
		{
			if (!IsOreSlot(slot2))
			{
				continue;
			}
			DynamicThing dynamicThing = slot2.Get();
			if ((object)dynamicThing == null)
			{
				if (slot == null)
				{
					slot = slot2;
				}
			}
			else if (dynamicThing is Stackable stackable && CanMergeOreInto(ore, stackable))
			{
				Thing.Merge(stackable, ore);
				if (ore.Quantity <= 0)
				{
					return true;
				}
			}
		}
		if (slot != null)
		{
			OnServer.MoveToSlot(ore, slot);
			return true;
		}
		return false;
	}

	public bool CanMergeAsOre(DynamicThing source)
	{
		if (source is Ore ore)
		{
			return CanAddOre(ore);
		}
		return false;
	}

	public bool MergeAsOre(IMergeable mergeable)
	{
		if (!(mergeable is Ore ore))
		{
			return false;
		}
		TryAddOre(ore);
		return true;
	}

	public bool ContainsUnfilledDirtCanister(out DirtCanister dirtCanister)
	{
		foreach (Slot slot in Slots)
		{
			if (slot.Occupant is DirtCanister { DirtCanisterState: not DirtCanisterState.Full } dirtCanister2)
			{
				dirtCanister = dirtCanister2;
				return true;
			}
		}
		dirtCanister = null;
		return false;
	}
}
