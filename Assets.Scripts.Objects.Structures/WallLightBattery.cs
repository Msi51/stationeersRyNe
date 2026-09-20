using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using JetBrains.Annotations;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public class WallLightBattery : WallLight, IBatteryPowered, IPowered, IDensePoolable, IReferencable, IEvaluable
{
	[Header("Wall Ligh tBattery")]
	[Tooltip("How many watts are used to charge the battery?")]
	public float BatteryChargeRate = 200f;

	private uint _lastPoweredByCableOnTick;

	private bool WasPoweredByCableLastTick => _lastPoweredByCableOnTick >= GameManager.GameTickCount;

	public Slot BatterySlot => Slots[0];

	public BatteryCell Battery => BatterySlot.Get<BatteryCell>();

	protected override void AssessPower(CableNetwork cableNetwork, bool isOn)
	{
		CheckPowerState();
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		float num = UsedPower - powerAdded;
		if (num > 0f)
		{
			if (!BatterySlot.Contains<BatteryCell>(out var occupant) || occupant.IsEmpty)
			{
				CheckPowerState();
				return;
			}
			float num2 = Mathf.Min(occupant.PowerStored, num);
			occupant.PowerStored -= num2;
			base.ReceivePower(cableNetwork, powerAdded);
		}
		else
		{
			_lastPoweredByCableOnTick = GameManager.GameTickCount;
			if (powerAdded > UsedPower)
			{
				Recharge(powerAdded - UsedPower);
			}
			base.ReceivePower(cableNetwork, powerAdded);
		}
	}

	public override float GetUsedPower([NotNull] CableNetwork cableNetwork)
	{
		if (base.PowerCableNetwork == null || cableNetwork != base.PowerCableNetwork)
		{
			return 0f;
		}
		float num = (OnOff ? UsedPower : 0f);
		if (BatterySlot.Contains<BatteryCell>(out var occupant) && !occupant.IsCharged)
		{
			num += Mathf.Min(BatteryChargeRate, occupant.PowerDelta);
		}
		return num;
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (GameManager.RunSimulation && OnOff && !IsCursor && GameManager.GameState == GameState.Running)
		{
			CheckPowerState();
			if (!WasPoweredByCableLastTick && BatterySlot.Contains<BatteryCell>(out var occupant))
			{
				occupant.PowerStored -= UsedPower;
			}
		}
	}

	public void Recharge(float amount)
	{
		if (BatterySlot.Contains<BatteryCell>(out var occupant))
		{
			occupant.PowerStored += amount;
		}
	}

	private bool HasPower()
	{
		if (WasPoweredByCableLastTick && base.PowerCableNetwork != null)
		{
			return true;
		}
		if (BatterySlot.Contains<BatteryCell>(out var occupant))
		{
			return !occupant.IsEmpty;
		}
		return false;
	}

	private void CheckPowerState()
	{
		if (Powered && (!OnOff || !HasPower()))
		{
			OnServer.Interact(base.InteractPowered, 0, skipAnimation: true);
		}
		else if (!Powered && OnOff && HasPower())
		{
			OnServer.Interact(base.InteractPowered, 1, skipAnimation: true);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		CheckPowerState();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		CheckPowerState();
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		CheckPowerState();
	}
}
