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

public class PoweredVent : SmallDeviceOutput, IPoweredVent, IReferencable, IEvaluable, IAirlockDevice, IFlowIndicator
{
	[Header("Vent Configuration")]
	[SerializeField]
	private SwitchMode switchMode;

	[SerializeField]
	protected GameBase fan;

	[SerializeField]
	private FlowIndicator flowIndicator;

	private FlowIndicatorState _flowIndicatorStatus;

	[SerializeField]
	private float DegreesRotationPerSecond = 360f;

	private PressurekPa _externalPressure = Chemistry.OneAtmosphere;

	private static readonly PressurekPa DefaultMaxPressure = new PressurekPa(50662.5);

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

	public VentDirection VentDirection => (VentDirection)Mode;

	public PressurekPa ExternalPressure
	{
		get
		{
			return _externalPressure;
		}
		set
		{
			value = RocketMath.Clamp(value, Chemistry.Pressure.Minimum, Chemistry.Pressure.Maximum);
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
			return Chemistry.Pressure.Maximum;
		}
		set
		{
		}
	}

	public override string[] ModeStrings => ActiveVent.VentDirectionStrings;

	public override void Awake()
	{
		base.Awake();
		FlowIndicator.Init(this);
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (!IsOperable || !base.IsStructureCompleted)
		{
			FlowIndicatorStatus = FlowIndicatorState.None;
		}
		else if (!OnOff || !Powered)
		{
			FlowIndicatorStatus = FlowIndicatorState.None;
		}
		else
		{
			ExchangeWithWorld();
		}
	}

	protected virtual void ExchangeWithWorld()
	{
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if ((object)fan != null && !IsOccluded && base.IsStructureCompleted && OnOff && Powered)
		{
			fan.Transform.Rotate(Vector3.forward, ((Mode == 1) ? (0f - DegreesRotationPerSecond) : DegreesRotationPerSecond) * Time.deltaTime);
		}
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
				InternalPressure = DefaultMaxPressure;
			}
		}
	}

	protected PressurekPa PumpGasToWorld(Atmosphere worldAtmosphere, Atmosphere targetAtmosphere, TemperatureKelvin totalTemperature, PressurekPa pressureToMove, bool force = false)
	{
		if (worldAtmosphere == null || targetAtmosphere == null)
		{
			return PressurekPa.Zero;
		}
		if (!force)
		{
			pressureToMove = RocketMath.Min(pressureToMove, ExternalPressure - worldAtmosphere.PressureGassesAndLiquids);
		}
		MoleQuantity transferMoles = IdealGas.Quantity(pressureToMove, worldAtmosphere.Volume, totalTemperature);
		PressurekPa pressureGasses = worldAtmosphere.PressureGasses;
		worldAtmosphere.Add(targetAtmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.All));
		return worldAtmosphere.PressureGasses - pressureGasses;
	}

	protected PressurekPa PumpGasToPipe(Atmosphere worldAtmosphere, Atmosphere targetAtmosphere, PressurekPa pressureToMove, bool force = false)
	{
		if (worldAtmosphere == null || targetAtmosphere == null)
		{
			return PressurekPa.Zero;
		}
		if (!force)
		{
			pressureToMove = RocketMath.Min(pressureToMove, worldAtmosphere.PressureGasses - ExternalPressure);
		}
		MoleQuantity transferMoles = IdealGas.Quantity(pressureToMove, worldAtmosphere.Volume, worldAtmosphere.Temperature);
		targetAtmosphere.Add(worldAtmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.Gas));
		return pressureToMove;
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

	public override bool CanLogicRead(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Setting:
		case LogicType.Maximum:
		case LogicType.Ratio:
			return false;
		case LogicType.PressureExternal:
			return true;
		default:
			return base.CanLogicRead(logicType);
		}
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Setting:
		case LogicType.Maximum:
		case LogicType.Ratio:
			return false;
		case LogicType.PressureExternal:
			return true;
		default:
			return base.CanLogicWrite(logicType);
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.PressureExternal)
		{
			return ExternalPressure.ToDouble();
		}
		return base.GetLogicValue(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		if (logicType == LogicType.PressureExternal)
		{
			ExternalPressure = new PressurekPa(value);
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

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (!base.GridController.CanContainAtmos(base.WorldGrid))
		{
			extendedText.AppendLine(GameStrings.DeviceWorldGridBlocked.AsString(ToTooltip()));
		}
		extendedText.AppendLine(GameStrings.PressureSafteySetTo.AsString(GameStrings.AtmosphereExternal.DisplayString, GetExternalPressurePa().ToStringPrefix("Pa", "yellow")));
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
			extendedText.AppendLine(GameStrings.PressureCloseToTarget.DisplayString);
		}
		return extendedText;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new PoweredVentSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is PoweredVentSaveData poweredVentSaveData)
		{
			ExternalPressure = new PressurekPa(poweredVentSaveData.ExternalPressure);
			InternalPressure = new PressurekPa(poweredVentSaveData.InternalPressure);
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is PoweredVentSaveData poweredVentSaveData)
		{
			poweredVentSaveData.ExternalPressure = ExternalPressure.ToFloat();
			poweredVentSaveData.InternalPressure = InternalPressure.ToFloat();
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(ExternalPressure.ToFloat());
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
		writer.WriteByte((byte)FlowIndicatorStatus);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		ExternalPressure = new PressurekPa(reader.ReadSingle());
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
		if (interactable.Action == InteractableType.Mode)
		{
			ResetVent();
		}
		if (GameManager.GameState == GameState.Running)
		{
			base.DataCableNetwork?.RequestDeviceRefresh(this);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		base.DataCableNetwork?.RequestDeviceRefresh(this);
		ResetVent();
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		ResetVent();
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if ((bool)switchMode)
		{
			switchMode.RefreshState(skipAnimation);
		}
	}
}
