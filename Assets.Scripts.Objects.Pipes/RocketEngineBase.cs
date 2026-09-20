using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Pipes;

public class RocketEngineBase : DeviceInput, IRocketEngine, IRocketInternals, IRocketComponent, IRocketMassContributor
{
	public const int MAX_ATMOS_INTERACTION_GRID_HEIGHT = 10000;

	[FormerlySerializedAs("FlameEmmissionTransform")]
	public Transform FlameAtmosphereTransform;

	protected Vector3 _flamePosition;

	protected Vector3 _flameDirection;

	private WorldGrid _lastExhaustGrid;

	private bool _hasLastExhaustGrid;

	private const int MAX_EXHAUST_TRAIL_GRIDS = 64;

	public float internalVolume = 10f;

	private float _massEjectedLastTick;

	public float minimumForce = 100f;

	[SerializeField]
	[HideInInspector]
	private float _maxThrust;

	[SerializeField]
	[HideInInspector]
	private float _maxExhaustVelocity;

	[SerializeField]
	[HideInInspector]
	private float _maxFuelFlowRate;

	[Header("Input Connections")]
	[SerializeField]
	protected Connection _inputConnection1;

	[SerializeField]
	protected Connection _inputConnection2;

	protected PipeNetwork _inputNetwork1;

	protected PipeNetwork _inputNetwork2;

	protected const float SAFE_GAS_FUEL_TEMPERATURE = 215f;

	protected const float SAFE_LIQUID_FUEL_TEMPERATURE = 125f;

	protected const float VOLATILES_RATIO = 0.666f;

	protected float _maxThrottle = 100f;

	private bool _playStopStartSound;

	public List<RocketEngineEffect> EngineEffects = new List<RocketEngineEffect>(2);

	private const float MAX_AUDIO_ALTITUDE = 800f;

	public const float ENGINE_COMBUSTION_RATE = 0.96f;

	public Atmosphere SimpleRocketExhaust0;

	public Atmosphere SimpleRocketExhaust1;

	public Atmosphere SimpleRocketExhaust2;

	private float _throttle = 100f;

	private float _force;

	private float _exhaustVelocity;

	private TemperatureKelvin _exhaustTemperature;

	public override bool HasReadableAtmosphere => true;

	public VolumeLitres InternalVolume => new VolumeLitres(internalVolume);

	public virtual float EngineEfficiency => 18f;

	public virtual float MassContribution => 500f;

	public float MaxThrust => _maxThrust;

	public float MaxExhaustVelocity => _maxExhaustVelocity;

	public float MaxFuelFlowRate => _maxFuelFlowRate;

	public float SpecificImpulse => MaxExhaustVelocity / 9.8f;

	public bool PlayStopStartSound
	{
		get
		{
			return _playStopStartSound;
		}
		set
		{
			if (value != PlayStopStartSound)
			{
				_playStopStartSound = value;
				GetAudioEvent(Defines.Sounds.RocketStartHash)?.Trigger();
			}
		}
	}

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public float FlowRate { get; protected set; }

	public MoleQuantity PassedMoles { get; protected set; }

	protected override bool IsOperable
	{
		get
		{
			if (!base.IsStructureCompleted)
			{
				return false;
			}
			bool flag = IsInput1Valid && base.GridController.CanContainAtmos(new WorldGrid(_flamePosition));
			if (GameManager.RunSimulation && HasErrorState && Error == 0 && !flag)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (GameManager.RunSimulation && HasErrorState && Error == 1 && flag)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return flag;
		}
	}

	public bool IsInput1Valid
	{
		get
		{
			if (_inputNetwork1 != null && _inputNetwork1.IsNetworkValid())
			{
				return !_inputNetwork1.IsAwaitingEvent;
			}
			return false;
		}
	}

	public bool IsInput2Valid
	{
		get
		{
			if (_inputNetwork2 != null && _inputNetwork2.IsNetworkValid())
			{
				return !_inputNetwork2.IsAwaitingEvent;
			}
			return false;
		}
	}

