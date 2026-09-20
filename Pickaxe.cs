using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Trading;
using UnityEngine;

public class Pickaxe : Tool, IMiningTool, IReferencable, IEvaluable
{
	[Header("Pickaxe")]
	[Tooltip("Time it takes to mine 1 asteriod voxel")]
	public float MineCompletionTime = 0.5f;

	[Tooltip("The amount that this tool mines at a time")]
	public float MineAmount = 0.5f;

	public override int EquipSoundHash => Crowbar.EquipCrowbarHash;

	public override int UnEquipSoundHash => Crowbar.UnEquipCrowbarHash;

	public CursorVoxelMode CursorVoxelMode => (CursorVoxelMode)Mode;

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		if (parent.HasAuthority && base.ParentSlot == InventoryManager.ActiveHandSlot)
		{
			CursorManager instance = CursorManager.Instance;
			instance.CursorHitMask = (int)instance.CursorHitMask | (int)LayerMasks.CursorVoxel;
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.ManualTools);
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		if (oldParent.HasAuthority)
		{
			CursorManager instance = CursorManager.Instance;
			instance.CursorHitMask = (int)instance.CursorHitMask & ~(int)LayerMasks.CursorVoxel;
		}
	}

	public bool IsAvailable()
	{
		return true;
	}

	public float GetMineCompletionTime()
	{
		return MineCompletionTime;
	}

	public float GetMineAmount()
	{
		return MineAmount;
	}
}
