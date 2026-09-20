using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class CableToolBelt : ToolBelt
{
	private static readonly int CableAmmoHash = Animator.StringToHash("CableAmmo");

	public int CableAmmoSlotCount = 6;

	public override bool TryCollect(Item item)
	{
		if (!(item is Stackable stackable))
		{
			return false;
		}
		Slot slot = null;
		for (int i = 0; i < Slots.Count; i++)
		{
			Slot slot2 = Slots[i];
			if (slot2.StringHash != CableAmmoHash || !slot2.TakesSpecificItem(item.PrefabHash))
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
			else if (dynamicThing is Stackable stackable2 && stackable2.CanStack(stackable))
			{
				Thing.Merge(stackable2, stackable);
				if (stackable.Quantity <= 0)
				{
					return true;
				}
			}
		}
		if (slot == null)
		{
			return false;
		}
		OnServer.MoveToSlot(item, slot);
		return true;
	}

	public MultiConstructor FindMatchingCoil(int prefabHash)
	{
		for (int i = 0; i < Slots.Count; i++)
		{
			if (Slots[i]?.Occupant is MultiConstructor multiConstructor && multiConstructor.PrefabHash == prefabHash && multiConstructor.Quantity > 0)
			{
				return multiConstructor;
			}
		}
		return null;
	}

	public int TotalMatchingCoilQuantity(int prefabHash)
	{
		int num = 0;
		for (int i = 0; i < Slots.Count; i++)
		{
			if (Slots[i]?.Occupant is MultiConstructor multiConstructor && multiConstructor.PrefabHash == prefabHash && multiConstructor.Quantity > 0)
			{
				num += multiConstructor.Quantity;
			}
		}
		return num;
	}
}
