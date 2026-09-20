using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class PortableAtmosphericsPowered : PortableAtmospherics
{
	[Tooltip("How much power does the device use per atmospheric tick")]
	public float UsedPower = 25f;

	[Tooltip("How much of the power used is converted to heat")]
	[Range(0f, 1f)]
	public float PowerEfficiency = 0.75f;

	[ReadOnly]
	public BatteryCell BatteryCell;

	[SerializeField]
	private LeverAnimationComponent openLever;

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (openLever != null)
		{
			openLever.RefreshState(skipAnimation);
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		BatteryCell batteryCell = newChild as BatteryCell;
		if (batteryCell != null)
		{
			BatteryCell = batteryCell;
			IsOperable();
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (BatteryCell == previousChild)
		{
			BatteryCell = null;
			if (Powered && GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractPowered, 0);
			}
			IsOperable();
		}
	}

	public virtual bool IsOperable()
	{
		return true;
	}
}
