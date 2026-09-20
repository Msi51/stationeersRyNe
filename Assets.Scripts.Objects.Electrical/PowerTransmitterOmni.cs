using System.Collections.Generic;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class PowerTransmitterOmni : Electrical
{
	[Header("PowerTransmitterOmni")]
	[Tooltip("The Maximum Distance that the device can charge a battery")]
	[SerializeField]
	private float _maxDistance = 15f;

	[Tooltip("How much power (in Watts) can it supply to one battery (Note: 25-75% of this will be lost depending on transmission distance).")]
	[SerializeField]
	private float _batteryChargeRate = 300f;

	[Tooltip("The Maximum power the unit will draw to charge all nearby batteries.")]
	[SerializeField]
	private float _maximumPowerUsage = 2000f;

	private readonly List<WirelessBattery> _batteriesInRange = new List<WirelessBattery>();

	private readonly List<WirelessBattery> _batteriesToCharge = new List<WirelessBattery>();

	private readonly List<WirelessBattery> _batteriesUsingPower = new List<WirelessBattery>();

	private float _powerRequired;

	public float MaxDistance => _maxDistance;

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		LocateBatteries();
		CalculateUsedPower();
	}

	public void LocateBatteries()
	{
		_batteriesInRange.Clear();
		if (!OnOff)
		{
			return;
		}
		foreach (WirelessBattery allWirelessBattery in WirelessBattery.AllWirelessBatteries)
		{
			float num = Vector3.Distance(base.Position, allWirelessBattery.RootParent.Position);
			if (num > _maxDistance)
			{
				if (allWirelessBattery.LinkedOmni == this)
				{
					allWirelessBattery.LinkedOmni = null;
					allWirelessBattery.RangeToOmni = float.MaxValue;
				}
			}
			else if ((!allWirelessBattery.ParentAsDevice || allWirelessBattery.ParentAsDevice is IBatteryPowered) && (!(allWirelessBattery.LinkedOmni != null) || !(allWirelessBattery.LinkedOmni != this) || !allWirelessBattery.LinkedOmni.OnOff || !allWirelessBattery.LinkedOmni.Powered || allWirelessBattery.LinkedOmni.Error != 0 || !(allWirelessBattery.RangeToOmni < num)))
			{
				allWirelessBattery.LinkedOmni = this;
				allWirelessBattery.RangeToOmni = num;
				_batteriesInRange.Add(allWirelessBattery);
			}
		}
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		if (powerAdded < UsedPower)
		{
			return;
		}
		powerAdded -= UsedPower;
		_batteriesToCharge.Clear();
		_batteriesToCharge.AddRange(_batteriesInRange);
		float num = 0.1f;
		while (_batteriesToCharge.Count > 0 && powerAdded > num)
		{
			float num2 = powerAdded / (float)_batteriesToCharge.Count;
			for (int num3 = _batteriesToCharge.Count - 1; num3 >= 0; num3--)
			{
				if (_batteriesToCharge[num3].LinkedOmni == null || _batteriesToCharge[num3].LinkedOmni != this)
				{
					_batteriesToCharge.RemoveAt(num3);
				}
				else
				{
					float num4 = _batteriesToCharge[num3].AddPowerWireless(num2);
					powerAdded -= num2 - num4;
					if (_batteriesToCharge[num3] == null || _batteriesToCharge[num3].IsCharged || _batteriesToCharge[num3].CalculateUniqueRatioIdentifier() >= 0.999f)
					{
						_batteriesToCharge.RemoveAt(num3);
					}
				}
			}
		}
		base.ReceivePower(cableNetwork, powerAdded);
	}

	private void CalculateUsedPower()
	{
		_batteriesUsingPower.Clear();
		_batteriesUsingPower.AddRange(_batteriesInRange);
		float num = 0f;
		num += UsedPower;
		foreach (WirelessBattery item in _batteriesUsingPower)
		{
			if (!item.IsCharged && item.LinkedOmni != null && item.LinkedOmni == this)
			{
				num += Mathf.Min(item.PowerDelta, _batteryChargeRate);
			}
		}
		_powerRequired = Mathf.Min(num, _maximumPowerUsage);
	}

	public override float GetUsedPower([NotNull] CableNetwork cableNetwork)
	{
		if (base.PowerCable == null || base.PowerCable.CableNetwork != cableNetwork)
		{
			return -1f;
		}
		if (!OnOff)
		{
			return 0f;
		}
		return _powerRequired;
	}
}
