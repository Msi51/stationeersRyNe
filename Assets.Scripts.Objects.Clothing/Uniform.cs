using Assets.Scripts.Objects.Entities;

namespace Assets.Scripts.Objects.Clothing;

public class Uniform : Clothing
{
	public override int GetAccess
	{
		get
		{
			int num = base.GetAccess;
			foreach (Slot slot in Slots)
			{
				if (slot.Type == Slot.Class.AccessCard && (bool)slot.Occupant && slot.Occupant.GetAccess != 0)
				{
					num |= slot.Occupant.GetAccess;
				}
			}
			return num;
		}
	}

	public override int GetCurrencySlot()
	{
		int result = base.GetAccess;
		for (int i = 0; i < Slots.Count; i++)
		{
			if (Slots[i].Type == Slot.Class.CreditCard && (bool)Slots[i].Occupant)
			{
				result = i;
			}
		}
		return result;
	}

	public override void RefreshVisibility()
	{
		base.RefreshVisibility();
		Human human = base.ParentSlot.Parent as Human;
		if (!human || !base.IsChild)
		{
			return;
		}
		foreach (SkinnedMeshRendererInstance skinnedMesh in SkinnedMeshes)
		{
			skinnedMesh.Parent.GameObject.layer = human.gameObject.layer;
		}
	}

	public override void SetWearableVisibility(bool clothingOn)
	{
		if (clothingOn)
		{
			SetWearableVisibleInternal(isArmor: false);
		}
	}
}
