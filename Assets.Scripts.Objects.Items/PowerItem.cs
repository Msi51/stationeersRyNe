using Assets.Scripts.Localization2;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class PowerItem : CharacterItem
{
	[Tooltip("Battery Cell that has been inserted")]
	public BatteryCell Battery;

	public float PowerUsedPerTick = 10f;

	private BatteryCell _batteryPrefab;

	public virtual bool IsOperable
	{
		get
		{
			if (Battery != null)
			{
				return !Battery.IsEmpty;
			}
			return false;
		}
	}

	public override bool OnUseItem(float quantity, Thing onUseThing)
	{
		if (Battery == null || Battery.IsEmpty)
		{
			return false;
		}
		Battery.PowerStored -= quantity;
		return true;
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (Battery == null || Battery.IsEmpty)
		{
			if (OnOff)
			{
				OnServer.Interact(base.InteractOnOff, 0);
			}
		}
		else
		{
			Battery.PowerStored -= PowerUsedPerTick;
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.OnOff)
		{
			if (Battery == null || Battery.IsEmpty)
			{
				return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.PowerItemNoPower);
			}
			if (!doAction)
			{
				return DelayedActionInstance.Success(interactable.ContextualName);
			}
			OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			return DelayedActionInstance.Success(interactable.ContextualName);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public void AddBattery()
	{
		if (!_batteryPrefab)
		{
			_batteryPrefab = Prefab.Find("ItemBatteryCell") as BatteryCell;
		}
		Slot slot = Slots.Find((Slot slot2) => slot2.Type == _batteryPrefab.SlotType);
		BatteryCell batteryCell = Thing.Create<BatteryCell>(_batteryPrefab, slot.Location);
		batteryCell.PowerStored = batteryCell.PowerMaximum;
		OnServer.MoveToSlot(batteryCell, slot);
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		BatteryCell batteryCell = newChild as BatteryCell;
		if (batteryCell != null)
		{
			Battery = batteryCell;
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (previousChild == Battery)
		{
			Battery = null;
		}
	}
}
