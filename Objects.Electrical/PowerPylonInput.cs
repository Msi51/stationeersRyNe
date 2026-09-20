using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Assets.Scripts.Networks;
using UnityEngine;

namespace Objects.Electrical;

public class PowerPylonInput : PowerPylonTerminus
{
	private bool _isDirty = true;

	private PowerPylonOutput _partner;

	private bool PartnerValid
	{
		get
		{
			if ((bool)_partner)
			{
				return _partner.CanTransfer;
			}
			return false;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (PartnerValid)
			{
				return InputNetwork != null;
			}
			return false;
		}
	}

	public void SetDirty()
	{
		_isDirty = true;
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (_isDirty)
		{
			RecomputePartner();
		}
		if (base.CanTransfer && PartnerValid)
		{
			MovePowerToPartner();
		}
	}

	private void MovePowerToPartner()
	{
		float num = Mathf.Min(Mathf.Clamp(_partner.PowerMaximum - _partner.PowerStored, 0f, PowerMaximum), base.PowerStored);
		_partner.ReceivePower(null, num);
		base.PowerStored -= num;
	}

	private void SetPartner(PowerPylonOutput partner)
	{
		_partner = partner;
		CheckError();
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
		if (!OnOff)
		{
			base.LastPowerRemoved = 0f;
			return;
		}
		base.LastPowerRemoved = powerUsed;
		base.PowerStored = Mathf.Clamp(base.PowerStored - powerUsed, 0f, PowerMaximum);
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		if (Error == 1 || !OnOff)
		{
			base.LastPowerAdded = 0f;
			return;
		}
		base.LastPowerAdded = powerAdded;
		base.PowerStored = Mathf.Clamp(powerAdded + base.PowerStored, 0f, PowerMaximum);
	}

	public override float GetUsedPower([NotNull] CableNetwork cableNetwork)
	{
		if (InputNetwork == null || cableNetwork != InputNetwork)
		{
			return 0f;
		}
		if (Error == 1 && OnOff)
		{
			return UsedPower;
		}
		if (!OnOff)
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
		if (!OnOff)
		{
			return 0f;
		}
		return Mathf.Max(base.PowerStored, 0f);
	}

	public void RecomputePartner()
	{
		if (FindOutput(out var output))
		{
			SetPartner(output);
		}
		else
		{
			SetPartner(null);
		}
	}

	private bool FindOutput(out PowerPylonOutput output)
	{
		HashSet<long> hashSet = new HashSet<long>();
		Queue<PylonNode> queue = new Queue<PylonNode>();
		hashSet.Add(base.ReferenceId);
		queue.Enqueue(base.Nodes[0]);
		while (queue.Count > 0)
		{
			PylonNode pylonNode = queue.Dequeue();
			if (pylonNode.Owner is PowerPylonOutput powerPylonOutput)
			{
				output = powerPylonOutput;
				return true;
			}
			foreach (PylonConnection connection in pylonNode.Connections)
			{
				PylonNode other = connection.GetOther(pylonNode);
				long refId = other.Owner.GetRefId();
				if (hashSet.Add(refId))
				{
					queue.Enqueue(other);
				}
			}
		}
		output = null;
		return false;
	}
}
