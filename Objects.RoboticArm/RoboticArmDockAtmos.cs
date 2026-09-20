using System;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.RoboticArm;

public class RoboticArmDockAtmos : RoboticArmDock, IThermal, IVolume
{
	[SerializeField]
	private NeedleRotator needle1 = new NeedleRotator
	{
		minDegrees = 0f,
		maxDegrees = 280f,
		lerpSpeed = 2f
	};

	[SerializeField]
	private NeedleRotator needle2 = new NeedleRotator
	{
		minDegrees = 0f,
		maxDegrees = 280f,
		lerpSpeed = 2f
	};

	private const float LITRES = 500f;

	public static readonly PressurekPa MAXPressure = Chemistry.OneAtmosphere * 600.0;

	public Vector3 ArmForward;

	[SerializeField]
	private SwitchLarreMode switchMode;

	private static readonly float MinimumRatioToFilterAll = 0.001f;

	public static readonly string[] VentDirectionStrings = EnumCollections.VentDirection.Names;

	private PressurekPa _externalPressure = Chemistry.OneAtmosphere;

	private PressurekPa _internalPressure = Chemistry.OneAtmosphere * 500.0;

	private (bool playing, VentDirection direction) _audioState = (playing: false, direction: VentDirection.Outward);

	public PressurekPa PressurePerTick = Chemistry.OneAtmosphere;

	[SerializeField]
	private Transform ventTransform;

	private PassiveVent _dockedVent;

	[SerializeField]
	private Collider Gauge1Collider;

	[SerializeField]
	private Collider Gauge2Collider;

	private int _damageTicker;

	private Vector3 _armVentPosition;

	public override bool HasReadableAtmosphere => true;

	public VolumeLitres GetVolume => new VolumeLitres(500.0);

	private Slot FilterSlot => Slots[0];

	public override string[] ModeStrings => VentDirectionStrings;

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

	public Vector3 ArmVentPosition
	{
		get
		{
			return _armVentPosition + ArmForward * 0.3f;
		}
		private set
		{
			_armVentPosition = value;
		}
	}

