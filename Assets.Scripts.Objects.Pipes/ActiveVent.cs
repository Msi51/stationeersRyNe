using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Objects;
using Objects.Electrical;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class ActiveVent : SmallDeviceOutput, IAirlockDevice, IPoweredVent, IReferencable, IEvaluable, IFlowIndicator
{
	[Header("Vent Configuration")]
	private PressurekPa _externalPressure = Chemistry.OneAtmosphere;

	private PressurekPa _internalPressure = Chemistry.OneAtmosphere * 500.0;

	[SerializeField]
	private SwitchMode switchMode;

	[SerializeField]
	private FlowIndicator flowIndicator;

	private FlowIndicatorState _flowIndicatorStatus;

	public Vector3 LocalRotationDataDisk;

	public static string[] VentDirectionStrings = LocalizedEnumCollections.VentDirection.Names;

	public PressurekPa ExternalPressure
	{
		get
		{
			return _externalPressure;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(_externalPressure, value))
			{
				base.NetworkUpdateFlags |= 256;
			}
			_externalPressure = value;
		}
	}

	public PressurekPa InternalPressure
	{
		get
		{
			return _internalPressure;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(_internalPressure, value))
			{
				base.NetworkUpdateFlags |= 256;
			}
			_internalPressure = value;
		}
	}

	public VentDirection VentDirection => (VentDirection)Mode;

	public FlowIndicator FlowIndicator
	{
		get
		{
			return flowIndicator;
		}
		set
		{
			flowIndicator = value;
		}
	}

	public FlowIndicatorState FlowIndicatorStatus
	{
		get
		{
			return _flowIndicatorStatus;
		}
		set
		{
			if (value != FlowIndicatorStatus && NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
			_flowIndicatorStatus = value;
			if (FlowIndicator != null)
			{
				FlowIndicator.RefreshState();
			}
		}
	}

	public override string[] ModeStrings => VentDirectionStrings;

	public override void Awake()
	{
		base.Awake();
		FlowIndicator.Init(this);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType - 7 <= LogicType.Power)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType - 7 <= LogicType.Power)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.PressureExternal => ExternalPressure.ToDouble(), 
			LogicType.PressureInternal => InternalPressure.ToDouble(), 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		switch (logicType)
		{
		case LogicType.PressureExternal:
			ExternalPressure = new PressurekPa(value);
			break;
		case LogicType.PressureInternal:
			InternalPressure = new PressurekPa(value);
			break;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (hitCollider != null)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		PassiveTooltip result = new PassiveTooltip(true);
		result.Title = DisplayName;
		result.Extended = (Settings.CurrentData.ExtendedTooltips ? GetExtendedText().ToString() : string.Empty);
		return result;
	}

	public float GetExternalPressurePa()
	{
		return _externalPressure.ToFloat() * 1000f;
	}

	public float GetInternalPressurePa()
	{
		return _internalPressure.ToFloat() * 1000f;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (!base.GridController.CanContainAtmos(base.WorldGrid))
		{
			extendedText.AppendLine(GameStrings.DeviceWorldGridBlocked.AsString(ToTooltip()));
		}
		if (Cell.IsInCrewModule(base.WorldGrid, out var crewModule))
		{
			extendedText.AppendLine(GameStrings.DeviceOutputCrewModule.AsString(crewModule.ToTooltip()));
		}
		extendedText.AppendLine(GameStrings.PressureSafteySetTo.AsString(GameStrings.AtmosphereExternal.DisplayString, GetExternalPressurePa().ToStringPrefix("Pa", "yellow")));
		extendedText.AppendLine(GameStrings.PressureSafteySetTo.AsString(GameStrings.AtmosphereInternal.DisplayString, GetInternalPressurePa().ToStringPrefix("Pa", "yellow")));
		string arg = FlowIndicatorStatus switch
		{
			FlowIndicatorState.None => Defines.Color.White, 
			FlowIndicatorState.Max => Defines.Color.Green, 
			FlowIndicatorState.InwardsLimited => Defines.Color.Orange, 
			FlowIndicatorState.InwardsVeryLimited => Defines.Color.Red, 
			FlowIndicatorState.OutwardsLimited => Defines.Color.Orange, 
			FlowIndicatorState.OutwardsVeryLimited => Defines.Color.Red, 
			FlowIndicatorState.Idle => Defines.Color.Blue, 
			_ => string.Empty, 
		};
		extendedText.AppendLine(GameStrings.FlowRateStatus.AsString(arg, FlowIndicatorStatus.GetName()));
		FlowIndicatorState flowIndicatorStatus = FlowIndicatorStatus;
		if (flowIndicatorStatus == FlowIndicatorState.OutwardsLimited || flowIndicatorStatus == FlowIndicatorState.OutwardsVeryLimited)
		{
			Atmosphere worldAtmosphere = GetWorldAtmosphere();
			if (worldAtmosphere != null && worldAtmosphere.PressureGasses > ExternalPressure - PressurekPa.One)
			{
				extendedText.AppendLine(GameStrings.PressureCloseToTarget.DisplayString);
			}
			else
			{
				extendedText.AppendLine(GameStrings.IncreasePipeVolumePressure.DisplayString);
			}
		}
		flowIndicatorStatus = FlowIndicatorStatus;
		if (flowIndicatorStatus == FlowIndicatorState.InwardsLimited || flowIndicatorStatus == FlowIndicatorState.InwardsVeryLimited)
		{
			if (ConnectedPipeNetwork.Atmosphere.PressureGasses > InternalPressure * 0.949999988079071)
			{
				extendedText.AppendLine(GameStrings.PressureCloseToInternalTarget.DisplayString);
			}
			else
			{
				extendedText.AppendLine(GameStrings.PressureCloseToTarget.DisplayString);
			}
		}
		return extendedText;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new ActiveVentSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		Quaternion localRotation = new Quaternion
		{
			eulerAngles = LocalRotationDataDisk
		};
		if (newChild.ParentSlot.Type == Slot.Class.DataDisk)
		{
			newChild.transform.localRotation = localRotation;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is ActiveVentSaveData activeVentSaveData)
		{
			ExternalPressure = new PressurekPa(activeVentSaveData.ExternalPressure);
			InternalPressure = new PressurekPa(activeVentSaveData.InternalPressure);
		}
		else
		{
			ResetVent();
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is ActiveVentSaveData activeVentSaveData)
		{
			activeVentSaveData.ExternalPressure = ExternalPressure.ToFloat();
			activeVentSaveData.InternalPressure = InternalPressure.ToFloat();
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(ExternalPressure.ToFloat());
			writer.WriteSingle(InternalPressure.ToFloat());
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteByte((byte)FlowIndicatorStatus);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			ExternalPressure = new PressurekPa(reader.ReadSingle());
			InternalPressure = new PressurekPa(reader.ReadSingle());
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			FlowIndicatorStatus = (FlowIndicatorState)reader.ReadByte();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(ExternalPressure.ToFloat());
		writer.WriteSingle(InternalPressure.ToFloat());
		writer.WriteByte((byte)FlowIndicatorStatus);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		ExternalPressure = new PressurekPa(reader.ReadSingle());
		InternalPressure = new PressurekPa(reader.ReadSingle());
		FlowIndicatorStatus = (FlowIndicatorState)reader.ReadByte();
	}

	public override CanConstructInfo CanConstruct()
	{
		CanMountResult canMountResult = CanMountOnWall();
		if (canMountResult.result == WallMountResult.Valid)
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(canMountResult.ResultMessage());
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Mode)
		{
			ResetVent();
		}
		if (GameManager.GameState == GameState.Running && base.DataCableNetwork != null)
		{
			base.DataCableNetwork.RequestDeviceRefresh(this);
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		if ((bool)switchMode)
		{
			switchMode.RefreshState(skipAnimation);
		}
		if ((bool)base.SwitchOnOff)
		{
			base.SwitchOnOff.RefreshState(skipAnimation);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (base.DataCableNetwork != null)
		{
			base.DataCableNetwork.RequestDeviceRefresh(this);
		}
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		ResetVent();
	}

	public void ResetVent()
	{
		if (GameManager.RunSimulation)
		{
			if (Mode == 0)
			{
				ExternalPressure = Chemistry.OneAtmosphere;
				InternalPressure = PressurekPa.Zero;
			}
			else
			{
				ExternalPressure = PressurekPa.Zero;
				InternalPressure = new PressurekPa(50662.5);
			}
		}
	}

	public virtual Atmosphere GetWorkingAtmosphere()
	{
		return base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (OnOff && Powered && IsOperable)
		{
			Atmosphere workingAtmosphere = GetWorkingAtmosphere();
			Atmosphere atmosphere = ConnectedPipeNetwork.Atmosphere;
			TemperatureKelvin totalTemperature = new TemperatureKelvin(atmosphere.Temperature.ToDouble() * atmosphere.TotalMoles.ToDouble() + workingAtmosphere.Temperature.ToDouble() * workingAtmosphere.TotalMoles.ToDouble()) / (workingAtmosphere.TotalMoles.ToDouble() + atmosphere.TotalMoles.ToDouble());
			FlowIndicatorState flowIndicatorStatus = FlowIndicatorState.Max;
			switch (VentDirection)
			{
			case VentDirection.Inward:
			{
				PressurekPa pressurekPa = base.PressurePerTick - PumpGasToPipe(workingAtmosphere, atmosphere, totalTemperature);
				if (pressurekPa > PressurekPa.One)
				{
					flowIndicatorStatus = ((pressurekPa < base.PressurePerTick / 2.0) ? FlowIndicatorState.InwardsLimited : FlowIndicatorState.InwardsVeryLimited);
				}
				break;
			}
			case VentDirection.Outward:
			{
				PressurekPa pressurekPa = base.PressurePerTick - PumpGasToWorld(workingAtmosphere, atmosphere, totalTemperature);
				if (pressurekPa > PressurekPa.One)
				{
					flowIndicatorStatus = ((pressurekPa < base.PressurePerTick / 2.0) ? FlowIndicatorState.OutwardsLimited : FlowIndicatorState.OutwardsVeryLimited);
				}
				break;
			}
			}
			FlowIndicatorStatus = flowIndicatorStatus;
		}
		else
		{
			FlowIndicatorStatus = FlowIndicatorState.None;
		}
	}

	private PressurekPa PumpGasToWorld(Atmosphere worldAtmosphere, Atmosphere pipeAtmosphere, TemperatureKelvin totalTemperature)
	{
		MoleQuantity val = IdealGas.Quantity(pipeAtmosphere.PressureGassesAndLiquids - InternalPressure, pipeAtmosphere.Volume, pipeAtmosphere.Temperature);
		MoleQuantity val2 = IdealGas.Quantity(RocketMath.Clamp(RocketMath.Min(base.PressurePerTick, ExternalPressure - worldAtmosphere.PressureGassesAndLiquids), PressurekPa.Zero, base.PressurePerTick), worldAtmosphere.Volume, totalTemperature);
		val2 = RocketMath.Min(val2, val);
		PressurekPa pressureGasses = worldAtmosphere.PressureGasses;
		worldAtmosphere.Add(pipeAtmosphere.Remove(val2, AtmosphereHelper.MatterState.All));
		return worldAtmosphere.PressureGasses - pressureGasses;
	}

	private PressurekPa PumpGasToPipe(Atmosphere worldAtmosphere, Atmosphere pipeAtmosphere, TemperatureKelvin totalTemperature)
	{
		PressurekPa val = worldAtmosphere.PressureGasses - ExternalPressure;
		MoleQuantity val2 = IdealGas.Quantity(RocketMath.Min(base.PressurePerTick, val), worldAtmosphere.Volume, totalTemperature);
		MoleQuantity val3 = IdealGas.Quantity(InternalPressure - pipeAtmosphere.PressureGasses, pipeAtmosphere.Volume, totalTemperature);
		MoleQuantity transferMoles = RocketMath.Min(val2, val3);
		PressurekPa pressureGasses = worldAtmosphere.PressureGasses;
		pipeAtmosphere.Add(worldAtmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.Gas));
		return pressureGasses - worldAtmosphere.PressureGasses;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action != InteractableType.Mode)
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
		if (IsLocked)
		{
			return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceLocked);
		}
		if (!doAction)
		{
			return DelayedActionInstance.Success(interactable.ContextualName);
		}
		OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
		return DelayedActionInstance.Success(interactable.ContextualName);
	}
}
