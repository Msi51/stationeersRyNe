using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Objects.Items;
using UnityEngine;

public class ApplianceTabletDock : Appliance
{
	public float BatteryChargeRate = 200f;

	public Slot TabletSlot => Slots[0];

	public override float GetUsedPower()
	{
		if (!OnOff)
		{
			return 0f;
		}
		float num = base.GetUsedPower();
		if (TabletSlot.Contains<IBatteryPowered>(out var occupant) && occupant.BatterySlot.Contains<BatteryCell>(out var occupant2) && !occupant2.IsCharged)
		{
			num += Mathf.Min(BatteryChargeRate, occupant2.PowerDelta);
		}
		return num;
	}

	public override float ReceivePower(float powerReceived)
	{
		powerReceived = base.ReceivePower(powerReceived);
		if (TabletSlot.Contains<IBatteryPowered>(out var occupant) && occupant.BatterySlot.Contains<BatteryCell>(out var occupant2) && !occupant2.IsCharged)
		{
			float num = Mathf.Min(BatteryChargeRate, occupant2.PowerDelta);
			if (powerReceived >= num)
			{
				occupant2.PowerStored += num;
				powerReceived -= num;
			}
		}
		return powerReceived;
	}

	public override void BenchPowerStateChanged(bool receivingPower)
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(this, InteractableType.Powered, (receivingPower && OnOff) ? 1 : 0);
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild is Tablet tablet)
		{
			tablet.OnDocked();
		}
	}
}
