using System.Diagnostics.CodeAnalysis;
using Assets.Scripts.Networks;
using UnityEngine;

namespace Objects.Electrical;

public class PowerPylonOutput : PowerPylonTerminus
{
	protected override bool IsOperable
	{
		get
		{
			if (OutputNetwork == null)
			{
				return false;
			}
			return base.IsOperable;
		}
	}

	public override void OnAddCableNetwork(CableNetwork newNetwork)
	{
		base.OnAddCableNetwork(newNetwork);
		CheckError();
	}

	public override void OnRemoveCableNetwork(CableNetwork oldNetwork)
	{
		base.OnRemoveCableNetwork(oldNetwork);
		CheckError();
	}

	public override void UsePower(CableNetwork cableNetwork, float powerUsed)
	{
		base.LastPowerRemoved = powerUsed;
		base.PowerStored = Mathf.Clamp(base.PowerStored - powerUsed, 0f, PowerMaximum);
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.LastPowerAdded = powerAdded;
		base.PowerStored = Mathf.Clamp(powerAdded + base.PowerStored, 0f, PowerMaximum);
	}

	public override float GetUsedPower([NotNull] CableNetwork cableNetwork)
	{
		if (InputNetwork == null || Error == 1 || cableNetwork != InputNetwork)
		{
			return 0f;
		}
		return UsedPower + Mathf.Clamp(PowerMaximum - base.PowerStored, 0f, PowerMaximum);
	}

	public override float GetGeneratedPower(CableNetwork cableNetwork)
	{
		if (OutputNetwork == null || Error == 1 || cableNetwork != OutputNetwork)
		{
			return 0f;
		}
		return Mathf.Max(base.PowerStored, 0f);
	}
}