	public float Throttle
	{
		get
		{
			return _throttle;
		}
		protected set
		{
			float throttle = Mathf.Clamp(value, 0f, _maxThrottle);
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 4096;
			}
			_throttle = throttle;
		}
	}

	public float Force
	{
		get
		{
			return _force;
		}
		protected set
		{
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 8192;
			}
			_force = value;
		}
	}

	public float ExhaustVelocity
	{
		get
		{
			return _exhaustVelocity;
		}
		protected set
		{
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
			_exhaustVelocity = value;
		}
	}

	public TemperatureKelvin ExhaustTemperature
	{
		get
		{
			return _exhaustTemperature;
		}
		protected set
		{
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 32768;
			}
			_exhaustTemperature = value;
		}
	}

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Engine;

	public float Thrust => Force;

	public float EfficiencyPercent => EngineEfficiency / 25f * 100f;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public override void OnPrefabLoad()
	{
		base.OnPrefabLoad();
		CalculateMaxThrust();
	}

	public void CalculateMaxThrust()
	{
		Atmosphere atmosphere = new Atmosphere
		{
			Volume = InternalVolume,
			Mode = AtmosphereHelper.AtmosphereMode.Thing
		};
		Atmosphere input = new Atmosphere
		{
			Volume = new VolumeLitres(100.0),
			Mode = AtmosphereHelper.AtmosphereMode.Network
		};
		Atmosphere input2 = new Atmosphere
		{
			Volume = new VolumeLitres(100.0),
			Mode = AtmosphereHelper.AtmosphereMode.Network
		};
		PrepareThrustSimulation(input, input2);
		MovePropellant(atmosphere, input, input2);
		CombustEngine(atmosphere);
		atmosphere.TryCombust(0.9599999785423279, force: true);
		SetEnginePerformanceValues();
	}

	protected virtual void PrepareThrustSimulation(Atmosphere input1, Atmosphere input2)
	{
	}

	private void SetEnginePerformanceValues()
	{
		_maxThrust = Thrust;
		_maxExhaustVelocity = ExhaustVelocity;
		_maxFuelFlowRate = FlowRate;
		Force = 0f;
		ExhaustVelocity = 0f;
		FlowRate = 0f;
		PassedMoles = MoleQuantity.Zero;
		ExhaustTemperature = TemperatureKelvin.Zero;
	}

	private void CloseSimulation()
	{
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.RocketEngineCategory);
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			Atmosphere atmosphere = (base.InternalAtmosphere = new Atmosphere(this, InternalVolume, 0L));
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		base.InternalAtmosphere.Volume = InternalVolume;
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		_flamePosition = FlameAtmosphereTransform.position;
		_flameDirection = FlameAtmosphereTransform.forward;
		if (!GameManager.IsBatchMode)
		{
			RocketEngineEffects();
		}
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		float num = Mathf.Clamp01(Force / MaxThrust);
		float num2 = RocketNetwork?.Rocket?.GetAltitude() ?? 800f;
		bool flag = Force > minimumForce;
		int num3;
		if (num2 < 800f)
		{
			RocketState? rocketState = RocketNetwork?.Rocket?.RocketState;
			num3 = ((!rocketState.HasValue || rocketState != RocketState.InSpace) ? 1 : 0);
		}
		else
		{
			num3 = 0;
		}
		bool flag2 = (byte)num3 != 0;
		bool flag3 = false;
		if (InventoryManager.Parent?.ParentSlot?.Parent is IRocketInternals rocketInternals)
		{
			flag3 = rocketInternals.RocketNetwork == RocketNetwork;
		}
		flag2 = flag2 || flag3;
		flag = flag && flag2;
		GameAudioEvent audioEvent = GetAudioEvent(Defines.Sounds.RocketCloseHash);
		GameAudioEvent audioEvent2 = GetAudioEvent(Defines.Sounds.RocketDistantHash);
		GameAudioEvent audioEvent3 = GetAudioEvent(Defines.Sounds.RocketAmbienceHash);
		if (audioEvent != null)
		{
			audioEvent.UpdatePlayState(flag);
			if (flag)
			{
				audioEvent.SetVolumeMultiplier(num);
				audioEvent.SetPitchMultiplier(Mathf.Lerp(0.75f, 1f, num));
			}
		}
		if (audioEvent2 != null)
		{
			audioEvent2.UpdatePlayState(flag);
			if (flag)
			{
				audioEvent2.SetVolumeMultiplier(num);
			}
		}
		if (audioEvent3 != null)
		{
			audioEvent3.UpdatePlayState(flag);
			if (flag)
			{
				audioEvent3.SetVolumeMultiplier(Mathf.Lerp(1f, 0f, num2 / 800f));
			}
		}
		PlayStopStartSound = Force > minimumForce;
	}

	private void RocketEngineEffects()
	{
		float thrust = Mathf.Clamp01(Force / MaxThrust);
		if (Force <= minimumForce)
		{
			foreach (RocketEngineEffect engineEffect in EngineEffects)
			{
				engineEffect?.SetRunning(isRunning: false);
			}
			return;
		}
		foreach (RocketEngineEffect engineEffect2 in EngineEffects)
		{
			if ((object)engineEffect2 != null)
			{
				engineEffect2.SetRunning(isRunning: true);
				engineEffect2.SetTemperature(Force);
				engineEffect2.SetThrust(thrust);
				engineEffect2.UpdateEachFrame();
			}
		}
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	protected override void OnBuildStateUpdated(int newState, int previousState)
	{
		base.OnBuildStateUpdated(newState, previousState);
		RocketNetwork?.Rocket?.MarkIconDirty();
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, emissive);
		RocketNetwork?.Rocket?.MarkIconDirty();
	}

	public override void OnPreAtmosphere()
	{
		base.OnPreAtmosphere();
		ClearExhaustBlocks();
		if (base.InternalAtmosphere == null || !IsOperable)
		{
			Force = 0f;
			base.InternalAtmosphere?.GasMixture.Reset();
			_hasLastExhaustGrid = false;
			return;
		}
		BlockPlumeNeighbourAtmospheres();
		Exhaust();
		SparkPlume();
		if (OnOff && Powered)
		{
			MovePropellant(base.InternalAtmosphere, _inputNetwork1?.Atmosphere, _inputNetwork2?.Atmosphere);
			CombustEngine(base.InternalAtmosphere);
		}
		else
		{
			ClearEngineValues();
		}
	}

	private void Exhaust()
	{
		WorldGrid lastExhaustGrid = new WorldGrid(_flamePosition);
		if (lastExhaustGrid.Value.y >= 10000 || !(_flamePosition != Vector3.zero))
		{
			base.InternalAtmosphere.Remove(base.InternalAtmosphere.TotalMoles, AtmosphereHelper.MatterState.All);
			_hasLastExhaustGrid = false;
			return;
		}
		Grid3 value = lastExhaustGrid.Value;
		Grid3 grid = value;
		if (_hasLastExhaustGrid && _lastExhaustGrid.Value.x == value.x && _lastExhaustGrid.Value.z == value.z && value.y > _lastExhaustGrid.Value.y)
		{
			grid = _lastExhaustGrid.Value + Grid3.Up;
			int num = value.y - 63 * Grid3.Up.y;
			if (grid.y < num)
			{
				grid.y = num;
			}
		}
		int num2 = (value.y - grid.y) / Grid3.Up.y + 1;
		Vector3 vector = _flameDirection * Force / 500f;
		Grid3 grid2 = grid;
		while (grid2.y <= value.y)
		{
			GasMixture gasMixture = base.InternalAtmosphere.Remove(base.InternalAtmosphere.TotalMoles / num2, AtmosphereHelper.MatterState.All);
			num2--;
			Atmosphere atmosphere = AtmosphericsController.World.CloneGlobalAtmosphere(new WorldGrid(grid2), 0L, setStateActive: true, allowCrewModules: false);
			if (atmosphere != null)
			{
				atmosphere.Add(gasMixture);
				atmosphere.Direction += vector;
				atmosphere.Sparked = true;
			}
			grid2 += Grid3.Up;
		}
		if (base.InternalAtmosphere.TotalMoles > MoleQuantity.Zero)
		{
			base.InternalAtmosphere.Remove(base.InternalAtmosphere.TotalMoles, AtmosphereHelper.MatterState.All);
		}
		_lastExhaustGrid = lastExhaustGrid;
		_hasLastExhaustGrid = true;
	}

	protected virtual void MovePropellant(Atmosphere internalAtmosphere, Atmosphere input1, Atmosphere input2)
	{
	}

	private void ClearEngineValues()
	{
		PassedMoles = MoleQuantity.Zero;
		ExhaustTemperature = TemperatureKelvin.Zero;
		ExhaustVelocity = 0f;
		FlowRate = 0f;
		Force = 0f;
	}

	private void CombustEngine(Atmosphere internalAtmosphere)
	{
		PassedMoles = internalAtmosphere.TotalMoles;
		internalAtmosphere.TryCombust(0.9599999785423279, force: true);
		ExhaustTemperature = internalAtmosphere.Temperature;
		ExhaustVelocity = ExitVelocity(internalAtmosphere);
		FlowRate = internalAtmosphere.GasMixture.MolarMassGassesGrams() * internalAtmosphere.TotalMolesGases.ToFloat() / GameManager.GameTickSpeedSeconds / 1000f;
		Force = FlowRate * ExhaustVelocity;
	}

	private void ClearExhaustBlocks()
	{
		if (SimpleRocketExhaust0 != null)
		{
			SimpleRocketExhaust0.SimpleRocketExhaust = false;
			SimpleRocketExhaust0 = null;
		}
		if (SimpleRocketExhaust1 != null)
		{
			SimpleRocketExhaust1.SimpleRocketExhaust = false;
			SimpleRocketExhaust1 = null;
		}
		if (SimpleRocketExhaust2 != null)
		{
			SimpleRocketExhaust2.SimpleRocketExhaust = false;
			SimpleRocketExhaust2 = null;
		}
	}

	private void BlockPlumeNeighbourAtmospheres()
	{
		WorldGrid worldGrid = new WorldGrid(_flamePosition);
		if (worldGrid.Value.y >= 10000 || Force < minimumForce)
		{
			return;
		}
		SimpleRocketExhaust0 = AtmosphericsController.World.CloneGlobalAtmosphere(worldGrid, 0L, setStateActive: true, allowCrewModules: false);
		SimpleRocketExhaust0.SimpleRocketExhaust = true;
		WorldGrid worldGrid2 = new WorldGrid(_flamePosition + Vector3.down * 2f);
		if (SimpleRocketExhaust0.OpenNeighbors.Contains(worldGrid2))
		{
			SimpleRocketExhaust1 = AtmosphericsController.World.CloneGlobalAtmosphere(worldGrid2, 0L, setStateActive: true, allowCrewModules: false);
			SimpleRocketExhaust1.SimpleRocketExhaust = true;
			WorldGrid worldGrid3 = new WorldGrid(_flamePosition + Vector3.down * 4f);
			if (SimpleRocketExhaust0.OpenNeighbors.Contains(worldGrid3))
			{
				SimpleRocketExhaust2 = AtmosphericsController.World.CloneGlobalAtmosphere(worldGrid3, 0L, setStateActive: true, allowCrewModules: false);
				SimpleRocketExhaust2.SimpleRocketExhaust = true;
			}
		}
	}

	public void SparkPlume()
	{
		if (SimpleRocketExhaust0 != null)
		{
			SimpleRocketExhaust0.Sparked = true;
		}
		if (SimpleRocketExhaust1 != null)
		{
			SimpleRocketExhaust1.Sparked = true;
		}
		if (SimpleRocketExhaust2 != null)
		{
			SimpleRocketExhaust2.Sparked = true;
		}
	}

	public float ExitVelocity(Atmosphere combustionAtmosphere)
	{
		double num = combustionAtmosphere.Temperature.ToDouble();
		float num2 = combustionAtmosphere.GasMixture.HeatCapacityRatio();
		return (float)((double)EngineEfficiency * Math.Sqrt((double)num2 * 8.3144 * num));
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (!base.IsStructureCompleted)
		{
			return extendedText;
		}
		string text = GameStrings.None.AsColor("yellow");
		extendedText.Append(GameStrings.Throttle.DisplayString).Append(" ").AppendLine(Throttle.ToStringPercent("yellow"));
		extendedText.Append(GameStrings.Thrust.DisplayString).Append(" ");
		extendedText.AppendLine((Force > 0f) ? Force.ToStringPrefix("N", "yellow") : text);
		extendedText.Append(GameStrings.ExhaustVelocity.DisplayString).Append(" ");
		extendedText.AppendLine((ExhaustVelocity > 0f) ? ExhaustVelocity.ToStringPrefix("m/s", "yellow") : text);
		extendedText.Append(GameStrings.ExhaustTemperature.DisplayString).Append(" ");
		extendedText.AppendLine((ExhaustTemperature > TemperatureKelvin.Zero) ? ExhaustTemperature.ToFloat().ToStringPrefix("K", "yellow") : text);
		extendedText.Append(GameStrings.FlowRate.DisplayString).Append(" ").AppendLine(PassedMoles.ToFloat().ToStringPrefix("mol", "yellow"));
		return extendedText;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (!string.IsNullOrEmpty(passiveTooltip.Title))
		{
			return passiveTooltip;
		}
		PassiveTooltip passiveTooltip2 = new PassiveTooltip(true);
		passiveTooltip2.Title = DisplayName;
		PassiveTooltip result = passiveTooltip2;
		StringBuilder stringBuilder = new StringBuilder();
		result.Extended = stringBuilder.ToString();
		return result;
	}

	public override void OnAddPipeNetwork(PipeNetwork newNetwork)
	{
		if (GameManager.RunSimulation)
		{
			CheckConnections();
		}
	}

	public override void OnRemovePipeNetwork(PipeNetwork oldNetwork)
	{
		if (oldNetwork == _inputNetwork1)
		{
			_inputNetwork1 = null;
		}
		if (oldNetwork == _inputNetwork2)
		{
			_inputNetwork2 = null;
		}
		if (GameManager.RunSimulation)
		{
			CheckConnections();
		}
	}

	protected override void CheckConnections()
	{
		_inputNetwork1 = (_inputConnection1?.GetINetworkedPipe())?.PipeNetwork;
		_inputNetwork2 = (_inputConnection2?.GetINetworkedPipe())?.PipeNetwork;
		_ = IsOperable;
	}

	public override void RebuildGridState()
	{
		base.RebuildGridState();
		_inputConnection1?.SetGrids();
		_inputConnection2?.SetGrids();
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Throttle => true, 
			LogicType.PassedMoles => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Throttle => Throttle, 
			LogicType.PassedMoles => PassedMoles.ToDouble(), 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Throttle)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.Throttle)
		{
			Throttle = (float)value;
		}
		base.SetLogicValue(logicType, value);
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteSingle(Force);
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteSingle(ExhaustVelocity);
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			writer.WriteSingle(ExhaustTemperature.ToFloat());
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			Force = reader.ReadSingle();
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			ExhaustVelocity = reader.ReadSingle();
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			ExhaustTemperature = new TemperatureKelvin(reader.ReadSingle());
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(Force);
		writer.WriteSingle(ExhaustVelocity);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Force = reader.ReadSingle();
		ExhaustVelocity = reader.ReadSingle();
	}

	public override CanConstructInfo CanConstruct()
	{
		Grid3[] array = (Grid3[])GridBounds.GetLocalSmallGrid(base.ThingTransformPosition, base.ThingTransformRotation);
		foreach (Grid3 localGrid in array)
		{
			if (base.GridController.GetSmallCell(localGrid)?.Owner == null)
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.RocketEnginePlacementRule.AsString(ToTooltip()));
			}
		}
		return base.CanConstruct();
	}
}
