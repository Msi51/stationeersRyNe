using System.Collections.Generic;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public class Bench : Device, ISmartRotatable, IFastenedConnector, IReferencable, IEvaluable
{
	[Header("Bench")]
	public List<Appliance> Appliances = new List<Appliance>();

	[SerializeField]
	private GameObject cable1;

	[SerializeField]
	private GameObject cable2;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	private float AppliancePower
	{
		get
		{
			float num = 0f;
			foreach (Appliance appliance in Appliances)
			{
				if (appliance.OnOff)
				{
					num += appliance.GetUsedPower();
				}
			}
			return num;
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.ChairTableCategory);
	}

	public override string GetStationpediaCategoryKey()
	{
		return StationpediaCategoryStrings.ChairTableCategory;
	}

	protected override void SetPower(CableNetwork cableNetwork, bool hasPower)
	{
		base.SetPower(cableNetwork, hasPower);
		foreach (Appliance appliance in Appliances)
		{
			appliance.BenchPowerStateChanged(OnOff && Powered);
		}
		if (Powered == hasPower)
		{
			return;
		}
		foreach (Appliance appliance2 in Appliances)
		{
			OnServer.Interact(appliance2, InteractableType.Powered, (hasPower && appliance2.OnOff) ? 1 : 0);
		}
	}

	protected override void AssessPower(CableNetwork cableNetwork, bool isOn)
	{
		base.AssessPower(cableNetwork, isOn);
		if (!isOn || !Powered)
		{
			foreach (Appliance appliance in Appliances)
			{
				OnServer.Interact(appliance, InteractableType.Powered, 0);
			}
		}
		foreach (Appliance appliance2 in Appliances)
		{
			appliance2.BenchPowerStateChanged(isOn && Powered);
		}
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded);
		powerAdded -= UsedPower;
		foreach (Appliance appliance in Appliances)
		{
			if (appliance.OnOff)
			{
				powerAdded = appliance.ReceivePower(powerAdded);
			}
		}
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (!base.PowerCable || base.PowerCable.CableNetwork != cableNetwork)
		{
			return -1f;
		}
		if (!OnOff)
		{
			return 0f;
		}
		return UsedPower + AppliancePower;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		Appliance appliance = newChild as Appliance;
		if ((bool)appliance)
		{
			appliance.SetColliders(on: true, always: true);
			appliance.ParentBench = this;
			Slots[0].IsInteractable = !Slots[0].Occupant;
			Slots[1].IsInteractable = !Slots[1].Occupant;
			cable1.SetActive(Slots[0].Occupant);
			cable2.SetActive(Slots[1].Occupant);
			Appliances.Add(appliance);
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(appliance, InteractableType.Powered, (Powered && appliance.OnOff) ? 1 : 0);
			}
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		Appliance appliance = previousChild as Appliance;
		if ((bool)appliance)
		{
			appliance.ParentBench = null;
			Slots[0].IsInteractable = !Slots[0].Occupant;
			Slots[1].IsInteractable = !Slots[1].Occupant;
			cable1.SetActive(Slots[0].Occupant);
			cable2.SetActive(Slots[1].Occupant);
			Appliances.Remove(appliance);
			if (GameManager.RunSimulation && appliance.Powered)
			{
				OnServer.Interact(appliance, InteractableType.Powered, 0);
			}
		}
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}
}
