using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Networks;
using Objects.Items;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public class SuitStorage : DeviceInputOutput, ILogicAtmospheric, IDensePoolable, IMemoryReadable, IMemory, IInstructable, ILogicStack, IRocketInternals, IRocketComponent
{
	private const int STACK_SIZE = 24;

	private readonly LogicStack _stack = new LogicStack(24);

	public Slot HelmetSlot => Slots[0];

	public Slot SuitSlot => Slots[1];

	public Slot BackSlot => Slots[2];

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	private GasMask Helmet
	{
		get
		{
			if (!HelmetSlot.Occupant)
			{
				return null;
			}
			return HelmetSlot.Get<GasMask>();
		}
	}

	private ISuit SpaceSuit
	{
		get
		{
			if (!SuitSlot.Occupant)
			{
				return null;
			}
			return SuitSlot.Get<ISuit>();
		}
	}

	private Jetpack Jetpack
	{
		get
		{
			if (!BackSlot.Occupant)
			{
				return null;
			}
			return BackSlot.Get<Jetpack>();
		}
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public int GetStackSize()
	{
		return _stack.Size;
	}

	public LogicStack GetLogicStack()
	{
		return _stack;
	}

	public double ReadMemory(int address)
	{
		return _stack[address];
	}

	public IEnumCollection GetInstructions()
	{
		return EnumCollections.SuitStorageInstructions;
	}

	public string GetInstructionDescription(int i)
	{
		if (EnumCollections.SuitStorageInstructions[i] == SuitStorageInstruction.None)
		{
			throw new NotImplementedException();
		}
		return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Slot_Index", typeof(byte)), new LogicStack.InstructionFormat("Prefab_Hash", typeof(uint)));
	}

	private void UpdateStack()
	{
		_stack.Clear();
		if (!OnOff || !Powered)
		{
			return;
		}
		int i = 0;
		Write(SuitStorageInstruction.SlotHelmet, HelmetSlot.SlotIndex, HelmetSlot.Get());
		Write(SuitStorageInstruction.SlotSuit, SuitSlot.SlotIndex, SuitSlot.Get());
		Write(SuitStorageInstruction.SlotBackpack, BackSlot.SlotIndex, BackSlot.Get());
		if (SpaceSuit != null)
		{
			Write(SuitStorageInstruction.SlotAirTank, SuitSlot.SlotIndex, SpaceSuit.AirTankSlot?.Get());
			Write(SuitStorageInstruction.SlotWasteTank, SuitSlot.SlotIndex, SpaceSuit.WasteTankSlot?.Get());
			if (SpaceSuit is SuitBase suitBase)
			{
				Write(SuitStorageInstruction.SlotCoolantTank, SuitSlot.SlotIndex, suitBase.CoolantTankSlot?.Get());
			}
		}
		if (Jetpack != null)
		{
			Write(SuitStorageInstruction.SlotPropellentTank, BackSlot.SlotIndex, Jetpack.PropellentSlot?.Get());
		}
		Write(SuitStorageInstruction.SlotBatteryCell, HelmetSlot.SlotIndex, HelmetSlot.Get<IBatteryPowered>()?.Battery);
		Write(SuitStorageInstruction.SlotBatteryCell, SuitSlot.SlotIndex, SuitSlot.Get<IBatteryPowered>()?.Battery);
		Write(SuitStorageInstruction.SlotBatteryCell, BackSlot.SlotIndex, BackSlot.Get<IBatteryPowered>()?.Battery);
		if (HelmetSlot.Contains<FilterMask>(out var occupant))
		{
			Write(SuitStorageInstruction.SlotFilter, HelmetSlot.SlotIndex, occupant.Filter1);
			Write(SuitStorageInstruction.SlotFilter, HelmetSlot.SlotIndex, occupant.Filter2);
		}
		if (!SuitSlot.Contains<ISuit>(out var occupant2))
		{
			return;
		}
		foreach (Slot filterSlot in occupant2.GetFilterSlots())
		{
			Write(SuitStorageInstruction.SlotFilter, SuitSlot.SlotIndex, filterSlot.Get<GasFilter>());
		}
		void Write(SuitStorageInstruction op, int coreSlot, Thing item)
		{
			if ((bool)item && i < _stack.Size)
			{
				_stack[i++] = LogicStack.PackByteInt32((byte)op, (byte)coreSlot, item.PrefabHash);
			}
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		UpdateStack();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		UpdateStack();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		UpdateStack();
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		UpdateStack();
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = new PassiveTooltip(true);
		passiveTooltip.Title = DisplayName;
		PassiveTooltip result = passiveTooltip;
		if (hitCollider == InputConnection.Collider)
		{
			result.Title = InterfaceStrings.ConnectionBreathing;
			return result;
		}
		if (hitCollider == OutputConnection.Collider)
		{
			result.Title = InterfaceStrings.ConnectionWaste;
			return result;
		}
		if (hitCollider == InputConnection2.Collider)
		{
			result.Title = InterfaceStrings.ConnectionPropellant;
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	protected override void CheckConnections()
	{
		INetworkedPipe iNetworkedPipe = InputConnection.GetINetworkedPipe();
		INetworkedPipe iNetworkedPipe2 = InputConnection2.GetINetworkedPipe();
		INetworkedPipe iNetworkedPipe3 = OutputConnection.GetINetworkedPipe();
		InputNetwork = iNetworkedPipe?.PipeNetwork;
		InputNetwork2 = iNetworkedPipe2?.PipeNetwork;
		OutputNetwork = iNetworkedPipe3?.PipeNetwork;
		AssessError();
	}

	public override bool CanLogicRead(LogicSlotType logicSlotType, int slotId)
	{
		if (!HasAnySlots || !base.IsStructureCompleted)
		{
			return false;
		}
		if (slotId < 0 || slotId >= Slots.Count)
		{
			return false;
		}
		switch (logicSlotType)
		{
		case LogicSlotType.PressureWaste:
		case LogicSlotType.PressureAir:
			return slotId == SuitSlot.SlotIndex;
		case LogicSlotType.Pressure:
		case LogicSlotType.Charge:
		case LogicSlotType.ChargeRatio:
			return true;
		default:
			return base.CanLogicRead(logicSlotType, slotId);
		}
	}

	public override double GetLogicValue(LogicSlotType logicSlotType, int slotId)
	{
		if (!HasAnySlots || !base.IsStructureCompleted)
		{
			return 0.0;
		}
		if (slotId < 0 || slotId >= Slots.Count)
		{
			return 0.0;
		}
		Slot slot = Slots[slotId];
		switch (logicSlotType)
		{
		case LogicSlotType.PressureAir:
		{
			if (slot.Contains<ISuit>(out var occupant3))
			{
				return occupant3.AirTankSlot.Get<GasCanister>()?.Pressure.ToDouble() ?? 0.0;
			}
			return 0.0;
		}
		case LogicSlotType.PressureWaste:
		{
			if (slot.Contains<ISuit>(out var occupant4))
			{
				return occupant4.WasteTankSlot.Get<GasCanister>()?.Pressure.ToDouble() ?? 0.0;
			}
			return 0.0;
		}
		case LogicSlotType.Pressure:
		{
			if (slot.Contains<Jetpack>(out var occupant5))
			{
				return occupant5.PropellentSlot.Get<GasCanister>()?.Pressure.ToDouble() ?? 0.0;
			}
			return 0.0;
		}
		case LogicSlotType.Charge:
		{
			if (slot.Contains<ISuit>(out var occupant2))
			{
				return occupant2.BatterySlot.Get<BatteryCell>()?.PowerStored ?? 0f;
			}
			return 0.0;
		}
		case LogicSlotType.ChargeRatio:
		{
			if (slot.Contains<ISuit>(out var occupant))
			{
				return occupant.BatterySlot.Get<BatteryCell>()?.PowerRatio ?? 0f;
			}
			return 0.0;
		}
		default:
			return base.GetLogicValue(logicSlotType, slotId);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (!OnOff || !Powered)
		{
			return;
		}
		if (base.IsInputValid && (bool)SpaceSuit?.AsThing && (bool)SpaceSuit.AirTank && !SpaceSuit.AsThing.IsEmergency)
		{
			AtmosphereHelper.Mix(InputNetwork.Atmosphere, SpaceSuit.AirTank.InternalAtmosphere, InputNetwork.Atmosphere.AllowedMatterState);
			AtmosphereHelper.DrainLiquids(SpaceSuit.AirTank.InternalAtmosphere, InputNetwork.Atmosphere, Chemistry.PipeVolume);
		}
		if (base.IsInput2Valid && (bool)Jetpack && Jetpack.PropellentSlot.Contains<GasCanister>(out var occupant) && !Jetpack.IsEmergency)
		{
			AtmosphereHelper.Mix(InputNetwork2.Atmosphere, occupant.InternalAtmosphere, InputNetwork2.Atmosphere.AllowedMatterState);
			AtmosphereHelper.DrainLiquids(occupant.InternalAtmosphere, InputNetwork2.Atmosphere, Chemistry.PipeVolume);
		}
		if (!base.IsOutputValid)
		{
			return;
		}
		if ((bool)SpaceSuit?.AsThing && !SpaceSuit.AsThing.IsEmergency)
		{
			OutputNetwork.Atmosphere.Add(SpaceSuit.InternalAtmosphere.GasMixture);
			SpaceSuit.InternalAtmosphere.GasMixture.Reset();
			if ((bool)SpaceSuit?.WasteTank)
			{
				OutputNetwork.Atmosphere.Add(SpaceSuit.WasteTank.InternalAtmosphere.GasMixture);
				SpaceSuit.WasteTank.InternalAtmosphere.GasMixture.Reset();
			}
		}
		if ((bool)Helmet && !Helmet.IsEmergency)
		{
			OutputNetwork.Atmosphere.Add(Helmet.InternalAtmosphere.GasMixture);
			Helmet.InternalAtmosphere.GasMixture.Reset();
		}
	}

	private static float Recharge(IBatteryPowered poweredItem, float powerUsed)
	{
		if (poweredItem != null && (bool)poweredItem.Battery && !poweredItem.Battery.IsCharged)
		{
			return poweredItem.Battery.AddPowerSafe(powerUsed);
		}
		return powerUsed;
	}

	private static float GetPowerDelta(IBatteryPowered poweredItem)
	{
		if (poweredItem != null && (bool)poweredItem.Battery && !poweredItem.Battery.IsCharged)
		{
			return poweredItem.Battery.PowerDelta;
		}
		return 0f;
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerUsed)
	{
		if (!(powerUsed <= UsedPower))
		{
			powerUsed -= UsedPower;
			powerUsed = Recharge(SuitSlot.Get<IBatteryPowered>(), powerUsed);
			powerUsed = Recharge(HelmetSlot.Get<IBatteryPowered>(), powerUsed);
			powerUsed = Recharge(BackSlot.Get<IBatteryPowered>(), powerUsed);
		}
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (cableNetwork != base.PowerCableNetwork || base.PowerCableNetwork == null)
		{
			return base.GetUsedPower(cableNetwork);
		}
		if (OnOff)
		{
			float b = GetPowerDelta(SuitSlot.Get<IBatteryPowered>()) + GetPowerDelta(HelmetSlot.Get<IBatteryPowered>()) + GetPowerDelta(BackSlot.Get<IBatteryPowered>());
			return UsedPower + Mathf.Min(1000f, b);
		}
		if (!OnOff)
		{
			return 0f;
		}
		return UsedPower;
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		Achievements.AssessTakeALoadOff(this);
	}
}
