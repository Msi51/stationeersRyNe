using System;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Electrical;

public class StirlingEngine : DeviceInputOutput, IThermal
{
	[Header("StirlingEngine")]
	[SerializeField]
	[Tooltip("Volume in liters of the pistons. a larger volume means a greater energy difference between hot and cold sides, meaning a greater potential power output")]
	private float PistonVolume = 4f;

	[SerializeField]
	[Tooltip("Volume in liters of the regenerator. This atmosphere connects the hot side and cold side")]
	private float InternalVolume = 56f;

	[SerializeField]
	[Tooltip("Area in m2 of the heat exchanger that moves heat from the hot side pipe network into the engine. A larger area means it can move more heat and generate more power")]
	private float HotSideHeatExchangerArea = 3f;

	[SerializeField]
	[Tooltip("Area in m2 of the heat exchanger with the cold side pipe networks into the engine. A larger area means it can move more heat and generate more power")]
	private float ColdSideHeatExchangerArea = 2f;

	[SerializeField]
	[Tooltip("Volume of Atmosphere that exchanges heat with the hot/cold side atmospheres inside the engine")]
	private float HeatExchangerAtmosphereVolume = 10f;

	[SerializeField]
	[Tooltip("Multiplier for how efficiently the engine turns heat into power vs Temperature of world atmosphere (K), NOTE: The Internal gas has its own efficiency multiplier ranging from 0.05 to 0.15")]
	private AnimationCurve MachineEfficiency = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(3000f, 1f));

	[Tooltip("The Rpm of the engines drive shaft at maximum power")]
	[SerializeField]
	private float MaxRpm = 300f;

	[Tooltip("The Maximum Power the engine can produce")]
	[SerializeField]
	[FormerlySerializedAs("MaxPower")]
	private float maxPower = 6000f;

	[Tooltip("The Pressure differential between the hot side and the cold side above which the Engine is most efficient (in Kpa)")]
	[SerializeField]
	private float IdealPressureDifferential = 3000f;

	[SerializeField]
	private Collider _infoPanel;

	[SerializeField]
	private AnimationCurve FanLowPitch;

	[SerializeField]
	private AnimationCurve FanLowVolume;

	[SerializeField]
	private AnimationCurve FanHighVolume;

	private readonly Atmosphere _hotInputAtmosphere = new Atmosphere();

	private readonly Atmosphere _hotSideAtmosphere = new Atmosphere();

	private readonly Atmosphere _coldSideAtmosphere = new Atmosphere();

	private static readonly TemperatureKelvin MAXOperatingTemperature = new TemperatureKelvin(3000.0);

	private static readonly int RpsHash = Animator.StringToHash("Rps");

	private MoleEnergy _energyAsPower;

	private float _machinePressureDifferentialEfficiency;

	private TemperatureKelvin _hotSideTemperature;

	private TemperatureKelvin _coldSideTemperature;

	private float _machineEnvironmentEfficiency;

	private float _workingGasEfficiency;

	private float _animationUpdateTolerance = 0.1f;

	private float _animationLerpSpeed = 0.5f;

	private GameAudioEvent _fanLow;

	private GameAudioEvent _fanHigh;

	private float _fanRps;

	public float ExplosionForce = 1850f;

	public static readonly float ExplosionRadius = 2f;

	public static readonly float MaxExplosionRadius = 4f;

	[NonSerialized]
	public bool HasBlown;

	private static System.Random _random = new System.Random();

	private static readonly int EngineStressedHash = Animator.StringToHash("EngineStressed");

	private bool _isStressedSound;

	public MoleEnergy MaxPower => new MoleEnergy(maxPower);

	private GasCanister GasCanister => Slots[0].Occupant as GasCanister;

	private MoleEnergy EnergyAsPower
	{
		get
		{
			return _energyAsPower;
		}
		set
		{
			if (!RocketMath.Approximately(value, _energyAsPower))
			{
				_energyAsPower = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 512;
				}
			}
		}
	}

	[ByteArraySync]
	private float MachinePressureDifferentialEfficiency
	{
		get
		{
			return _machinePressureDifferentialEfficiency;
		}
		set
		{
			if (!RocketMath.Approximately(value, _machinePressureDifferentialEfficiency))
			{
				_machinePressureDifferentialEfficiency = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 512;
				}
			}
		}
	}

	[ByteArraySync]
	private TemperatureKelvin HotSideTemperature
	{
		get
		{
			return _hotSideTemperature;
		}
		set
		{
			if (!RocketMath.Approximately(value, _hotSideTemperature))
			{
				_hotSideTemperature = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 512;
				}
			}
		}
	}

	[ByteArraySync]
	private TemperatureKelvin ColdSideTemperature
	{
		get
		{
			return _coldSideTemperature;
		}
		set
		{
			if (!RocketMath.Approximately(value, _coldSideTemperature))
			{
				_coldSideTemperature = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 512;
				}
			}
		}
	}

	public override bool HasReadableAtmosphere => true;

	public bool IsStressedSound
	{
		get
		{
			return _isStressedSound;
		}
		set
		{
			if (value == IsStressedSound)
			{
				return;
			}
			_isStressedSound = value;
			if (IsStressedSound)
			{
				if (ThreadedManager.IsThread)
				{
					UnityMainThreadDispatcher.Instance().Enqueue(delegate
					{
						PlaySound(EngineStressedHash);
					});
				}
				else
				{
					PlaySound(EngineStressedHash);
				}
			}
			else if (ThreadedManager.IsThread)
			{
				UnityMainThreadDispatcher.Instance().Enqueue(delegate
				{
					StopSound(EngineStressedHash);
				});
			}
			else
			{
				StopSound(EngineStressedHash);
			}
		}
	}

	protected override bool IsOperable
	{
		get
		{
			bool flag = IsAbleToOperate();
			if (Error == 1)
			{
				if (!flag)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (flag)
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

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None)
		{
			_hotSideAtmosphere.Volume = new VolumeLitres(PistonVolume);
			_coldSideAtmosphere.Volume = new VolumeLitres(PistonVolume);
			_hotInputAtmosphere.Volume = new VolumeLitres(HeatExchangerAtmosphereVolume);
			_fanLow = GetAudioEvent(Animator.StringToHash("FanLow"));
			_fanHigh = GetAudioEvent(Animator.StringToHash("FanHigh"));
		}
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, new VolumeLitres(InternalVolume), 0L);
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(EnergyAsPower.ToFloat());
			writer.WriteSingle(MachinePressureDifferentialEfficiency);
			writer.WriteSingle(HotSideTemperature.ToFloat());
			writer.WriteSingle(ColdSideTemperature.ToFloat());
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			EnergyAsPower = new MoleEnergy(reader.ReadSingle());
			MachinePressureDifferentialEfficiency = reader.ReadSingle();
			HotSideTemperature = new TemperatureKelvin(reader.ReadSingle());
			ColdSideTemperature = new TemperatureKelvin(reader.ReadSingle());
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(EnergyAsPower.ToFloat());
		writer.WriteSingle(MachinePressureDifferentialEfficiency);
		writer.WriteSingle(HotSideTemperature.ToFloat());
		writer.WriteSingle(ColdSideTemperature.ToFloat());
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		EnergyAsPower = new MoleEnergy(reader.ReadSingle());
		MachinePressureDifferentialEfficiency = reader.ReadSingle();
		HotSideTemperature = new TemperatureKelvin(reader.ReadSingle());
		ColdSideTemperature = new TemperatureKelvin(reader.ReadSingle());
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		if (!IsOccluded && Powered)
		{
			_fanLow.SetPitchMultiplier(FanLowPitch.Evaluate(_fanRps));
			_fanLow.SetVolumeMultiplier(FanLowVolume.Evaluate(_fanRps));
			_fanHigh.SetVolumeMultiplier(FanHighVolume.Evaluate(_fanRps));
		}
	}

	public override void UpdateEachFrame()
	{
		if (WorldManager.IsGamePaused)
		{
			return;
		}
		base.UpdateEachFrame();
		if (!GameManager.IsBatchMode && !IsOccluded && BaseAnimator != null && BaseAnimator.HasParameter(RpsHash))
		{
			float num = BaseAnimator.GetFloat(RpsHash);
			float num2 = MaxRpm * Mathf.Clamp01((EnergyAsPower / MaxPower).ToFloat()) / 60f;
			if (!RocketMath.Approximately(num, num2, _animationUpdateTolerance))
			{
				_fanRps = Mathf.Lerp(num, num2, GameManager.DeltaTime * _animationLerpSpeed);
				BaseAnimator.SetFloat(RpsHash, _fanRps);
			}
		}
	}

	public override bool AllowSetPower(CableNetwork cableNetwork)
	{
		return false;
	}

	public override float GetGeneratedPower(CableNetwork cableNetwork)
	{
		if (cableNetwork != base.PowerCableNetwork)
		{
			return 0f;
		}
		return EnergyAsPower.ToFloat();
	}

	public override CanConstructInfo CanConstruct()
	{
		if (HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
	}

	private GasMixture ResetGasMixtures()
	{
		GasMixture result = GasMixtureHelper.Create();
		result.Add(_hotSideAtmosphere.GasMixture);
		result.Add(_coldSideAtmosphere.GasMixture);
		result.Add(base.InternalAtmosphere.GasMixture);
		_hotSideAtmosphere.GasMixture.Reset();
		_coldSideAtmosphere.GasMixture.Reset();
		base.InternalAtmosphere.GasMixture.Reset();
		return result;
	}

	public override void OnAtmosphericTick()
	{
		if (!OnOff)
		{
			EnergyAsPower = MoleEnergy.Zero;
			if (GasCanister != null && GasCanister.InternalAtmosphere != null)
			{
				GasCanister.InternalAtmosphere.GasMixture.Add(ResetGasMixtures());
			}
			if (Powered)
			{
				OnServer.Interact(base.InteractPowered, 0);
			}
			return;
		}
		if ((GasCanister == null || GasCanister.IsBroken) && base.InternalAtmosphere != null && base.InternalAtmosphere.TotalMoles > Chemistry.MINIMUM_QUANTITY_MOLES)
		{
			base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L)?.Add(ResetGasMixtures());
			EnergyAsPower = MoleEnergy.Zero;
			if (Powered)
			{
				OnServer.Interact(base.InteractPowered, 0);
			}
			return;
		}
		if (!IsOperable || base.InternalAtmosphere == null || InputNetwork.Atmosphere == null || OutputNetwork.Atmosphere == null || GasCanister?.InternalAtmosphere == null)
		{
			EnergyAsPower = MoleEnergy.Zero;
			if (Powered)
			{
				OnServer.Interact(base.InteractPowered, 0);
			}
			return;
		}
		if (GasCanister.InternalAtmosphere.TotalMoles > Chemistry.MINIMUM_QUANTITY_MOLES)
		{
			base.InternalAtmosphere.Add(GasCanister.InternalAtmosphere.GasMixture);
			GasCanister.InternalAtmosphere.GasMixture.Reset();
		}
		AtmosphereHelper.Mix(_hotSideAtmosphere, base.InternalAtmosphere, AtmosphereHelper.MatterState.Gas);
		AtmosphereHelper.Mix(_coldSideAtmosphere, base.InternalAtmosphere, AtmosphereHelper.MatterState.Gas);
		AtmosphereHelper.Mix(_hotInputAtmosphere, InputNetwork.Atmosphere, AtmosphereHelper.MatterState.All);
		MoleEnergy convectionHeat = AtmosphereHelper.GetConvectionHeat(_hotInputAtmosphere, _hotSideAtmosphere, HotSideHeatExchangerArea * _hotInputAtmosphere.HeatExchangeRatio());
		_hotInputAtmosphere.GasMixture.TransferEnergyTo(ref _hotSideAtmosphere.GasMixture, convectionHeat * AtmosphericsManager.Instance.TickSpeedSeconds * 1.0);
		AtmosphereHelper.Mix(_hotInputAtmosphere, OutputNetwork.Atmosphere, AtmosphereHelper.MatterState.All);
		Atmosphere atmosphere = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
		if (atmosphere != null)
		{
			convectionHeat = AtmosphereHelper.GetConvectionHeat(atmosphere, _coldSideAtmosphere, ColdSideHeatExchangerArea * atmosphere.HeatExchangeRatio());
			atmosphere.GasMixture.TransferEnergyTo(ref _coldSideAtmosphere.GasMixture, convectionHeat * AtmosphericsManager.Instance.TickSpeedSeconds * 1.0);
		}
		MoleEnergy moleEnergy = RocketMath.Abs(_hotSideAtmosphere.GasMixture.TotalEnergy - _coldSideAtmosphere.GasMixture.TotalEnergy);
		_machineEnvironmentEfficiency = ((atmosphere != null) ? MachineEfficiency.Evaluate(RocketMath.Clamp(atmosphere.Temperature, TemperatureKelvin.Zero, MAXOperatingTemperature).ToFloat()) : 1f);
		PressurekPa pressurekPa = RocketMath.Max(PressurekPa.Zero, _hotSideAtmosphere.PressureGassesAndLiquids - _coldSideAtmosphere.PressureGassesAndLiquids);
		MachinePressureDifferentialEfficiency = Mathf.Clamp01((pressurekPa / IdealPressureDifferential).ToFloat());
		_workingGasEfficiency = base.InternalAtmosphere.GasMixture.ThermalEfficiency();
		EnergyAsPower = RocketMath.Min(MaxPower, moleEnergy * _workingGasEfficiency * _machineEnvironmentEfficiency * MachinePressureDifferentialEfficiency);
		HotSideTemperature = _hotSideAtmosphere.Temperature;
		ColdSideTemperature = _coldSideAtmosphere.Temperature;
		_hotSideAtmosphere.GasMixture.RemoveEnergy(EnergyAsPower);
		HandlePressureCheck();
		if (EnergyAsPower > new MoleEnergy(1.0) && !Powered)
		{
			OnServer.Interact(base.InteractPowered, 1);
		}
	}

	public override void OnAtmosphereClient()
	{
		if (OnOff && Error != 1)
		{
			Atmosphere atmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
			_machineEnvironmentEfficiency = ((atmosphere != null) ? MachineEfficiency.Evaluate(RocketMath.Clamp(atmosphere.Temperature, TemperatureKelvin.Zero, MAXOperatingTemperature).ToFloat()) : 1f);
			_workingGasEfficiency = base.InternalAtmosphere.GasMixture.ThermalEfficiency();
		}
	}

	public void HandlePressureCheck()
	{
		if (!base.HasOpenGrid)
		{
			return;
		}
		Atmosphere atmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
		PressurekPa pressurekPa = PressurekPa.Zero;
		PressurekPa pressureGassesAndLiquids = base.InternalAtmosphere.PressureGassesAndLiquids;
		if (atmosphere != null)
		{
			pressurekPa = atmosphere.PressureGassesAndLiquids;
		}
		PressurekPa pressurekPa2 = RocketMath.Abs(pressurekPa - pressureGassesAndLiquids);
		_ = GameManager.IsBatchMode;
		int num = (int)Mathf.Clamp((int)pressurekPa2.ToFloat(), 2f, float.PositiveInfinity);
		if (!base.Indestructable && !IsBroken && (float)num > base.MaxPressureDelta.ToFloat() && (float)_random.Next(1, num) > base.MaxPressureDelta.ToFloat())
		{
			if (!HasBlown)
			{
				base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L).Add(ResetGasMixtures());
				UnityMainThreadDispatcher.Instance().Enqueue(Explode);
				StopAllAudio(immediate: true);
				HasBlown = true;
			}
			DamageState.Damage(ChangeDamageType.Increment, 200f, DamageUpdateType.Brute);
		}
	}

	public override void Explode()
	{
		base.Explode();
		if (GameManager.RunSimulation && base.InternalAtmosphere.IsAboveArmstrong())
		{
			global::Explosion.Explode(ExplosionForce * (base.InternalAtmosphere.PressureGassesAndLiquids / base.MaxPressureDelta).ToFloat(), radius: Mathf.Clamp(ExplosionRadius * base.InternalAtmosphere.PressureGassesAndLiquids.ToFloat() / base.MaxPressureDelta.ToFloat(), 0f, MaxExplosionRadius), pos: base.Position);
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.PowerGeneration:
			return true;
		case LogicType.Pressure:
		case LogicType.Temperature:
		case LogicType.RatioOxygen:
		case LogicType.RatioCarbonDioxide:
		case LogicType.RatioNitrogen:
		case LogicType.RatioPollutant:
		case LogicType.RatioMethane:
		case LogicType.RatioWater:
		case LogicType.Quantity:
		case LogicType.Volume:
		case LogicType.RatioNitrousOxide:
		case LogicType.Combustion:
		case LogicType.EnvironmentEfficiency:
		case LogicType.WorkingGasEfficiency:
		case LogicType.RatioLiquidNitrogen:
		case LogicType.RatioLiquidOxygen:
		case LogicType.RatioLiquidMethane:
		case LogicType.RatioSteam:
		case LogicType.RatioLiquidCarbonDioxide:
		case LogicType.RatioLiquidPollutant:
		case LogicType.RatioLiquidNitrousOxide:
		case LogicType.RatioHydrogen:
		case LogicType.RatioLiquidHydrogen:
		case LogicType.RatioPollutedWater:
		case LogicType.RatioHydrazine:
		case LogicType.RatioLiquidHydrazine:
		case LogicType.RatioLiquidAlcohol:
		case LogicType.RatioHelium:
		case LogicType.RatioLiquidSodiumChloride:
		case LogicType.RatioSilanol:
		case LogicType.RatioLiquidSilanol:
		case LogicType.RatioHydrochloricAcid:
		case LogicType.RatioLiquidHydrochloricAcid:
		case LogicType.RatioOzone:
		case LogicType.RatioLiquidOzone:
			return true;
		default:
			return base.CanLogicRead(logicType);
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		Atmosphere atmosphere = ((!OnOff && GasCanister?.InternalAtmosphere != null) ? GasCanister.InternalAtmosphere : base.InternalAtmosphere);
		switch (logicType)
		{
		case LogicType.PowerGeneration:
			return EnergyAsPower.ToDouble();
		case LogicType.Combustion:
			if (!atmosphere.Sparked)
			{
				return 0.0;
			}
			return 1.0;
		case LogicType.RatioOxygen:
		case LogicType.RatioCarbonDioxide:
		case LogicType.RatioNitrogen:
		case LogicType.RatioPollutant:
		case LogicType.RatioMethane:
		case LogicType.RatioWater:
		case LogicType.RatioNitrousOxide:
		case LogicType.RatioLiquidNitrogen:
		case LogicType.RatioLiquidOxygen:
		case LogicType.RatioLiquidMethane:
		case LogicType.RatioSteam:
		case LogicType.RatioLiquidCarbonDioxide:
		case LogicType.RatioLiquidPollutant:
		case LogicType.RatioLiquidNitrousOxide:
		case LogicType.RatioHydrogen:
		case LogicType.RatioLiquidHydrogen:
		case LogicType.RatioPollutedWater:
		case LogicType.RatioHydrazine:
		case LogicType.RatioLiquidHydrazine:
		case LogicType.RatioLiquidAlcohol:
		case LogicType.RatioHelium:
		case LogicType.RatioLiquidSodiumChloride:
		case LogicType.RatioSilanol:
		case LogicType.RatioLiquidSilanol:
		case LogicType.RatioHydrochloricAcid:
		case LogicType.RatioLiquidHydrochloricAcid:
		case LogicType.RatioOzone:
		case LogicType.RatioLiquidOzone:
			if (atmosphere == null)
			{
				return 0.0;
			}
			return AtmosphereHelper.GasRatio(logicType, atmosphere);
		case LogicType.Pressure:
			return atmosphere?.PressureGassesAndLiquids.ToDouble() ?? 0.0;
		case LogicType.Quantity:
			if (atmosphere == null)
			{
				return 0.0;
			}
			return (atmosphere.TotalMoles + _hotSideAtmosphere.TotalMoles + _coldSideAtmosphere.TotalMoles).ToDouble();
		case LogicType.Temperature:
			return atmosphere?.Temperature.ToDouble() ?? 0.0;
		case LogicType.EnvironmentEfficiency:
			return _machineEnvironmentEfficiency;
		case LogicType.WorkingGasEfficiency:
			return _workingGasEfficiency;
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		Tooltip.ToolTipStringBuilder.Clear();
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (passiveTooltip.Title.Equals(string.Empty))
		{
			passiveTooltip.Title = DisplayName;
		}
		if (_infoPanel != null && hitCollider == _infoPanel)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (Error == 0)
			{
				string color = ((base.InternalAtmosphere.PressureGasses > base.MaxPressureDelta * 0.800000011920929) ? "red" : "yellow");
				StringManager.AddKeyValueLine(stringBuilder, GameStrings.GeneratingPower, EnergyAsPower.ToFloat().ToStringPrefix("W", "yellow"));
				StringManager.AddKeyValueLine(stringBuilder, GameStrings.Pressure, base.InternalAtmosphere.PressureGassesAndLiquidsInPa.ToStringPrefix("Pa", color));
				StringManager.AddKeyValueLine(stringBuilder, GameStrings.StirlingTemperatureHot, HotSideTemperature.ToFloat().ToStringPrefix("K", "yellow"));
				StringManager.AddKeyValueLine(stringBuilder, GameStrings.StirlingTemperatureCold, ColdSideTemperature.ToFloat().ToStringPrefix("K", "yellow"));
				StringManager.AddKeyValueLine(stringBuilder, GameStrings.StirlingOperatingEfficiency, (_machineEnvironmentEfficiency * 100f).ToStringPercent("yellow"));
				StringManager.AddKeyValueLine(stringBuilder, GameStrings.StirlingPressureDifferential, (MachinePressureDifferentialEfficiency * 100f).ToStringPercent("yellow"));
				StringManager.AddKeyValueLine(stringBuilder, GameStrings.StirlingGasEfficiency, (_workingGasEfficiency * 100f).ToStringPercent("yellow"));
				passiveTooltip.Extended = stringBuilder.ToString();
			}
			else
			{
				stringBuilder.AppendLine(GameStrings.StirlingMissingCanister.DisplayString);
				stringBuilder.AppendLine(GameStrings.StirlingHelperText.DisplayString);
				passiveTooltip.Extended = stringBuilder.ToString();
			}
		}
		return passiveTooltip;
	}

	private bool IsAbleToOperate()
	{
		bool flag = GasCanister != null && !GasCanister.IsBroken && !GasCanister.IsOpen;
		return base.IsInputValid && base.IsOutputValid && flag;
	}

	public override void AssessError()
	{
		bool flag = IsAbleToOperate();
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && !flag)
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if (GameManager.RunSimulation && HasErrorState && Error == 1 && flag)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new StirlingEngineSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is StirlingEngineSaveData stirlingEngineSaveData)
		{
			_hotInputAtmosphere.Load(stirlingEngineSaveData.HotInputAtmosphere);
			_hotSideAtmosphere.Load(stirlingEngineSaveData.HotSideAtmosphere);
			_coldSideAtmosphere.Load(stirlingEngineSaveData.ColdSideAtmosphere);
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is StirlingEngineSaveData stirlingEngineSaveData)
		{
			stirlingEngineSaveData.HotInputAtmosphere = new AtmosphereSaveData(_hotInputAtmosphere);
			stirlingEngineSaveData.HotSideAtmosphere = new AtmosphereSaveData(_hotSideAtmosphere);
			stirlingEngineSaveData.ColdSideAtmosphere = new AtmosphereSaveData(_coldSideAtmosphere);
		}
	}
}