	public PassiveVent DockedVent
	{
		get
		{
			return _dockedVent;
		}
		private set
		{
			if (_dockedVent != null && _dockedVent.DockedAtmosArm == this && value != _dockedVent)
			{
				_dockedVent.DockedAtmosArm = null;
			}
			_dockedVent = value;
			if (_dockedVent != null)
			{
				_dockedVent.DockedAtmosArm = this;
			}
		}
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			Atmosphere atmosphere = (base.InternalAtmosphere = new Atmosphere(this, new VolumeLitres(500.0), 0L));
		}
	}

	private void HandleOperatingAudio(bool force = false)
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		bool flag = base.ArmState == ArmState.Down && IsOperable;
		if (flag && (force || !_audioState.playing || VentDirection != _audioState.direction))
		{
			switch (VentDirection)
			{
			case VentDirection.Inward:
				GetAudioEvent(Defines.Sounds.VentInwards).Trigger();
				GetAudioEvent(Defines.Sounds.VentOutwards).Stop(immediate: true);
				break;
			case VentDirection.Outward:
				GetAudioEvent(Defines.Sounds.VentOutwards).Trigger();
				GetAudioEvent(Defines.Sounds.VentInwards).Stop(immediate: true);
				break;
			}
		}
		else if (!flag && (force || _audioState.playing))
		{
			GetAudioEvent(Defines.Sounds.VentInwards).Stop();
			GetAudioEvent(Defines.Sounds.VentOutwards).Stop();
		}
		_audioState = (playing: flag, direction: VentDirection);
	}

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None)
		{
			AtmosphericsManager.Instance.Register(this);
		}
		ArmVentPosition = ventTransform.position;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (GameManager.GameState != GameState.None)
		{
			VentArmAtmosphereToWorld();
			AtmosphericsManager.Instance.Deregister(this);
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		ArmVentPosition = ventTransform.position;
		ArmForward = ventTransform.forward;
	}

	public override void OnDamageDestroyed()
	{
		base.OnDamageDestroyed();
		if (GameManager.RunSimulation)
		{
			VentArmAtmosphereToWorld();
		}
	}

	public override void OnStructureBroken()
	{
		base.OnStructureBroken();
		if (GameManager.RunSimulation)
		{
			VentArmAtmosphereToWorld();
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		HandleOperatingAudio(force: true);
		ArmVentPosition = ventTransform.position;
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if ((bool)switchMode)
		{
			switchMode.RefreshState(skipAnimation);
		}
	}

	private void ResetSettings()
	{
		if (GameManager.RunSimulation)
		{
			if (Mode == 0)
			{
				ExternalPressure = ((DockedVent != null) ? (Chemistry.OneAtmosphere * 500.0) : Chemistry.OneAtmosphere);
				InternalPressure = PressurekPa.Zero;
			}
			else
			{
				ExternalPressure = PressurekPa.Zero;
				InternalPressure = Chemistry.OneAtmosphere * 500.0;
			}
		}
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		extendedText.Append(GameStrings.RoboticArmVentDirection.AsString(VentDirectionStrings[(int)VentDirection].AsColor("yellow"))).AppendLine();
		extendedText.Append(AtmosphericsManager.DisplayBasicAtmosphere(base.InternalAtmosphere));
		return extendedText;
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		switch (interactable.Action)
		{
		case InteractableType.OnOff:
		case InteractableType.Powered:
			HandleOperatingAudio();
			break;
		case InteractableType.Mode:
			HandleOperatingAudio();
			ResetSettings();
			break;
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Mode)
		{
			return HandleMode(interactable, doAction);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	private DelayedActionInstance HandleMode(Interactable interactable, bool doAction = true)
	{
		VentDirection state = ((interactable.State != 1) ? VentDirection.Inward : VentDirection.Outward);
		if (doAction)
		{
			OnServer.Interact(base.InteractMode, (int)state);
		}
		return new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		}.Succeed();
	}

	protected override void AnimateDownFinished()
	{
		HandleOperatingAudio();
		switchMode.RefreshState();
		SetTargetSmallGrid();
		ArmVentPosition = ventTransform.position;
	}

	protected override void AnimateUpStarted()
	{
		base.AnimateUpStarted();
		switchMode.RefreshState();
		HandleOperatingAudio();
	}

	protected override void AnimateUpFinished()
	{
		base.AnimateUpFinished();
		ArmVentPosition = ventTransform.position;
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (GameManager.IsBatchMode)
		{
			return;
		}
		float num = ((base.InternalAtmosphere?.PressureGassesAndLiquids / MAXPressure) ?? PressurekPa.Zero).ToFloat();
		if (float.IsNaN(num))
		{
			num = 0f;
		}
		needle1.UpdateAngle(num, Time.deltaTime);
		needle2.UpdateAngle(num, Time.deltaTime);
		if (!IsOccluded)
		{
			if ((bool)needle1.transform)
			{
				needle1.ApplyRotationToTransform();
			}
			if ((bool)needle2.transform)
			{
				needle2.ApplyRotationToTransform();
			}
		}
	}

	protected override void SetTargetSmallGrid()
	{
		if (base.CurrentBypass != null)
		{
			DockedVent = null;
		}
		else
		{
			DockedVent = GetArmInteractionCell()?.Pipe as PassiveVent;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (hitCollider == Gauge1Collider || hitCollider == Gauge2Collider)
		{
			PassiveTooltip result = new PassiveTooltip(true);
			result.Title = DisplayName;
			result.Extended = AtmosphericsManager.DisplayBasicAtmosphere(base.InternalAtmosphere);
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		HandleOverPressure();
		if (IsOperable && base.ArmState == ArmState.Down)
		{
			HandleVentDirection();
		}
	}

	private void HandleOverPressure()
	{
		if (RocketMath.Abs(base.GridController.AtmosphericsController.SampleGlobalAtmosphere(new WorldGrid(ArmVentPosition)).PressureGassesAndLiquids - base.InternalAtmosphere.PressureGassesAndLiquids) >= MAXPressure)
		{
			if (_damageTicker > 5)
			{
				DamageState.Damage(ChangeDamageType.Increment, 5f, DamageUpdateType.Brute);
			}
			_damageTicker++;
		}
		else
		{
			_damageTicker = 0;
		}
	}

	private void HandleVentDirection()
	{
		if ((object)DockedVent == null || !DockedVent.HasOnOffState || DockedVent.OnOff)
		{
			Atmosphere atmosphere = AtmosphericsController.World.SampleGlobalAtmosphere(new WorldGrid(ArmVentPosition));
			if (DockedVent != null && DockedVent.PipeNetwork?.Atmosphere != null)
			{
				atmosphere = DockedVent.PipeNetwork.Atmosphere;
			}
			TemperatureKelvin totalTemperature = Atmosphere.CalculateCombinedTotalTemperature(atmosphere, base.InternalAtmosphere);
			if (FilterSlot.Contains<GasFilter>(out var occupant) && occupant.IsEmpty)
			{
				occupant = null;
			}
			switch (VentDirection)
			{
			case VentDirection.Inward:
				PumpGasInward(atmosphere, totalTemperature, occupant);
				break;
			case VentDirection.Outward:
				PumpGasOutward(atmosphere, totalTemperature, occupant);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
	}

	private void PumpGasOutward(Atmosphere targetAtmosphere, TemperatureKelvin totalTemperature, GasFilter gasFilter)
	{
		Atmosphere internalAtmosphere = base.InternalAtmosphere;
		MoleQuantity val = IdealGas.Quantity(internalAtmosphere.PressureGassesAndLiquids - InternalPressure, internalAtmosphere.Volume, internalAtmosphere.Temperature);
		PressurekPa pressure = RocketMath.Clamp(RocketMath.Min(PressurePerTick, ExternalPressure - targetAtmosphere.PressureGassesAndLiquids), PressurekPa.Zero, PressurePerTick);
		VolumeLitres volumeLitres = targetAtmosphere.Volume / (targetAtmosphere.Volume / Chemistry.GridVolume);
		volumeLitres = (volumeLitres.IsNaN() ? targetAtmosphere.Volume : volumeLitres);
		MoleQuantity val2 = IdealGas.Quantity(pressure, volumeLitres, totalTemperature);
		val2 = RocketMath.Min(val2, val);
		GasMixture fromMix = internalAtmosphere.Remove(val2, AtmosphereHelper.MatterState.All);
		if (fromMix.IsValid)
		{
			if ((object)gasFilter == null)
			{
				targetAtmosphere.Add(fromMix);
				return;
			}
			gasFilter.FilterGas(ref fromMix, ref targetAtmosphere.GasMixture, internalAtmosphere, MinimumRatioToFilterAll);
			internalAtmosphere.Add(fromMix);
		}
	}

	private void PumpGasInward(Atmosphere targetAtmosphere, TemperatureKelvin totalTemperature, GasFilter gasFilter)
	{
		Atmosphere internalAtmosphere = base.InternalAtmosphere;
		PressurekPa val = targetAtmosphere.PressureGasses - ExternalPressure;
		VolumeLitres volumeLitres = targetAtmosphere.Volume / (targetAtmosphere.Volume / Chemistry.GridVolume);
		volumeLitres = (volumeLitres.IsNaN() ? targetAtmosphere.Volume : volumeLitres);
		MoleQuantity val2 = IdealGas.Quantity(RocketMath.Min(PressurePerTick, val), volumeLitres, totalTemperature);
		MoleQuantity val3 = IdealGas.Quantity(InternalPressure - internalAtmosphere.PressureGasses, internalAtmosphere.Volume, totalTemperature);
		MoleQuantity transferMoles = RocketMath.Min(val2, val3);
		GasMixture fromMix = targetAtmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.Gas);
		if (fromMix.IsValid)
		{
			if ((object)gasFilter == null)
			{
				internalAtmosphere.Add(fromMix);
				return;
			}
			gasFilter.FilterGas(ref fromMix, ref base.InternalAtmosphere.GasMixture, targetAtmosphere, MinimumRatioToFilterAll);
			targetAtmosphere.Add(fromMix);
		}
	}

	private void VentArmAtmosphereToWorld()
	{
		if (!Singleton<GameManager>.IsQuitting && GameManager.GameState != GameState.None && !IsCursor)
		{
			AtmosphericEventInstance.CloneGlobalAddGasMix(new WorldGrid(ArmVentPosition), base.InternalAtmosphere.GasMixture, base.InternalAtmosphere.Inflamed);
			AtmosphericEventInstance.Reset(base.InternalAtmosphere);
		}
	}

	public override void RailNetworkUpdated()
	{
		base.RailNetworkUpdated();
		switchMode.RefreshState();
		HandleOperatingAudio();
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.PressureExternal => true, 
			LogicType.PressureInternal => true, 
			LogicType.PressureInput => true, 
			LogicType.TemperatureInput => true, 
			LogicType.RatioOxygenInput => true, 
			LogicType.RatioNitrogenInput => true, 
			LogicType.RatioCarbonDioxideInput => true, 
			LogicType.RatioMethaneInput => true, 
			LogicType.RatioPollutantInput => true, 
			LogicType.RatioWaterInput => true, 
			LogicType.RatioNitrousOxideInput => true, 
			LogicType.RatioLiquidNitrogenInput => true, 
			LogicType.RatioLiquidOxygenInput => true, 
			LogicType.RatioLiquidMethaneInput => true, 
			LogicType.RatioSteamInput => true, 
			LogicType.RatioLiquidCarbonDioxideInput => true, 
			LogicType.RatioLiquidPollutantInput => true, 
			LogicType.RatioLiquidNitrousOxideInput => true, 
			LogicType.TotalMolesInput => true, 
			LogicType.CombustionInput => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.PressureExternal => true, 
			LogicType.PressureInternal => true, 
			_ => base.CanLogicWrite(logicType), 
		};
	}

	private Atmosphere GetInputAtmos()
	{
		if (DockedVent != null)
		{
			return DockedVent.PipeNetwork.Atmosphere;
		}
		return AtmosphericsController.World.SampleGlobalAtmosphere(new WorldGrid(ArmVentPosition));
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.PressureExternal:
			return ExternalPressure.ToDouble();
		case LogicType.PressureInternal:
			return InternalPressure.ToDouble();
		case LogicType.PressureInput:
			return GetInputAtmos()?.PressureGassesAndLiquids.ToDouble() ?? 0.0;
		case LogicType.TemperatureInput:
			return GetInputAtmos()?.Temperature.ToDouble() ?? 0.0;
		case LogicType.RatioOxygenInput:
		case LogicType.RatioCarbonDioxideInput:
		case LogicType.RatioNitrogenInput:
		case LogicType.RatioPollutantInput:
		case LogicType.RatioMethaneInput:
		case LogicType.RatioWaterInput:
		case LogicType.RatioNitrousOxideInput:
		case LogicType.RatioLiquidNitrogenInput:
		case LogicType.RatioLiquidOxygenInput:
		case LogicType.RatioLiquidMethaneInput:
		case LogicType.RatioSteamInput:
		case LogicType.RatioLiquidCarbonDioxideInput:
		case LogicType.RatioLiquidPollutantInput:
		case LogicType.RatioLiquidNitrousOxideInput:
		case LogicType.RatioHydrogenInput:
		case LogicType.RatioLiquidHydrogenInput:
		case LogicType.RatioPollutedWaterInput:
		case LogicType.RatioHydrazineInput:
		case LogicType.RatioLiquidHydrazineInput:
		case LogicType.RatioLiquidAlcoholInput:
		case LogicType.RatioHeliumInput:
		case LogicType.RatioLiquidSodiumChlorideInput:
		case LogicType.RatioSilanolInput:
		case LogicType.RatioLiquidSilanolInput:
		case LogicType.RatioHydrochloricAcidInput:
		case LogicType.RatioLiquidHydrochloricAcidInput:
		case LogicType.RatioOzoneInput:
		case LogicType.RatioLiquidOzoneInput:
			return AtmosphereHelper.GasRatio(logicType, GetInputAtmos());
		case LogicType.TotalMolesInput:
			return GetInputAtmos()?.TotalMoles.ToDouble() ?? 0.0;
		case LogicType.CombustionInput:
			return (GetInputAtmos() != null) ? (GetInputAtmos().Inflamed ? 1 : 0) : 0;
		default:
			return base.GetLogicValue(logicType);
		}
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

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RoboticArmDockAtmosSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData thingSaveData)
	{
		base.InitialiseSaveData(ref thingSaveData);
		if (thingSaveData is RoboticArmDockAtmosSaveData roboticArmDockAtmosSaveData)
		{
			roboticArmDockAtmosSaveData.ExternalPressure = ExternalPressure.ToFloat();
			roboticArmDockAtmosSaveData.InternalPressure = InternalPressure.ToFloat();
		}
	}

	public override void DeserializeSave(ThingSaveData thingSaveData)
	{
		base.DeserializeSave(thingSaveData);
		if (thingSaveData is RoboticArmDockAtmosSaveData roboticArmDockAtmosSaveData)
		{
			ExternalPressure = new PressurekPa(roboticArmDockAtmosSaveData.ExternalPressure);
			InternalPressure = new PressurekPa(roboticArmDockAtmosSaveData.InternalPressure);
		}
		else
		{
			ResetSettings();
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
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			ExternalPressure = new PressurekPa(reader.ReadSingle());
			InternalPressure = new PressurekPa(reader.ReadSingle());
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(ExternalPressure.ToFloat());
		writer.WriteSingle(InternalPressure.ToFloat());
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		ExternalPressure = new PressurekPa(reader.ReadSingle());
		InternalPressure = new PressurekPa(reader.ReadSingle());
	}
}
