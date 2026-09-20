using System.Collections.Generic;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class BatteryCellCharger : Electrical
{
	public List<BatteryCell> Batteries = new List<BatteryCell>();

	[Tooltip("How many watts are used to charge the battery?")]
	public float BatteryChargeRate = 500f;

	public int BatteriesNeedingCharge;

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		if (powerAdded < UsedPower)
		{
			return;
		}
		powerAdded -= UsedPower;
		List<BatteryCell> list = new List<BatteryCell>(Batteries.Count);
		foreach (BatteryCell battery in Batteries)
		{
			if (!battery.IsCharged)
			{
				list.Add(battery);
				if (Activate == 0)
				{
					OnServer.Interact(base.InteractActivate, 1);
				}
			}
			if (list.Count <= 0 && Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
		}
		float num = 0.1f;
		while (list.Count > 0 && powerAdded > num)
		{
			float num2 = powerAdded / (float)list.Count;
			for (int num3 = list.Count - 1; num3 >= 0; num3--)
			{
				float num4 = list[num3].AddPowerSafe(num2);
				powerAdded -= num2 - num4;
				if (list[num3].IsCharged)
				{
					list.RemoveAt(num3);
				}
				else if (list[num3].CalculateUniqueRatioIdentifier() >= 0.999f)
				{
					list.RemoveAt(num3);
				}
			}
		}
		base.ReceivePower(cableNetwork, powerAdded);
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (base.PowerCable == null || base.PowerCable.CableNetwork != cableNetwork)
		{
			return -1f;
		}
		if (!OnOff)
		{
			return 0f;
		}
		float num = base.GetUsedPower(cableNetwork);
		BatteriesNeedingCharge = 0;
		foreach (BatteryCell battery in Batteries)
		{
			if (!battery.IsCharged)
			{
				num += Mathf.Min(battery.PowerDelta, BatteryChargeRate);
			}
		}
		return num;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		BatteryCell batteryCell = newChild as BatteryCell;
		if (batteryCell != null)
		{
			Batteries.Add(batteryCell);
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		BatteryCell batteryCell = previousChild as BatteryCell;
		if (batteryCell != null)
		{
			Batteries.Remove(batteryCell);
		}
	}

	public override CanConstructInfo CanConstruct()
	{
		CanMountResult canMountResult = CanMountOnWall();
		if (canMountResult.result == WallMountResult.Valid)
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(canMountResult.ResultMessage());
	}
}
