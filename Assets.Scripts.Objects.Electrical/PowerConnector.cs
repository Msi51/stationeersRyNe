using Assets.Scripts.Networks;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class PowerConnector : Electrical
{
	[Header("Power Connector")]
	[ReadOnly]
	[Tooltip("Currently connected Generator")]
	public DynamicGenerator ConnectedDynamicGenerator;

	[Tooltip("Input Connection for this device")]
	public Connection InputConnection;

	public Slot ConnectedSlot => Slots[0];

	public override float GetGeneratedPower(CableNetwork cableNetwork)
	{
		if ((bool)ConnectedDynamicGenerator)
		{
			return ConnectedDynamicGenerator.PowerGenerated;
		}
		return base.GetGeneratedPower(cableNetwork);
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		ConnectedDynamicGenerator = newChild as DynamicGenerator;
		IsOpen = ConnectedDynamicGenerator != null;
		if (ConnectedDynamicGenerator != null)
		{
			ConnectedDynamicGenerator.Connected = true;
		}
	}

	public override void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		newChild.SetOnBaseOfSlot();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (ConnectedDynamicGenerator == previousChild)
		{
			ConnectedDynamicGenerator.Connected = false;
			ConnectedDynamicGenerator = null;
		}
		IsOpen = ConnectedDynamicGenerator != null;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		if (hitCollider == InputConnection.Collider)
		{
			result.Title = InterfaceStrings.ConnectionInput;
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}
}
