using System.Collections.Generic;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class WirelessBattery : BatteryCell
{
	public PowerTransmitterOmni LinkedOmni;

	public float RangeToOmni;

	public static List<WirelessBattery> AllWirelessBatteries = new List<WirelessBattery>();

	private static readonly AnimationCurve efficiencyOverDistanceMultiplier = new AnimationCurve(new Keyframe(0f, 0.75f), new Keyframe(1f, 0.25f));

	public Device ParentAsDevice { get; private set; }

	public override void Awake()
	{
		base.Awake();
		if (!IsCursor)
		{
			AllWirelessBatteries.Add(this);
		}
	}

	public float AddPowerWireless(float availablePower)
	{
		availablePower *= efficiencyOverDistanceMultiplier.Evaluate(Mathf.Clamp01(RangeToOmni / LinkedOmni.MaxDistance));
		return AddPowerSafe(availablePower);
	}

	public override void OnDestroy()
	{
		AllWirelessBatteries.Remove(this);
		base.OnDestroy();
	}

	public override void OnEnterInventory(Thing parent)
	{
		if (parent is Device device && !(device is IBatteryPowered))
		{
			ParentAsDevice = device;
			LinkedOmni = null;
			RangeToOmni = float.MaxValue;
		}
		base.OnEnterInventory(parent);
	}

	public override void OnExitInventory(Thing parent)
	{
		ParentAsDevice = null;
		base.OnExitInventory(parent);
	}
}
