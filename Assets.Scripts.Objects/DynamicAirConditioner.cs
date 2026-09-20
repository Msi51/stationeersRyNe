using System;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Objects;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class DynamicAirConditioner : PortableAtmosphericsPowered, IThermal
{
	public static string[] AirconditioningModeStrings = Enum.GetNames(typeof(AirConditioningMode));

	[SerializeField]
	private Transform fanTransform;

	[SerializeField]
	private SwitchMode switchMode;

	[SerializeField]
	private float fanRpm = 120f;

	private float _currentRpm;

	private static readonly TemperatureKelvin MAXTempDelta = new TemperatureKelvin(100.0);

	private bool _coolantFrozen;

	private EnergyMode _energyMode;

	private short _heatingEnergyLastTick;

	private const float HEAT_PUMP_EFFICIENCY = 100f;

	private const float HEATER_EFFICIENCY = 10f;

	public override string[] ModeStrings => AirconditioningModeStrings;

	private Slot LiquidCanisterSlot
	{
		get
		{
			if (Slots.Count <= 1)
			{
				return null;
			}
			return Slots[1];
		}
	}

	private GasCanister LiquidCanister => LiquidCanisterSlot?.Get<GasCanister>();

	private Atmosphere CoolantAtmosphere
	{
		get
		{
			if (!(LiquidCanister != null) || LiquidCanister.InternalAtmosphere == null)
			{
				return null;
			}
			return LiquidCanister.InternalAtmosphere;
		}
	}

	public static TemperatureKelvin MAXHeatingTemp => new TemperatureKelvin(323.15);

	public static TemperatureKelvin MINCoolingTemp => new TemperatureKelvin(263.15);

	private static PressurekPa MaxCoolantPressure => Chemistry.OneAtmosphere * 40.0;

	public bool CoolantFrozen
	{
		get
		{
			return _coolantFrozen;
		}
		set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients() && value != CoolantFrozen)
			{
				base.NetworkUpdateFlags |= 512;
			}
			_coolantFrozen = value;
		}
	}

	public EnergyMode EnergyMode
	{
		get
		{
			return _energyMode;
		}
		set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients() && value != EnergyMode)
			{
				base.NetworkUpdateFlags |= 512;
			}
			_energyMode = value;
		}
	}

	public short HeatingEnergyLastTick
	{
		get
		{
			return _heatingEnergyLastTick;
		}
		set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients() && !Mathf.Approximately(value, HeatingEnergyLastTick))
			{
				base.NetworkUpdateFlags |= 512;
			}
			_heatingEnergyLastTick = value;
		}
	}

	public override void InitInternalAtmosphere()
	{
		base.InitInternalAtmosphere();
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		bool flag = OnOff && Powered && Error == 0;
		if (!GameManager.IsBatchMode && !IsOccluded)
		{
			_currentRpm = Mathf.Lerp(_currentRpm, flag ? fanRpm : 0f, GameManager.DeltaTime);
			_currentRpm = Mathf.Clamp(_currentRpm, 0f, fanRpm);
			fanTransform.Rotate(Vector3.up, 360f * GameManager.DeltaTime * _currentRpm / 60f);
		}
	}

	public override void OnThreadUpdate()
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		_pressureRating = PressurekPa.Zero.ToFloat();
		if (base.InternalAtmosphere != null)
		{
			_pressureRating = base.InternalAtmosphere.PressureGassesAndLiquids.ToFloat() / MaxCoolantPressure.ToFloat();
			if (float.IsNaN(_pressureRating))
			{
				_pressureRating = PressurekPa.Zero.ToFloat();
			}
		}
		_needleRotation = (float)((!FlipNeedleDirection) ? 1 : (-1)) * Mathf.Lerp(NeedleMinimum, NeedleMaximum, _pressureRating);
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (switchMode != null)
		{
			switchMode.RefreshState(skipAnimation);
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (LiquidCanister != null && hitCollider == LiquidCanisterSlot.Collider)
		{
			PassiveTooltip passiveTooltip = new PassiveTooltip(true);
			passiveTooltip.Title = DisplayName;
			PassiveTooltip result = passiveTooltip;
			StringBuilder stringBuilder = new StringBuilder();
			AtmosphericsManager.DisplayBasicAtmosphere(LiquidCanister.InternalAtmosphere, stringBuilder);
			result.Extended = stringBuilder.ToString();
			return result;
		}
		if (hitCollider == InfoPanel)
		{
			PassiveTooltip passiveTooltip = new PassiveTooltip(true);
			passiveTooltip.Title = DisplayName;
			PassiveTooltip result2 = passiveTooltip;
			StringBuilder stringBuilder2 = new StringBuilder();
			AtmosphericsManager.DisplayBasicAtmosphere(base.InternalAtmosphere, stringBuilder2);
			if (CoolantFrozen && LiquidCanister != null)
			{
				stringBuilder2.AppendLine(GameStrings.CoolantFrozen.AsString(LiquidCanister.ToTooltip()));
			}
			switch (EnergyMode)
			{
			case EnergyMode.HeatingFromCanister:
				if (LiquidCanister != null)
				{
					stringBuilder2.AppendLine(GameStrings.HeatingFromInternal.AsString(ToTooltip(), LiquidCanister.ToTooltip(), ExtensionMethods.ToStringPrefix(HeatingEnergyLastTick, "W", "yellow", true)));
				}
				break;
			case EnergyMode.HeatingFromElement:
				stringBuilder2.AppendLine(GameStrings.HeatingFromElement.AsString(ToTooltip(), ExtensionMethods.ToStringPrefix(HeatingEnergyLastTick, "W", "yellow", true)));
				break;
			case EnergyMode.CoolingFromCanister:
				if (LiquidCanister != null)
				{
					stringBuilder2.AppendLine(GameStrings.CoolingFromInternal.AsString(ToTooltip(), LiquidCanister.ToTooltip(), ExtensionMethods.ToStringPrefix(Mathf.Abs(HeatingEnergyLastTick), "W", "yellow", true)));
				}
				break;
			}
			if (!IsWithinOperatingRange(out var errorGameString))
			{
				stringBuilder2.AppendLine(errorGameString.AsString(ToTooltip()));
			}
			result2.Extended = stringBuilder2.ToString();
			return result2;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	private bool IsWithinOperatingRange(out Assets.Scripts.Localization2.GameString errorGameString)
	{
		errorGameString = null;
		switch ((AirConditioningMode)Mode)
		{
		case AirConditioningMode.Cold:
			if (base.WorldAtmosphere != null && base.WorldAtmosphere.Temperature < MINCoolingTemp)
			{
				errorGameString = GameStrings.TemperatureTooColdForThing;
				return false;
			}
			break;
		case AirConditioningMode.Hot:
			if (base.WorldAtmosphere != null && base.WorldAtmosphere.Temperature > MAXHeatingTemp)
			{
				errorGameString = GameStrings.TemperatureTooHotForThing;
				return false;
			}
			break;
		}
		return true;
	}

	public override bool IsOperable()
	{
		Assets.Scripts.Localization2.GameString errorGameString;
		bool flag = IsWithinOperatingRange(out errorGameString) && base.GridController.CanContainAtmos(base.WorldGrid);
		if (base.InternalAtmosphere == null || base.InternalAtmosphere.PressureGasses > MaxCoolantPressure)
		{
			flag = false;
		}
		if (Mode == 0 && (LiquidCanister == null || LiquidCanister.InternalAtmosphere == null || (LiquidCanister.InternalAtmosphere.PressureGasses < Chemistry.ArmstrongLimit && LiquidCanister.InternalAtmosphere.TotalMolesLiquids.Equals(MoleQuantity.Zero))))
		{
			flag = false;
		}
		if (!GameManager.RunSimulation)
		{
			return flag;
		}
		if (!flag && Error == 0 && Powered)
		{
			OnServer.Interact(base.InteractError, 1);
		}
		if (flag && Error == 1)
		{
			OnServer.Interact(base.InteractError, 0);
		}
		return flag;
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.AirContitioningAtmos);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		if (interactable.Action == InteractableType.Mode)
		{
			if (!doAction)
			{
				return DelayedActionInstance.Success(interactable.ContextualName);
			}
			OnServer.Interact(interactable, (Mode == 0) ? 1 : 0);
			return DelayedActionInstance.Success(interactable.ContextualName);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		short heatingEnergyLastTick = 0;
		EnergyMode = GetEnergyMode();
		if (!OnOff || !BatteryCell || BatteryCell.IsEmpty)
		{
			if (Powered)
			{
				OnServer.Interact(base.InteractPowered, 0);
			}
			HeatingEnergyLastTick = heatingEnergyLastTick;
			return;
		}
		if (!Powered)
		{
			OnServer.Interact(base.InteractPowered, 1);
		}
		if (IsOpen)
		{
			VentWaste();
		}
		if (CoolantAtmosphere != null)
		{
			if (CoolantAtmosphere.PressureGasses > MaxCoolantPressure)
			{
				MoleQuantity transferMoles = IdealGas.Quantity(CoolantAtmosphere.PressureGasses - MaxCoolantPressure, LiquidCanister.InternalAtmosphere.GetGasVolume(), LiquidCanister.InternalAtmosphere.Temperature);
				base.InternalAtmosphere.Add(LiquidCanister.InternalAtmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.Gas));
			}
			CoolantFrozen = LiquidCanister.InternalAtmosphere.GasMixture.CheckForFreezing(Chemistry.ArmstrongLimit).GetTotalMolesGassesAndLiquids > MoleQuantity.Zero;
		}
		else
		{
			CoolantFrozen = false;
		}
		if (!IsOperable())
		{
			HeatingEnergyLastTick = heatingEnergyLastTick;
			return;
		}
		if (base.WorldAtmosphere == null || !base.WorldAtmosphere.IsAboveArmstrong())
		{
			HeatingEnergyLastTick = heatingEnergyLastTick;
			return;
		}
		if (base.WorldAtmosphere.IsGlobalAtmosphere)
		{
			base.WorldAtmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
		}
		float num = 1f;
		MoleEnergy zero = MoleEnergy.Zero;
		switch (EnergyMode)
		{
		case EnergyMode.HeatingFromCanister:
			if (Mode == 1)
			{
				if (base.WorldAtmosphere.Temperature > CoolantAtmosphere.Temperature)
				{
					num = ((MAXTempDelta - (base.WorldAtmosphere.Temperature - CoolantAtmosphere.Temperature)) / MAXTempDelta).ToFloat();
					num = Mathf.Clamp(num, 0.01f, 1f);
				}
				zero = new MoleEnergy(UsedPower * 100f * num);
				base.WorldAtmosphere.GasMixture.AddEnergy(CoolantAtmosphere.GasMixture.RemoveEnergy(zero));
				heatingEnergyLastTick = (short)zero.ToFloat();
			}
			break;
		case EnergyMode.HeatingFromElement:
			if (Mode == 1)
			{
				zero = new MoleEnergy(UsedPower * 10f);
				base.WorldAtmosphere.GasMixture.AddEnergy(zero);
				heatingEnergyLastTick = (short)zero.ToFloat();
			}
			break;
		case EnergyMode.CoolingFromCanister:
			if (Mode == 0)
			{
				if (base.WorldAtmosphere.Temperature < CoolantAtmosphere.Temperature)
				{
					num = ((MAXTempDelta - (CoolantAtmosphere.Temperature - base.WorldAtmosphere.Temperature)) / MAXTempDelta).ToFloat();
					num = Mathf.Clamp(num, 0.01f, 1f);
				}
				zero = new MoleEnergy(UsedPower * 100f * num);
				heatingEnergyLastTick = (short)(0f - zero.ToFloat());
				CoolantAtmosphere.GasMixture.AddEnergy(base.WorldAtmosphere.GasMixture.RemoveEnergy(zero));
				BatteryCell.PowerStored -= UsedPower;
			}
			break;
		}
		HeatingEnergyLastTick = heatingEnergyLastTick;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteBoolean(CoolantFrozen);
			writer.WriteInt16(HeatingEnergyLastTick);
			writer.WriteByte((byte)EnergyMode);
		}
		base.BuildUpdate(writer, networkUpdateType);
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			CoolantFrozen = reader.ReadBoolean();
			HeatingEnergyLastTick = reader.ReadInt16();
			EnergyMode = (EnergyMode)reader.ReadByte();
		}
		base.ProcessUpdate(reader, networkUpdateType);
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteBoolean(CoolantFrozen);
		writer.WriteInt16(HeatingEnergyLastTick);
		writer.WriteByte((byte)EnergyMode);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		CoolantFrozen = reader.ReadBoolean();
		HeatingEnergyLastTick = reader.ReadInt16();
		EnergyMode = (EnergyMode)reader.ReadByte();
	}

	public void VentWaste()
	{
		base.WorldAtmosphere.Add(base.InternalAtmosphere.Remove(base.InternalAtmosphere.TotalMoles, AtmosphereHelper.MatterState.All));
	}

	private EnergyMode GetEnergyMode()
	{
		if (!IsOperable())
		{
			return EnergyMode.None;
		}
		if (Mode == 1 && !CoolantFrozen && LiquidCanister != null && LiquidCanister.GetTotalMoles() > Chemistry.MINIMUM_QUANTITY_MOLES)
		{
			return EnergyMode.HeatingFromCanister;
		}
		if (Mode == 1 && (CoolantFrozen || LiquidCanister == null || LiquidCanister.GetTotalMoles() < Chemistry.MINIMUM_QUANTITY_MOLES))
		{
			return EnergyMode.HeatingFromElement;
		}
		if (Mode == 0 && LiquidCanister != null && LiquidCanister.InternalAtmosphere != null)
		{
			return EnergyMode.CoolingFromCanister;
		}
		return EnergyMode.None;
	}
}
