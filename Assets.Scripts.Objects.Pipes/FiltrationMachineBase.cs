using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class FiltrationMachineBase : DeviceInputOutputCircuit
{
	protected float _powerUsedDuringTick;

	public static readonly float EnergyPerAtmosphere = 100f;

	public static readonly float MinimumRatioToFilterAll = 0.001f;

	public bool IsFullyConnected
	{
		get
		{
			if (InputNetwork != null && OutputNetwork != null)
			{
				return OutputNetwork2 != null;
			}
			return false;
		}
	}

	protected override Slot ProgrammableChipSlot
	{
		get
		{
			List<Slot> slots = Slots;
			if (slots == null || slots.Count <= 2)
			{
				return null;
			}
			return Slots[2];
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (!base.IsStructureCompleted)
			{
				return false;
			}
			bool flag = base.ProgrammableChip != null && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
			bool flag2 = IsFullyConnected && !flag;
			if (Error == 1)
			{
				if (!flag2)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (flag2)
			{
				return true;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
		}
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (!OnOff || cableNetwork != base.PowerCableNetwork || base.PowerCableNetwork == null)
		{
			return 0f;
		}
		if (!IsOperable)
		{
			return UsedPower;
		}
		return UsedPower + _powerUsedDuringTick;
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded);
		_powerUsedDuringTick = 0f;
	}

	public bool HasEmptyFilter()
	{
		foreach (Slot slot in Slots)
		{
			if (slot.Type == Slot.Class.GasFilter && slot.Contains<GasFilter>(out var occupant) && occupant.IsEmpty)
			{
				return true;
			}
		}
		return false;
	}

	protected override void ToggleInteractableColliders()
	{
		foreach (Interactable interactable in Interactables)
		{
			InteractableType action = interactable.Action;
			if (action == InteractableType.Slot3 || (uint)(action - 31) <= 1u)
			{
				Collider collider = interactable.Collider;
				int num;
				if (IsOpen)
				{
					BuildState currentBuildState = base.CurrentBuildState;
					List<BuildState> buildStates = BuildStates;
					num = ((currentBuildState == buildStates[buildStates.Count - 1]) ? 1 : 0);
				}
				else
				{
					num = 0;
				}
				collider.enabled = (byte)num != 0;
			}
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = new PassiveTooltip(true);
		passiveTooltip.Title = DisplayName;
		if (hitCollider == InputConnection.Collider)
		{
			passiveTooltip = passiveTooltip.Populate(InputConnection);
			passiveTooltip.Title = InterfaceStrings.ConnectionInput;
			return passiveTooltip;
		}
		if (hitCollider == OutputConnection2.Collider)
		{
			passiveTooltip = passiveTooltip.Populate(OutputConnection2);
			passiveTooltip.Title = InterfaceStrings.ConnectionUnfiltered;
			return passiveTooltip;
		}
		if (hitCollider == OutputConnection.Collider)
		{
			passiveTooltip = passiveTooltip.Populate(OutputConnection);
			passiveTooltip.Title = InterfaceStrings.ConnectionFiltered;
			return passiveTooltip;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	protected override StringBuilder GetInfoPanelOperationText()
	{
		StringBuilder infoPanelOperationText = base.GetInfoPanelOperationText();
		PressurekPa val = PressurekPa.Zero;
		if (InputNetwork?.Atmosphere != null && OutputNetwork?.Atmosphere != null && OutputNetwork2?.Atmosphere != null)
		{
			val = InputNetwork.Atmosphere.PressureGasses - RocketMath.Max(OutputNetwork.Atmosphere.PressureGasses, OutputNetwork2.Atmosphere.PressureGasses);
		}
		val = RocketMath.Max(PressurekPa.Zero, val);
		PressurekPa pressurekPa = val * 1000.0;
		infoPanelOperationText.AppendLine(GameStrings.MachinePressureDifferential.AsString(pressurekPa.ToFloat().ToStringPrefix("Pa", "yellow")));
		return infoPanelOperationText;
	}

	public override void OnAtmosphericTick()
	{
		if (!OnOff || !Powered || Mode == 0 || !IsOperable)
		{
			_powerUsedDuringTick = 0f;
			base.ProcessedMoles = MoleQuantity.Zero;
			return;
		}
		_powerUsedDuringTick = Mathf.Lerp(0f, EnergyPerAtmosphere, (InputNetwork.Atmosphere.PressureGassesAndLiquids / base.PressurePerTick).ToFloat());
		PressurekPa inputPressureDelta = InputNetwork.Atmosphere.PressureGasses - RocketMath.Max(OutputNetwork.Atmosphere.PressureGasses, OutputNetwork2.Atmosphere.PressureGasses);
		MoleQuantity transferMoles;
		GasMixture fromMix = AtmosphereHelper.TakeNormalisedGasPressureScaled(InputNetwork.Atmosphere, base.PressurePerTick, inputPressureDelta, out transferMoles);
		foreach (Slot slot in Slots)
		{
			if (!fromMix.IsValid)
			{
				return;
			}
			if (slot.Type == Slot.Class.GasFilter && slot.Contains<GasFilter>(out var occupant) && !occupant.IsEmpty)
			{
				occupant.FilterGas(ref fromMix, ref OutputNetwork.Atmosphere.GasMixture, InputNetwork.Atmosphere, MinimumRatioToFilterAll);
			}
		}
		base.ProcessedMoles = transferMoles;
		OutputNetwork2.Atmosphere.Add(fromMix);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new FiltrationMachineSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}
}
