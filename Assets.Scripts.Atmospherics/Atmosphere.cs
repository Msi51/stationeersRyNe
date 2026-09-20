using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Networks;
using Objects.Structures;
using Trading;
using UnityEngine;
using Weather;

namespace Assets.Scripts.Atmospherics;

public class Atmosphere : IReferencable, IEvaluable, IDensePoolable
{
	[Flags]
	public enum OutsideFaceFlags : byte
	{
		None = 0,
		Left = 1,
		Right = 2,
		Up = 4,
		Down = 8,
		Front = 0x10,
		Back = 0x20
	}

	private struct MixData
	{
		public readonly Atmosphere atmos;

		public readonly float PressureRatioPreTake;

		public readonly PressurekPa PressureDifferentialPreTake;

		public readonly VolumeLitres GasVolumeCached;

		public float PressureRatioPostTake;

		public bool ConsiderVacuum;

		public float TakeWeight;

		public float GiveWeight;

		public void SetGiveWeight(float value)
		{
			GiveWeight = value;
		}

		public void SetTakeWeight(float value)
		{
			TakeWeight = value;
		}

		public void SetPressureRatioPostTake(float value)
		{
			PressureRatioPostTake = value;
		}

		public MixData(Atmosphere atmos, float pressureRatioPreTake, PressurekPa pressureDifferentialPreTake, bool considerVacuum)
		{
			this.atmos = atmos;
			PressureRatioPreTake = pressureRatioPreTake;
			PressureDifferentialPreTake = pressureDifferentialPreTake;
			GasVolumeCached = atmos.GetGasVolume();
			PressureRatioPostTake = -1f;
			ConsiderVacuum = considerVacuum;
			TakeWeight = -1f;
			GiveWeight = -1f;
		}
	}

	public bool WillSave = true;

	public LiquidRenderState LiquidRenderState;

	public AtmosLifeState AtmosLifeState;

	public int FrameToDestroy;

	private bool _beingDestroyed;

	private DateTime _lastNetworkUpdateTime = DateTime.Now;

	public double NetworkLifeTimeAsSec = 5.0;

	public Cell Cell;

	public AtmosphericsNetwork AtmosphericsNetwork;

	public Thing Thing;

	public Room Room;

	private Vector3 _worldPosition;

	private bool _worldPositionInitialized;

	private bool _sparked;

	private bool _inflamed;

	public bool NeverReset;

	public long ParentThingReferenceId;

	public bool HasLight;

	public float SquareDistanceToPlayer;

	public MoleEnergy CombustionEnergy = MoleEnergy.Zero;

	public GridController ParentGridController;

	public AtmosphericsController AtmosphericsController;

	public bool IsAwaitingEvent;

	public readonly Grid3Buffer OpenNeighbors = new Grid3Buffer();

	public readonly Grid3Buffer ClosedNeighbors = new Grid3Buffer();

	public readonly List<DynamicThing> AllDynamicThings = new List<DynamicThing>();

	private MoleQuantity _totalMolesCached;

	private VolumeLitres _totalVolumeLiquidsCached;

	private VolumeLitres _volume;

	private Vector3 _directionServerCached = Vector3.zero;

	private Vector3 _direction = Vector3.zero;

	public const float LIQUID_ATMOSPHERIC_INTERACTION_THRESHOLD = 0.0001f;

	public WorldGrid RegisteredWorldGrid;

	private float _cleanBurnRate;

	private float fuelBurnedRatio;

	private LiquidFlowDirection _liquidFlow;

	private LiquidParticleDirection _liquidParticleDirection;

	private Vector4 _flowDirection;

	private const float FLOW_DIRECTION_LERP_SPEED = 0.4f;

	private bool _condensation;

	public GasMixture GasMixture = new GasMixture(new MoleQuantity(0.0));

	private int _lastTick = -1;

	private TemperatureKelvin _temperatureCachedClient;

	private VolumeLitres _temporaryVolume;

	private PressurekPa _pressureGassesAndLiquidsCached;

	private PressurekPa _pressureGassesCached;

	private const float CAPACITY_OFFSET_NUMERATOR = 10f;

	private PressurekPa _pressureLiquidCached;

	private const double MINIMUM_GAS_VOLUME_SCALE_WORLD = 100.0;

	private PressurekPa _partialPressureO2Cached;

	private PressurekPa _partialPressureCarbonDioxideCached;

	private PressurekPa _partialPressureMethaneCached;

	private PressurekPa _partialPressureNitrousOxideCached;

	private PressurekPa _partialPressureNitrogenCached;

	private PressurekPa _partialPressurePollutantCached;

	private PressurekPa _partialPressureZrillianToxinsCached;

	private PressurekPa _partialPressureHumanToxinsCached;

	private PressurekPa _partialPressureSteamCached;

	private PressurekPa _partialPressureHydrogenCached;

	private PressurekPa _partialPressureHydrazineCached;

	private PressurekPa _partialPressureHeliumCached;

	private PressurekPa _partialPressureCoolantCached;

	private PressurekPa _partialPressureAcidCached;

	private PressurekPa _partialPressureOzoneCached;

	private OutsideFaceFlags _faceFlags;

	public bool HasPartialFrame;

	private static readonly VolumeLitres OpenNeighbourVolumeThreshold = new VolumeLitres(1.0);

	private bool _isNotInRoomAndTerrained;

	private MixData[] _mixingAtmos = new MixData[7];

	private int _mixingAtmosCount;

	private readonly object _mixingAtmosLock = new object();

	private VolumeLitres _totalMixInWorldVolume;

	private GasMixture _totalMixInWorldGasMix = new GasMixture(new MoleQuantity(0.0));

	private float _totalMixInWorldGiveWeight;

	public PressurekPa MixInWorldStartPressure;

	private int _previousGlobalNeighboursCount;

	public bool SimpleRocketExhaust;

	private const float MINIMUM_RATIO_TO_TAKE = 1.2f;

	private List<Atmosphere> _flatNeighbours = new List<Atmosphere>(4);

	private List<Atmosphere> _verticalNeighbours = new List<Atmosphere>(2);

	private Atmosphere _lowerNeighbour;

	private List<(Atmosphere atmos, float ratio)> _toMix = new List<(Atmosphere, float)>(6);

	private float _mixSpeed = 0.4f;

	private List<(Atmosphere atmos, MoleQuantity amountRemoved)> _removedAmounts = new List<(Atmosphere, MoleQuantity)>(7);

	private GasMixture _liquidsToMix = new GasMixture(MoleQuantity.Zero);

	private List<Atmosphere> _emptyNeighbours = new List<Atmosphere>(6);

	private static readonly double FlowSpeed = 0.1;

	public static readonly double MIN_MOLES_LIQUID_TO_SOLVE = 500.0;

	private const double NOS_CURVE_COEFFICIENT_A = 0.0025;

	private const double NOS_CURVE_COEFFICIENT_B = 1.01;

	private const double OXY_CURVE_COEFFICIENT_A = 0.002;

	private const double OXY_CURVE_COEFFICIENT_B = 1.6;

	private const double MIN_COMBUSTION = 0.05;

	public const float IDEAL_LIQUID_VOLUME_RATIO_FOR_HEAT_EXCHANGE = 0.001f;

	private bool _isCachable;

	public bool ForceNonCachable;

	private const float WORLD_ATMOSPHERE_SURFACE_AREA = 10f;

	public float EnergyRadiated;

	public float SolarEnergy;

	public int LastTickScore = 1;

	private WorldGrid _wGrid;

	public readonly int WorkerJobIndex;

	private MoleEnergy _lastNetworkUpdatedValue;

	private MoleEnergy _lastTickLatentEnergy;

	private static readonly Color NitrogenColor = new Color(0f, 1f, 0.8926389f, 0.5f);

	private static readonly Color OxygenColor = new Color(1f, 1f, 1f, 0.5f);

	private static readonly Color MethaneColor = new Color(1f, 0f, 0f, 0.5f);

	private static readonly Color CarbonDioxideColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);

	private static readonly Color WaterColor = new Color(0.1f, 0.27f, 1f, 0.5f);

	private static readonly Color PollutedWaterColor = new Color(0.540756f, 0.5566037f, 0.3911979f, 0.5f);

	private static readonly Color NitrousOxideColor = new Color(0.748561f, 1f, 0.5990566f, 0.5f);

	private static readonly Color PollutantColor = new Color(1f, 0.9717359f, 0.2333333f, 0.5f);

	private static readonly Color HydrogenColor = new Color(1f, 0.4502047f, 0.7514171f, 0.5f);

	private static readonly Color HydrazineColor = new Color(0.9f, 0.5f, 0.1f, 0.5f);

	private static readonly Color AlcoholColor = new Color(0.9f, 0.8f, 0.5f, 0.5f);

	private static readonly Color HeliumColor = new Color(0.7f, 0.8f, 1f, 0.5f);

	private static readonly Color MoltenSaltColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);

	private static readonly Color SilanolColor = new Color(0f, 1f, 1f, 0.5f);

	private static readonly Color HydrochloricAcidColor = new Color(0.1f, 1f, 0.4f, 0.5f);

	private static readonly Color OzoneColor = new Color(0.6f, 0.8f, 1f, 0.5f);

	private readonly DensePoolReference<Atmosphere> _densePoolReference = new DensePoolReference<Atmosphere>(AtmosphericsManager.AllAtmospheres);

	public string DisplayName => Mode switch
	{
		AtmosphereHelper.AtmosphereMode.World => "WorldAtmosphere: " + StringManager.Get(Grid.x) + ", " + StringManager.Get(Grid.y) + ", " + StringManager.Get(Grid.z), 
		AtmosphereHelper.AtmosphereMode.Network => "PipeAtmosphere", 
		AtmosphereHelper.AtmosphereMode.Thing => "Thing Atmosphere: " + Thing.DisplayName, 
		AtmosphereHelper.AtmosphereMode.Global => "Global Atmosphere", 
		_ => string.Empty, 
	};

	public int FrameRegistered { get; private set; }

	public long ReferenceId { get; set; }

	public ushort NetworkUpdateFlags { get; set; }

	public bool BeingDestroyed
	{
		get
		{
			return _beingDestroyed;
		}
		set
		{
			_beingDestroyed = value;
			if (BeingDestroyed)
			{
				Direction = Vector3.zero;
			}
		}
	}

	public DateTime LastNetworkUpdateTime
	{
		get
		{
			return _lastNetworkUpdateTime;
		}
		set
		{
			_lastNetworkUpdateTime = value.AddSeconds(NetworkLifeTimeAsSec);
		}
	}

	public bool IsGlobalAtmosphere => Mode == AtmosphereHelper.AtmosphereMode.Global;

	public AtmosphereHelper.MatterState AllowedMatterState
	{
		get
		{
			if (Mode == AtmosphereHelper.AtmosphereMode.Network && AtmosphericsNetwork != null)
			{
				return AtmosphericsNetwork.NetworkContentType switch
				{
					Pipe.ContentType.Gas => AtmosphereHelper.MatterState.Gas, 
					Pipe.ContentType.Liquid => AtmosphereHelper.MatterState.Liquid, 
					Pipe.ContentType.Unknown => AtmosphereHelper.MatterState.All, 
					Pipe.ContentType.All => AtmosphereHelper.MatterState.All, 
					_ => AtmosphereHelper.MatterState.All, 
				};
			}
			if (Mode != AtmosphereHelper.AtmosphereMode.World)
			{
				_ = 3;
			}
			return AtmosphereHelper.MatterState.All;
		}
	}

	public Quaternion WorldRotation => Quaternion.identity;

	public AtmosphereHelper.AtmosphereMode Mode { get; set; } = AtmosphereHelper.AtmosphereMode.None;

	public MoleQuantity TotalMoles
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _totalMolesCached;
			}
			return GasMixture.GetTotalMolesGassesAndLiquids;
		}
	}

	public MoleQuantity TotalMolesGases => GasMixture.GetTotalMolesGasses;

	public MoleQuantity TotalMolesLiquids => GasMixture.GetTotalMolesLiquids;

	public double LiquidWorldVolumeScale
	{
		get
		{
			if (Mode != AtmosphereHelper.AtmosphereMode.World)
			{
				return 1.0;
			}
			return GasMixture.InWorldLiquidVolumeMultiplier();
		}
	}

	public VolumeLitres TotalVolumeLiquids
	{
		get
		{
			if (!AtmosphereHelper.CanWriteAccess)
			{
				return _totalVolumeLiquidsCached;
			}
			return GasMixture.VolumeLiquids * LiquidWorldVolumeScale;
		}
	}

	public MoleQuantity TotalInertMoles => GasMixture.TotalInertMoles;

	public VolumeLitres Volume
	{
		get
		{
			return _volume;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(_volume, value))
			{
				NetworkUpdateFlags |= 32;
			}
			_volume = value;
		}
	}

	public Vector3 Direction
	{
		get
		{
			return _direction;
		}
		set
		{
			_direction = ((Mode == AtmosphereHelper.AtmosphereMode.Thing) ? Vector3.zero : value);
		}
	}

	public float CleanBurnRate
	{
		get
		{
			return _cleanBurnRate;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(_cleanBurnRate, value))
			{
				NetworkUpdateFlags |= 16;
			}
			if (float.IsNaN(value))
			{
				value = 0f;
			}
			_cleanBurnRate = value;
		}
	}

	public float FuelBurnedRatio
	{
		get
		{
			return fuelBurnedRatio;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(fuelBurnedRatio, value))
			{
				NetworkUpdateFlags |= 16;
			}
			fuelBurnedRatio = value;
		}
	}

	public int Suppressed { get; set; }

	public LiquidFlowDirection LiquidFlow
	{
		get
		{
			return _liquidFlow;
		}
		set
		{
			if (NetworkManager.IsServer && _liquidFlow != value)
			{
				NetworkUpdateFlags |= 128;
			}
			_liquidFlow = value;
		}
	}

	public LiquidParticleDirection LiquidParticleDirection
	{
		get
		{
			return _liquidParticleDirection;
		}
		set
		{
			if (NetworkManager.IsServer && _liquidParticleDirection != value)
			{
				NetworkUpdateFlags |= 128;
			}
			_liquidParticleDirection = value;
		}
	}

	public bool Inflamed
	{
		get
		{
			return _inflamed;
		}
		set
		{
			if (NetworkManager.IsServer && _inflamed != value)
			{
				NetworkUpdateFlags |= 16;
			}
			bool inflamed = _inflamed;
			_inflamed = value;
			if (Mode == AtmosphereHelper.AtmosphereMode.World && _inflamed != inflamed)
			{
				if (value)
				{
					AtmosphericFire.Register(new AtmosphericFire(this));
				}
				else
				{
					AtmosphericFire.DeRegister(this);
				}
			}
		}
	}

	public bool Condensation
	{
		get
		{
			return _condensation;
		}
		set
		{
			if (NetworkManager.IsServer && _condensation != value)
			{
				NetworkUpdateFlags |= 128;
			}
			bool condensation = _condensation;
			_condensation = value;
			if (Mode == AtmosphereHelper.AtmosphereMode.World && _condensation != condensation)
			{
				if (value)
				{
					AtmosphericFog.Register(new AtmosphericFog(this));
				}
				else
				{
					AtmosphericFog.DeRegister(this);
				}
			}
		}
	}

	public bool Sparked
	{
		get
		{
			if (!_sparked)
			{
				return _inflamed;
			}
			return true;
		}
		set
		{
			if (value)
			{
				_sparked = true;
			}
			else
			{
				_sparked = false;
			}
		}
	}

	public TemperatureKelvin Temperature
	{
		get
		{
			if (!NetworkManager.IsClient || Mode == AtmosphereHelper.AtmosphereMode.Global)
			{
				return GasMixture.Temperature;
			}
			return _temperatureCachedClient;
		}
	}

	public float SolarEnergyReceived { get; private set; }

	public WorldGrid WorldGrid => _wGrid;

	public Grid3 Grid
	{
		get
		{
			switch (Mode)
			{
			case AtmosphereHelper.AtmosphereMode.World:
				return _wGrid.Value;
			case AtmosphereHelper.AtmosphereMode.Network:
				throw new Exception("network atmosphere is requesting world position");
			case AtmosphereHelper.AtmosphereMode.Thing:
				if ((bool)Thing)
				{
					return Thing.WorldGrid.Value;
				}
				break;
			}
			return _wGrid.Value;
		}
	}

	public Vector3 WorldPosition
	{
		get
		{
			switch (Mode)
			{
			case AtmosphereHelper.AtmosphereMode.World:
			case AtmosphereHelper.AtmosphereMode.Network:
				return _worldPosition;
			case AtmosphereHelper.AtmosphereMode.Thing:
				if ((bool)Thing)
				{
					return Thing.CenterPosition;
				}
				break;
			}
			return _worldPosition;
		}
		set
		{
			_worldPosition = value;
			_wGrid = new WorldGrid(_worldPosition);
		}
	}

	public PressurekPa PressureGassesAndLiquids
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _pressureGassesAndLiquidsCached;
			}
			PressurekPa pressurekPa = IdealGas.Pressure(GasMixture.GetTotalMolesGasses, GasMixture.Temperature, GetGasVolume());
			if (TotalVolumeLiquids > Volume - GetMinimumGasVolume(Mode))
			{
				AtmosphereHelper.AtmosphereMode mode = Mode;
				if ((mode == AtmosphereHelper.AtmosphereMode.Thing || mode == AtmosphereHelper.AtmosphereMode.Network || mode == AtmosphereHelper.AtmosphereMode.None) && pressurekPa < LiquidPressureOffset)
				{
					return LiquidPressureOffset;
				}
			}
			return pressurekPa;
		}
	}

	public PressurekPa PressureGasses
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _pressureGassesCached;
			}
			return IdealGas.Pressure(GasMixture.GetTotalMolesGasses, GasMixture.Temperature, GetGasVolume());
		}
	}

	public PressurekPa LiquidPressureOffset
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _pressureLiquidCached;
			}
			if (GasMixture.GetTotalMolesLiquids < Chemistry.MINIMUM_QUANTITY_MOLES)
			{
				return PressurekPa.Zero;
			}
			double num = Math.Clamp((TotalVolumeLiquids / Volume).ToDouble(), 0.0, 1.0);
			double num2 = 0.0;
			num2 = ((!(num >= 1.0)) ? (10.0 / (1.0 - num) - 10.0) : 1013249.9694824219);
			return new PressurekPa(num2);
		}
	}

	public float PressureGassesAndLiquidsInPa => PressureGassesAndLiquids.ToFloat() * 1000f;

	public float LiquidVolumeRatio => (TotalVolumeLiquids / Volume).ToFloat();

	public PressurekPa PartialPressureO2
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _partialPressureO2Cached;
			}
			return IdealGas.Pressure(GasMixture.Oxygen.Quantity, GasMixture.Temperature, GetGasVolume());
		}
	}

	public PressurekPa PartialPressureCarbonDioxide
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _partialPressureCarbonDioxideCached;
			}
			return IdealGas.Pressure(GasMixture.CarbonDioxide.Quantity, GasMixture.Temperature, GetGasVolume());
		}
	}

	public PressurekPa PartialPressureMethane
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _partialPressureMethaneCached;
			}
			return IdealGas.Pressure(GasMixture.Methane.Quantity, GasMixture.Temperature, GetGasVolume());
		}
	}

	public PressurekPa PartialPressureNitrousOxide
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _partialPressureNitrousOxideCached;
			}
			return IdealGas.Pressure(GasMixture.NitrousOxide.Quantity, GasMixture.Temperature, GetGasVolume());
		}
	}

	public PressurekPa PartialPressureNitrogen
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _partialPressureNitrogenCached;
			}
			return IdealGas.Pressure(GasMixture.Nitrogen.Quantity, GasMixture.Temperature, GetGasVolume());
		}
	}

	public PressurekPa PartialPressurePollutant
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _partialPressurePollutantCached;
			}
			return IdealGas.Pressure(GasMixture.Pollutant.Quantity, GasMixture.Temperature, GetGasVolume());
		}
	}

	public PressurekPa PartialPressureZrillianToxins
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _partialPressureZrillianToxinsCached;
			}
			return IdealGas.Pressure(GasMixture.Pollutant.Quantity + GasMixture.Hydrazine.Quantity + GasMixture.Silanol.Quantity + GasMixture.HydrochloricAcid.Quantity, GasMixture.Temperature, GetGasVolume());
		}
	}

	public PressurekPa PartialPressureHumanToxins
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _partialPressureHumanToxinsCached;
			}
			return IdealGas.Pressure(GasMixture.Pollutant.Quantity + GasMixture.Methane.Quantity + GasMixture.Hydrazine.Quantity + GasMixture.Silanol.Quantity + GasMixture.HydrochloricAcid.Quantity, GasMixture.Temperature, GetGasVolume());
		}
	}

	public PressurekPa PartialPressureSteam
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _partialPressureSteamCached;
			}
			return IdealGas.Pressure(GasMixture.Steam.Quantity, GasMixture.Temperature, GetGasVolume());
		}
	}

	public PressurekPa PartialPressureHydrogen
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _partialPressureHydrogenCached;
			}
			return IdealGas.Pressure(GasMixture.Hydrogen.Quantity, GasMixture.Temperature, GetGasVolume());
		}
	}

	public PressurekPa PartialPressureHydrazine
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _partialPressureHydrazineCached;
			}
			return IdealGas.Pressure(GasMixture.Hydrazine.Quantity, GasMixture.Temperature, GetGasVolume());
		}
	}

	public PressurekPa PartialPressureHelium
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _partialPressureHeliumCached;
			}
			return IdealGas.Pressure(GasMixture.Helium.Quantity, GasMixture.Temperature, GetGasVolume());
		}
	}

	public PressurekPa PartialPressureCoolant
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _partialPressureCoolantCached;
			}
			return IdealGas.Pressure(GasMixture.Silanol.Quantity, GasMixture.Temperature, GetGasVolume());
		}
	}

	public PressurekPa PartialPressureAcid
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _partialPressureAcidCached;
			}
			return IdealGas.Pressure(GasMixture.HydrochloricAcid.Quantity, GasMixture.Temperature, GetGasVolume());
		}
	}

	public PressurekPa PartialPressureOzone
	{
		get
		{
			if (IsCachable && !AtmosphereHelper.CanWriteAccess)
			{
				return _partialPressureOzoneCached;
			}
			return IdealGas.Pressure(GasMixture.Ozone.Quantity, GasMixture.Temperature, GetGasVolume());
		}
	}

	public OutsideFaceFlags FaceFlags
	{
		get
		{
			return _faceFlags;
		}
		set
		{
			_faceFlags = value;
		}
	}

	private bool IsRoom => Room != null;

	public LiquidBallSpawnCollection LiquidBallSpawns { get; private set; }

	public float RatioOneAtmosphereUnclamped => RocketMath.Max(RocketMath.Clamp(PressureGassesAndLiquids, PressurekPa.Zero, Chemistry.Limits.MAXPressureGasPipe) / Chemistry.OneAtmosphere, PressurekPa.Zero).ToFloat();

	public bool IsCachable
	{
		get
		{
			return _isCachable;
		}
		set
		{
			_isCachable = value;
			GasMixture.IsCachable = value;
		}
	}

	public MoleEnergy LastTickLatentEnergy
	{
		get
		{
			return _lastTickLatentEnergy;
		}
		set
		{
			if (NetworkManager.IsServer && (int)_lastNetworkUpdatedValue.ToDouble() != (int)value.ToDouble())
			{
				NetworkUpdateFlags |= 128;
				_lastNetworkUpdatedValue = value;
			}
			_lastTickLatentEnergy = value;
		}
	}

	public int DensePoolSlot => _densePoolReference.Slot;

	public void OnAssignedReference()
	{
		FrameRegistered = AtmosphericsManager.Instance.TotalTickCount;
		AtmosLifeState = AtmosLifeState.Active;
	}

	public virtual void PrintDebugInfo(bool verbose = false)
	{
		ConsoleWindow.Print($"Debug information for {DisplayName} with Reference ID: {ReferenceId}");
		ConsoleWindow.Print("Gas Mixture: " + GasMixture);
		ConsoleWindow.Print($"Volume: {Volume}");
		ConsoleWindow.Print($"GasVolume: {GetGasVolume()}");
		ConsoleWindow.Print($"CleanBurnRate: {CleanBurnRate}");
		ConsoleWindow.Print($"Direction: {Direction}");
		if (verbose)
		{
			ConsoleWindow.Print("No Verbose Info");
		}
	}

	public Atmosphere()
	{
	}

	public Atmosphere(AtmosphericsNetwork atmosphericsNetwork, long referenceId = 0L)
	{
		AtmosphericsNetwork = atmosphericsNetwork;
		if (NetworkManager.IsServer)
		{
			NetworkUpdateFlags |= 1;
		}
		Volume = VolumeLitres.Zero;
		Mode = AtmosphereHelper.AtmosphereMode.Network;
		AtmosphericsController = AtmosphericsController.World;
		bool flag = false;
		if (referenceId != 0L)
		{
			flag = Referencable.RegisterAs(this, referenceId);
		}
		if (!flag)
		{
			flag = Referencable.RegisterNew(this);
		}
		if (flag)
		{
			AtmosphericsManager.RegisterFromMainThread(this);
		}
		UpdateCache();
	}

	public Atmosphere(Thing thing, VolumeLitres volume, long referenceId = 0L)
	{
		Thing = thing;
		if (NetworkManager.IsServer)
		{
			NetworkUpdateFlags |= 1;
		}
		Volume = volume;
		Mode = AtmosphereHelper.AtmosphereMode.Thing;
		AtmosphericsController = AtmosphericsController.World;
		if (!thing.IsCursor)
		{
			bool flag = false;
			if (referenceId != 0L)
			{
				flag = Referencable.RegisterAs(this, referenceId);
			}
			if (!flag)
			{
				flag = Referencable.RegisterNew(this);
			}
			if (flag)
			{
				AtmosphericsManager.RegisterFromMainThread(this);
			}
		}
		UpdateCache();
	}

	public Atmosphere(long referenceId, VolumeLitres volume)
	{
		Volume = volume;
		Mode = AtmosphereHelper.AtmosphereMode.Thing;
		AtmosphericsController = AtmosphericsController.World;
		if (referenceId != 0L && Referencable.RegisterAs(this, referenceId))
		{
			AtmosphericsManager.RegisterFromMainThread(this);
		}
		UpdateCache();
	}

	public Atmosphere(WorldGrid worldGrid, long referenceId = 0L)
	{
		Init(worldGrid, referenceId);
		int workerJobIndex = AtmosphereHelper.GetWorkerJobIndex(Grid);
		if (workerJobIndex < 0 || workerJobIndex >= 27)
		{
			throw new IndexOutOfRangeException($"Unable to assign Worker Index to {DisplayName}, ReferenceId: {ReferenceId}. Its world position is out of bounds.");
		}
		WorkerJobIndex = workerJobIndex;
	}

	private void Init(WorldGrid worldGrid, long referenceId)
	{
		ParentGridController = GridController.World;
		AtmosphericsController = ParentGridController.AtmosphericsController;
		_worldPosition = worldGrid.Value.ToVector3();
		_wGrid = worldGrid;
		if (NetworkManager.IsServer)
		{
			NetworkUpdateFlags |= 2;
		}
		Volume = Chemistry.GridVolume;
		Mode = AtmosphereHelper.AtmosphereMode.World;
		Room = GridController.World.RoomController.GetRoom(WorldGrid);
		Cell = GridController.World.GetCell(WorldGrid);
		bool flag = false;
		if (referenceId != 0L)
		{
			flag = Referencable.RegisterAs(this, referenceId);
		}
		if (!flag)
		{
			flag = Referencable.RegisterNew(this);
		}
		if (flag)
		{
			AtmosphericsManager.Register(this);
		}
		CalculateWorldVolume();
		UpdateCache();
		LiquidBallSpawns = new LiquidBallSpawnCollection(this);
	}

	public VolumeLitres GetVolume(AtmosphereHelper.MatterState matterState)
	{
		return matterState switch
		{
			AtmosphereHelper.MatterState.Liquid => Volume, 
			AtmosphereHelper.MatterState.Gas => GetGasVolume(), 
			_ => Volume, 
		};
	}

	public bool IsValid()
	{
		return !(GasMixture.GetTotalMolesGassesAndLiquids <= Chemistry.MINIMUM_VALID_TOTAL_MOLES);
	}

	public bool IsActive()
	{
		return GasMixture.GetTotalMolesGassesAndLiquids > Chemistry.MINIMUM_WORLD_VALID_TOTAL_MOLES;
	}

	public bool IsAboveArmstrong()
	{
		if (!(PressureGassesAndLiquids > Chemistry.ArmstrongLimit))
		{
			if (Mode == AtmosphereHelper.AtmosphereMode.Network)
			{
				return LiquidVolumeRatio > 0.0001f;
			}
			return false;
		}
		return true;
	}

	public bool WillMeltIce()
	{
		if (Mode != AtmosphereHelper.AtmosphereMode.Thing && Mode != AtmosphereHelper.AtmosphereMode.Network)
		{
			return PressureGassesAndLiquids > Chemistry.MeltIcePressureThreshold;
		}
		return true;
	}

	public bool IsLive()
	{
		if (Mode != AtmosphereHelper.AtmosphereMode.World)
		{
			return true;
		}
		if (!IsRoom && OpenNeighbors.Count == 0)
		{
			return false;
		}
		if (IsRoom || !IsCloseToGlobal(new PressurekPa((double)AtmosphereHelper.GlobalAtmosphereNeighbourThreshold / 6.0 * (double)AtmosphereHelper.NewAtmosSupressionMultiplier())) || IsAwaitingEvent)
		{
			return true;
		}
		if (Cell == null)
		{
			return false;
		}
		Structure structure = Cell.Lookup[StructureElement.Center];
		if (structure == null)
		{
			return false;
		}
		return structure.AlwaysInstanceWorldAtmosphere();
	}

	public bool IsValidWorld()
	{
		if (Mode == AtmosphereHelper.AtmosphereMode.World)
		{
			return AtmosphericsController.SampleGlobalAtmosphere(WorldGrid) == this;
		}
		return false;
	}

	public bool IsValidThing()
	{
		if (Mode == AtmosphereHelper.AtmosphereMode.Thing && Thing != null)
		{
			return Thing.ReferenceId > 0;
		}
		return false;
	}

	public bool IsValidNetwork()
	{
		if (Mode == AtmosphereHelper.AtmosphereMode.Network && AtmosphericsNetwork != null)
		{
			return AtmosphericsNetwork.ReferenceId > 0;
		}
		return false;
	}

	public bool IsInvalidWorld()
	{
		if (Mode == AtmosphereHelper.AtmosphereMode.World)
		{
			if (!(RegisteredWorldGrid != WorldGrid))
			{
				return WorldGrid == WorldGrid.INVALID;
			}
			return true;
		}
		return false;
	}

	public bool IsInvalidThing()
	{
		if (Mode == AtmosphereHelper.AtmosphereMode.Thing)
		{
			if (!(Thing == null) && Thing.ReferenceId != 0L)
			{
				return Thing.InternalAtmosphere != this;
			}
			return true;
		}
		return false;
	}

	public bool IsInvalidNetwork()
	{
		if (Mode == AtmosphereHelper.AtmosphereMode.Network)
		{
			if (AtmosphericsNetwork != null && AtmosphericsNetwork.ReferenceId != 0L)
			{
				return AtmosphericsNetwork.Atmosphere != this;
			}
			return true;
		}
		return false;
	}

	private void FlagLiquidParticleDirection(Atmosphere current, Atmosphere neighbour)
	{
		Vector3 normalized = (neighbour.WorldPosition - current.WorldPosition).normalized;
		if (normalized.x > 0f)
		{
			LiquidParticleDirection |= LiquidParticleDirection.Right;
		}
		else if (normalized.x < 0f)
		{
			LiquidParticleDirection |= LiquidParticleDirection.Left;
		}
		else if (normalized.z > 0f)
		{
			LiquidParticleDirection |= LiquidParticleDirection.Forward;
		}
		else if (normalized.z < 0f)
		{
			LiquidParticleDirection |= LiquidParticleDirection.Back;
		}
	}

	private void FlagLiquidFlowDirection(Atmosphere current, Atmosphere neighbour)
	{
		Vector3 normalized = (neighbour.WorldPosition - current.WorldPosition).normalized;
		if (normalized.x > 0f)
		{
			current.LiquidFlow |= LiquidFlowDirection.RightFlowOut;
			neighbour.LiquidFlow |= LiquidFlowDirection.LeftFlowIn;
		}
		else if (normalized.x < 0f)
		{
			current.LiquidFlow |= LiquidFlowDirection.LeftFlowOut;
			neighbour.LiquidFlow |= LiquidFlowDirection.RightFlowIn;
		}
		else if (normalized.z > 0f)
		{
			current.LiquidFlow |= LiquidFlowDirection.ForwardFlowOut;
			neighbour.LiquidFlow |= LiquidFlowDirection.BackFlowIn;
		}
		else if (normalized.z < 0f)
		{
			current.LiquidFlow |= LiquidFlowDirection.BackFlowOut;
			neighbour.LiquidFlow |= LiquidFlowDirection.ForwardFlowIn;
		}
	}

	public void ResetLiquidFlowFlags()
	{
		LiquidFlow = LiquidFlowDirection.None;
	}

	public Vector4 GetSmoothedFlowDirection(float deltaTime)
	{
		_flowDirection = Vector4.MoveTowards(_flowDirection, GetFlowDirection(), deltaTime * 0.4f);
		return _flowDirection;
	}

	public Vector4 GetFlowDirection()
	{
		Vector4 zero = Vector4.zero;
		bool flag = (LiquidFlow & LiquidFlowDirection.LeftFlowOut) != 0;
		bool flag2 = (LiquidFlow & LiquidFlowDirection.RightFlowOut) != 0;
		bool flag3 = (LiquidFlow & LiquidFlowDirection.ForwardFlowOut) != 0;
		bool flag4 = (LiquidFlow & LiquidFlowDirection.BackFlowOut) != 0;
		bool flag5 = (LiquidFlow & LiquidFlowDirection.LeftFlowIn) != 0;
		bool flag6 = (LiquidFlow & LiquidFlowDirection.RightFlowIn) != 0;
		bool flag7 = (LiquidFlow & LiquidFlowDirection.ForwardFlowIn) != 0;
		bool flag8 = (LiquidFlow & LiquidFlowDirection.BackFlowIn) != 0;
		bool flag9 = (LiquidParticleDirection & LiquidParticleDirection.Left) != 0;
		bool flag10 = (LiquidParticleDirection & LiquidParticleDirection.Right) != 0;
		bool flag11 = (LiquidParticleDirection & LiquidParticleDirection.Forward) != 0;
		bool flag12 = (LiquidParticleDirection & LiquidParticleDirection.Back) != 0;
		if (flag && flag5)
		{
			flag = false;
			flag5 = false;
		}
		if (flag2 && flag6)
		{
			flag2 = false;
			flag6 = false;
		}
		if (flag3 && flag7)
		{
			flag3 = false;
			flag7 = false;
		}
		if (flag4 && flag8)
		{
			flag4 = false;
			flag8 = false;
		}
		if (flag || flag6 || flag9)
		{
			zero.x = 1f;
		}
		if (flag2 || flag5 || flag10)
		{
			zero.y = 1f;
		}
		if (flag3 || flag8 || flag11)
		{
			zero.z = 1f;
		}
		if (flag4 || flag7 || flag12)
		{
			zero.w = 1f;
		}
		return zero;
	}

	public void EqualiseInternalEnergy()
	{
		GasMixture.EqualiseInternalEnergy();
	}

	public void CalculateWorldVolume()
	{
		if (Mode == AtmosphereHelper.AtmosphereMode.World && ParentGridController == GridController.World)
		{
			_temporaryVolume = AtmosphereHelper.GetWorldVolume(WorldPosition);
			Volume = RocketMath.Max(new VolumeLitres(1.0), _temporaryVolume);
			UpdateCache();
		}
	}

	private DirtyMoleTolerance UpdateTolerance()
	{
		AtmosphereHelper.AtmosphereMode mode = Mode;
		if (mode != AtmosphereHelper.AtmosphereMode.Thing && mode != AtmosphereHelper.AtmosphereMode.Network && mode != AtmosphereHelper.AtmosphereMode.Global)
		{
			return DirtyMoleTolerance.OnePercent;
		}
		return DirtyMoleTolerance.Precision;
	}

	public void UpdateCache()
	{
		_partialPressureO2Cached = PartialPressureO2;
		_partialPressureNitrousOxideCached = PartialPressureNitrousOxide;
		_partialPressureMethaneCached = PartialPressureMethane;
		_partialPressurePollutantCached = PartialPressurePollutant;
		_partialPressureHumanToxinsCached = PartialPressureHumanToxins;
		_partialPressureZrillianToxinsCached = PartialPressureZrillianToxins;
		_partialPressureNitrogenCached = PartialPressureNitrogen;
		_partialPressureCarbonDioxideCached = PartialPressureCarbonDioxide;
		_partialPressureSteamCached = PartialPressureSteam;
		_partialPressureHydrogenCached = PartialPressureHydrogen;
		_partialPressureHydrazineCached = PartialPressureHydrazine;
		_partialPressureHeliumCached = PartialPressureHelium;
		_partialPressureCoolantCached = PartialPressureCoolant;
		_partialPressureAcidCached = PartialPressureAcid;
		_partialPressureOzoneCached = PartialPressureOzone;
		_pressureGassesCached = PressureGasses;
		_pressureGassesAndLiquidsCached = PressureGassesAndLiquids;
		_pressureLiquidCached = LiquidPressureOffset;
		_totalMolesCached = TotalMoles;
		_totalVolumeLiquidsCached = TotalVolumeLiquids;
		GasMixture.UpdateCache(UpdateTolerance());
		if (Mode == AtmosphereHelper.AtmosphereMode.World && GridController.World.GridCells.TryGetValue(Grid, out var value))
		{
			Cell = value;
		}
		if (Mode == AtmosphereHelper.AtmosphereMode.Thing)
		{
			Direction = Vector3.zero;
		}
		CheckDirtyDirectionServer();
	}

	private void CheckDirtyDirectionServer()
	{
		if (!RocketMath.Approximately(_directionServerCached, Direction, 1f) || (Direction.magnitude < 0.1f && _directionServerCached.magnitude >= 0.1f))
		{
			_directionServerCached = Direction;
			if (NetworkManager.IsServer)
			{
				NetworkUpdateFlags |= 8;
			}
		}
	}

	public void Load(AtmosphereSaveData saveData)
	{
		_worldPosition = saveData.Position;
		_wGrid = new WorldGrid(_worldPosition);
		GasMixture.Reset();
		Volume = new VolumeLitres(saveData.Volume);
		CalculateWorldVolume();
		Direction = saveData.Direction;
		GasMixture.Oxygen = new Mole(Chemistry.GasType.Oxygen, new MoleQuantity(saveData.Oxygen), MoleEnergy.Zero);
		GasMixture.Nitrogen = new Mole(Chemistry.GasType.Nitrogen, new MoleQuantity(saveData.Nitrogen), MoleEnergy.Zero);
		GasMixture.CarbonDioxide = new Mole(Chemistry.GasType.CarbonDioxide, new MoleQuantity(saveData.CarbonDioxide), MoleEnergy.Zero);
		GasMixture.Methane = new Mole(Chemistry.GasType.Methane, new MoleQuantity(saveData.Methane), MoleEnergy.Zero);
		GasMixture.Pollutant = new Mole(Chemistry.GasType.Pollutant, new MoleQuantity(saveData.Chlorine), MoleEnergy.Zero);
		GasMixture.Water = new Mole(Chemistry.GasType.Water, new MoleQuantity(saveData.Water), MoleEnergy.Zero);
		GasMixture.PollutedWater = new Mole(Chemistry.GasType.PollutedWater, new MoleQuantity(saveData.PollutedWater), MoleEnergy.Zero);
		GasMixture.NitrousOxide = new Mole(Chemistry.GasType.NitrousOxide, new MoleQuantity(saveData.NitrousOxide), MoleEnergy.Zero);
		GasMixture.LiquidNitrogen = new Mole(Chemistry.GasType.LiquidNitrogen, new MoleQuantity(saveData.LiquidNitrogen), MoleEnergy.Zero);
		GasMixture.LiquidOxygen = new Mole(Chemistry.GasType.LiquidOxygen, new MoleQuantity(saveData.LiquidOxygen), MoleEnergy.Zero);
		GasMixture.LiquidMethane = new Mole(Chemistry.GasType.LiquidMethane, new MoleQuantity(saveData.LiquidMethane), MoleEnergy.Zero);
		GasMixture.Steam = new Mole(Chemistry.GasType.Steam, new MoleQuantity(saveData.Steam), MoleEnergy.Zero);
		GasMixture.LiquidCarbonDioxide = new Mole(Chemistry.GasType.LiquidCarbonDioxide, new MoleQuantity(saveData.LiquidCarbonDioxide), MoleEnergy.Zero);
		GasMixture.LiquidPollutant = new Mole(Chemistry.GasType.LiquidPollutant, new MoleQuantity(saveData.LiquidPollutant), MoleEnergy.Zero);
		GasMixture.LiquidNitrousOxide = new Mole(Chemistry.GasType.LiquidNitrousOxide, new MoleQuantity(saveData.LiquidNitrousOxide), MoleEnergy.Zero);
		GasMixture.Hydrogen = new Mole(Chemistry.GasType.Hydrogen, new MoleQuantity(saveData.Hydrogen), MoleEnergy.Zero);
		GasMixture.LiquidHydrogen = new Mole(Chemistry.GasType.LiquidHydrogen, new MoleQuantity(saveData.LiquidHydrogen), MoleEnergy.Zero);
		GasMixture.Hydrazine = new Mole(Chemistry.GasType.Hydrazine, new MoleQuantity(saveData.Hydrazine), MoleEnergy.Zero);
		GasMixture.LiquidHydrazine = new Mole(Chemistry.GasType.LiquidHydrazine, new MoleQuantity(saveData.LiquidHydrazine), MoleEnergy.Zero);
		GasMixture.LiquidAlcohol = new Mole(Chemistry.GasType.LiquidAlcohol, new MoleQuantity(saveData.LiquidAlcohol), MoleEnergy.Zero);
		GasMixture.Helium = new Mole(Chemistry.GasType.Helium, new MoleQuantity(saveData.Helium), MoleEnergy.Zero);
		GasMixture.LiquidSodiumChloride = new Mole(Chemistry.GasType.LiquidSodiumChloride, new MoleQuantity(saveData.LiquidSodiumChloride), MoleEnergy.Zero);
		GasMixture.Silanol = new Mole(Chemistry.GasType.Silanol, new MoleQuantity(saveData.Silanol), MoleEnergy.Zero);
		GasMixture.LiquidSilanol = new Mole(Chemistry.GasType.LiquidSilanol, new MoleQuantity(saveData.LiquidSilanol), MoleEnergy.Zero);
		GasMixture.HydrochloricAcid = new Mole(Chemistry.GasType.HydrochloricAcid, new MoleQuantity(saveData.HydrochloricAcid), MoleEnergy.Zero);
		GasMixture.LiquidHydrochloricAcid = new Mole(Chemistry.GasType.LiquidHydrochloricAcid, new MoleQuantity(saveData.LiquidHydrochloricAcid), MoleEnergy.Zero);
		GasMixture.Ozone = new Mole(Chemistry.GasType.Ozone, new MoleQuantity(saveData.Ozone), MoleEnergy.Zero);
		GasMixture.LiquidOzone = new Mole(Chemistry.GasType.LiquidOzone, new MoleQuantity(saveData.LiquidOzone), MoleEnergy.Zero);
		GasMixture.TotalEnergy = new MoleEnergy(saveData.Energy);
		CleanBurnRate = saveData.CleanBurnRate;
		if (saveData.Energy <= 0.0 && GasMixture.GetTotalMolesGassesAndLiquids > MoleQuantity.Zero)
		{
			GasMixture.TotalEnergy = new MoleEnergy(GasMixture.HeatCapacity, Chemistry.Temperature.TwentyDegrees);
		}
		UpdateCache();
	}

	public void Add(GasMixture gasMixture)
	{
		GasMixture.Add(gasMixture);
	}

	public void Add(Mole mole)
	{
		GasMixture.Add(mole);
	}

	public void EnsureWorldPositionFromGrid()
	{
		if (!_worldPositionInitialized)
		{
			_worldPosition = GridController.World.LocalToWorld(Grid);
			_wGrid = new WorldGrid(_worldPosition);
			_worldPositionInitialized = true;
		}
	}

	public PressurekPa PartialPressure(Chemistry.GasType gasType)
	{
		return gasType switch
		{
			Chemistry.GasType.Undefined => PressurekPa.Zero, 
			Chemistry.GasType.Oxygen => PartialPressureO2, 
			Chemistry.GasType.Nitrogen => PartialPressureNitrogen, 
			Chemistry.GasType.CarbonDioxide => PartialPressureCarbonDioxide, 
			Chemistry.GasType.Methane => PartialPressureMethane, 
			Chemistry.GasType.Pollutant => PartialPressurePollutant, 
			Chemistry.GasType.Water => PressurekPa.Zero, 
			Chemistry.GasType.PollutedWater => PressurekPa.Zero, 
			Chemistry.GasType.NitrousOxide => PartialPressureNitrousOxide, 
			Chemistry.GasType.LiquidNitrogen => PressurekPa.Zero, 
			Chemistry.GasType.LiquidOxygen => PressurekPa.Zero, 
			Chemistry.GasType.LiquidMethane => PressurekPa.Zero, 
			Chemistry.GasType.Steam => PartialPressureSteam, 
			Chemistry.GasType.Hydrogen => PartialPressureHydrogen, 
			Chemistry.GasType.LiquidHydrogen => PressurekPa.Zero, 
			Chemistry.GasType.LiquidCarbonDioxide => PressurekPa.Zero, 
			Chemistry.GasType.LiquidPollutant => PressurekPa.Zero, 
			Chemistry.GasType.LiquidNitrousOxide => PressurekPa.Zero, 
			Chemistry.GasType.Hydrazine => PartialPressureHydrazine, 
			Chemistry.GasType.LiquidHydrazine => PressurekPa.Zero, 
			Chemistry.GasType.LiquidAlcohol => PressurekPa.Zero, 
			Chemistry.GasType.Helium => PartialPressureHelium, 
			Chemistry.GasType.LiquidSodiumChloride => PressurekPa.Zero, 
			Chemistry.GasType.Silanol => PartialPressureCoolant, 
			Chemistry.GasType.LiquidSilanol => PressurekPa.Zero, 
			Chemistry.GasType.HydrochloricAcid => PartialPressureAcid, 
			Chemistry.GasType.LiquidHydrochloricAcid => PressurekPa.Zero, 
			Chemistry.GasType.Ozone => PartialPressureOzone, 
			Chemistry.GasType.LiquidOzone => PressurekPa.Zero, 
			_ => throw new ArgumentOutOfRangeException("gasType", gasType, null), 
		};
	}

	public static VolumeLitres GetMinimumGasVolume(AtmosphereHelper.AtmosphereMode mode)
	{
		return Chemistry.MinimumGasVolume * ((mode == AtmosphereHelper.AtmosphereMode.World || mode == AtmosphereHelper.AtmosphereMode.Global) ? 100.0 : 1.0);
	}

	public VolumeLitres GetGasVolume()
	{
		return RocketMath.Max(Volume - TotalVolumeLiquids, GetMinimumGasVolume(Mode));
	}

	public void Mix()
	{
		if (Mode == AtmosphereHelper.AtmosphereMode.World)
		{
			MixInWorld();
			if (LiquidBallSpawns.SpawnInfo.Count > 0)
			{
				LiquidSolver.Instance.QueueSpawns(LiquidBallSpawns);
			}
		}
	}

	public void GetOpenAirNeighbors(Span<Grid3> bufClosed, Span<Grid3> bufOpen)
	{
		if (ParentGridController == null)
		{
			return;
		}
		int count = 0;
		int bufOpenCount = 0;
		GridController.PopulateGridNeighbours(bufClosed, ref count, Grid);
		DoOpenNeighbours(bufClosed, ref count, bufOpen, ref bufOpenCount);
		Grid3Buffer closedNeighbors = ClosedNeighbors;
		Span<Grid3> span = bufClosed;
		closedNeighbors.CopyFrom(span.Slice(0, count));
		lock (OpenNeighbors)
		{
			Grid3Buffer openNeighbors = OpenNeighbors;
			span = bufOpen;
			openNeighbors.CopyFrom(span.Slice(0, bufOpenCount));
		}
	}

	private OutsideFaceFlags GetFaceFlag(Grid3 grid, Grid3 neighbour)
	{
		if (neighbour.x > grid.x)
		{
			return OutsideFaceFlags.Right;
		}
		if (neighbour.x < grid.x)
		{
			return OutsideFaceFlags.Left;
		}
		if (neighbour.y > grid.y)
		{
			return OutsideFaceFlags.Up;
		}
		if (neighbour.y < grid.y)
		{
			return OutsideFaceFlags.Down;
		}
		if (neighbour.z > grid.z)
		{
			return OutsideFaceFlags.Front;
		}
		if (neighbour.z < grid.z)
		{
			return OutsideFaceFlags.Back;
		}
		return OutsideFaceFlags.None;
	}

	private void DoOpenNeighbours(Span<Grid3> bufClosed, ref int bufClosedCount, Span<Grid3> bufOpen, ref int bufOpenCount)
	{
		FaceFlags = OutsideFaceFlags.None;
		Cell cell = Cell;
		if (Volume < OpenNeighbourVolumeThreshold || (cell != null && !cell.IsOpenAir()))
		{
			return;
		}
		Structure structure = ParentGridController.Get<Structure>(WorldGrid);
		bool flag = (HasPartialFrame = structure is Frame && structure.CurrentBuildStateIndex == 1);
		int index = bufClosedCount;
		while (index-- > 0)
		{
			Grid3 grid = bufClosed[index];
			Structure structure2 = ParentGridController.Get<Structure>(new WorldGrid(grid));
			bool flag2 = structure2 is Frame && structure2.CurrentBuildStateIndex == 1;
			Room value;
			if (flag)
			{
				if (!flag2 && !ParentGridController.RoomController.RoomLookup.TryGetValue(grid, out value))
				{
					OutsideFaceFlags faceFlag = GetFaceFlag(Grid, grid);
					FaceFlags |= faceFlag;
				}
			}
			else if (!flag2 && ParentGridController.RoomController.RoomLookup.TryGetValue(Grid, out value) && !ParentGridController.RoomController.RoomLookup.TryGetValue(grid, out value) && (!structure2 || structure2.CanAirPass || structure2.CanLightPass))
			{
				OutsideFaceFlags faceFlag2 = GetFaceFlag(Grid, grid);
				FaceFlags |= faceFlag2;
			}
			if ((bool)structure2 && !structure2.CanAirPass)
			{
				continue;
			}
			if (!AtmosphereHelper.CanContainAtmos(grid))
			{
				OutsideFaceFlags faceFlag3 = GetFaceFlag(Grid, grid);
				if ((FaceFlags & faceFlag3) == faceFlag3)
				{
					FaceFlags &= (OutsideFaceFlags)(byte)(~(int)faceFlag3);
				}
			}
			else if (PlanetaryAtmosphereSimulation.IsInSpaceAtmosphere(Grid) == PlanetaryAtmosphereSimulation.IsInSpaceAtmosphere(grid))
			{
				bufOpen[bufOpenCount++] = grid;
				bufClosedCount--;
			}
		}
		Span<Vector3> voxelBuf = stackalloc Vector3[4];
		int index2 = bufOpenCount;
		while (index2-- > 0)
		{
			Grid3 grid2 = bufOpen[index2];
			Grid3 grid3 = Grid.Middle(grid2);
			OutsideFaceFlags faceFlag4 = GetFaceFlag(Grid, grid2);
			bool flag3 = (FaceFlags & faceFlag4) == faceFlag4;
			Cell cell2 = ParentGridController.GetCell(grid2);
			if (!ParentGridController.IsVoxelFaceOpen(_worldPosition, ParentGridController.LocalToWorld(grid3), voxelBuf) || !ParentGridController.IsVoxelFaceOpen(grid2.ToVector3(), ParentGridController.LocalToWorld(grid3), voxelBuf))
			{
				if (flag3)
				{
					FaceFlags &= (OutsideFaceFlags)(byte)(~(int)faceFlag4);
				}
				bufOpen[index2] = bufOpen[--bufOpenCount];
				bufClosed[bufClosedCount++] = grid2;
			}
			else if (cell != null && !cell.IsOpenAir(grid3))
			{
				bufOpen[index2] = bufOpen[--bufOpenCount];
				bufClosed[bufClosedCount++] = grid2;
			}
			else if (cell2 != null && !cell2.IsOpenAir(grid3))
			{
				bufOpen[index2] = bufOpen[--bufOpenCount];
				bufClosed[bufClosedCount++] = grid2;
			}
		}
	}

	private void TakeAtmospherePortion(Atmosphere atmosphere, double takeScalar, VolumeLitres gasVolume)
	{
		if (atmosphere.IsGlobalAtmosphere)
		{
			GasMixture newGasMix = PlanetaryAtmosphereSimulation.TakeGlobalMoles(PlanetaryAtmosphereSimulation.GetGlobalMoles(AtmosphereHelper.MatterState.Gas) * takeScalar, AtmosphereHelper.MatterState.Gas);
			_totalMixInWorldVolume += gasVolume;
			_totalMixInWorldGasMix.Add(newGasMix);
		}
		else
		{
			_totalMixInWorldVolume += gasVolume;
			atmosphere.GasMixture.RemoveInto(atmosphere.GasMixture.GetTotalMolesGasses * takeScalar, AtmosphereHelper.MatterState.Gas, ref _totalMixInWorldGasMix);
		}
	}

	private void ResetGasMixing()
	{
		_totalMixInWorldVolume = VolumeLitres.Zero;
		_totalMixInWorldGasMix.Reset();
		lock (_mixingAtmosLock)
		{
			_mixingAtmosCount = 0;
		}
		_flatNeighbours.Clear();
		_verticalNeighbours.Clear();
		_lowerNeighbour = null;
		LiquidBallSpawns.Clear();
		LiquidParticleDirection = LiquidParticleDirection.None;
	}

	public bool IsCloseToGlobal(PressurekPa minPressure)
	{
		Atmosphere atmosphere = AtmosphericsController.ReadonlyGlobalAtmosphere(Grid);
		if (atmosphere == null)
		{
			return false;
		}
		double value = (atmosphere.PressureGasses / Chemistry.OneAtmosphere).ToDouble();
		value = Math.Clamp(value, 0.04, 1.0);
		minPressure *= value;
		if (Math.Abs((atmosphere.PressureGasses - PressureGasses).ToDouble()) > minPressure.ToDouble())
		{
			return false;
		}
		double num = (atmosphere.Volume / Volume).ToDouble();
		GasMixture gasMixture = atmosphere.GasMixture;
		return !(0.0 + Math.Abs((gasMixture.Oxygen.Quantity - GasMixture.Oxygen.Quantity * num).ToDouble()) + Math.Abs((gasMixture.Nitrogen.Quantity - GasMixture.Nitrogen.Quantity * num).ToDouble()) + Math.Abs((gasMixture.CarbonDioxide.Quantity - GasMixture.CarbonDioxide.Quantity * num).ToDouble()) + Math.Abs((gasMixture.Methane.Quantity - GasMixture.Methane.Quantity * num).ToDouble()) + Math.Abs((gasMixture.Pollutant.Quantity - GasMixture.Pollutant.Quantity * num).ToDouble()) + Math.Abs((gasMixture.NitrousOxide.Quantity - GasMixture.NitrousOxide.Quantity * num).ToDouble()) + Math.Abs((gasMixture.Steam.Quantity - GasMixture.Steam.Quantity * num).ToDouble()) + Math.Abs((gasMixture.Hydrogen.Quantity - GasMixture.Hydrogen.Quantity * num).ToDouble()) + Math.Abs((gasMixture.Hydrazine.Quantity - GasMixture.Hydrazine.Quantity * num).ToDouble()) + Math.Abs((gasMixture.Helium.Quantity - GasMixture.Helium.Quantity * num).ToDouble()) + Math.Abs((gasMixture.Silanol.Quantity - GasMixture.Silanol.Quantity * num).ToDouble()) + Math.Abs((gasMixture.HydrochloricAcid.Quantity - GasMixture.HydrochloricAcid.Quantity * num).ToDouble()) + Math.Abs((gasMixture.Ozone.Quantity - GasMixture.Ozone.Quantity * num).ToDouble()) > minPressure.ToDouble());
	}

	private void LerpToGlobalAtmosphere()
	{
		float t = AtmosphereHelper.LerpRate();
		GasMixture target = (PlanetaryAtmosphereSimulation.IsInSpaceAtmosphere(WorldGrid) ? GasMixtureHelper.Create() : PlanetaryAtmosphereSimulation.TakeGlobalGasMix(Volume));
		GasMixture.LerpGasses(ref target, t);
		GasMixture.LerpLiquids(ref target, t);
		PlanetaryAtmosphereSimulation.GiveToGlobal(target);
	}

	private void MixInWorld()
	{
		ResetGasMixing();
		Span<Grid3> span = stackalloc Grid3[6];
		int globalNeighborPositionsCount = 0;
		lock (_mixingAtmosLock)
		{
			Direction *= 0.5f;
			GasMixture.TotalEnergy = GasMixture.TotalEnergy;
			if (!IsRoom && Cell == null && _previousGlobalNeighboursCount > 0)
			{
				LerpToGlobalAtmosphere();
			}
			PrepareDataMixInWorld(span, ref globalNeighborPositionsCount);
			if (TotalVolumeLiquids > VolumeLitres.Zero)
			{
				GiveLiquids();
				MixLiquids();
				SpawnLiquidSolverParticles();
				RecachePressureAfterLiquidMix();
			}
			CalculateTakeWeights();
			TakeAtmospheresMixInWorld();
			CalculateGiveWeightsMixInWorld();
			GiveAtmospheresMixInWorld();
			CalculateDirectionVectorsMixInWorld(span, globalNeighborPositionsCount);
		}
		GasMixture.Add(_totalMixInWorldGasMix);
		_previousGlobalNeighboursCount = globalNeighborPositionsCount;
	}

	private void CalculateTakeWeights()
	{
		float takeWeight = 1f / (float)Mathf.Max(OpenNeighbors.Count + 1, 2);
		for (int i = 0; i < _mixingAtmosCount; i++)
		{
			_mixingAtmos[i].TakeWeight = takeWeight;
		}
	}

	private void TakeAtmospheresMixInWorld()
	{
		for (int i = 0; i < _mixingAtmosCount; i++)
		{
			ref MixData reference = ref _mixingAtmos[i];
			if (reference.PressureRatioPreTake > 1.2f || float.IsNaN(reference.PressureRatioPreTake) || (reference.atmos.SimpleRocketExhaust && reference.atmos != this))
			{
				_totalMixInWorldVolume += reference.GasVolumeCached;
			}
			else
			{
				TakeAtmospherePortion(reference.atmos, reference.TakeWeight, reference.GasVolumeCached);
			}
		}
	}

	private void GiveAtmospheresMixInWorld()
	{
		MoleQuantity getTotalMolesGasses = _totalMixInWorldGasMix.GetTotalMolesGasses;
		for (int i = 0; i < _mixingAtmosCount; i++)
		{
			ref MixData reference = ref _mixingAtmos[i];
			float num = reference.GiveWeight / _totalMixInWorldGiveWeight;
			MoleQuantity totalMolesRemoved = getTotalMolesGasses * num;
			if (reference.atmos.IsGlobalAtmosphere)
			{
				PlanetaryAtmosphereSimulation.GiveToGlobal(_totalMixInWorldGasMix.Remove(totalMolesRemoved, AtmosphereHelper.MatterState.Gas));
				continue;
			}
			_totalMixInWorldGasMix.RemoveInto(totalMolesRemoved, AtmosphereHelper.MatterState.Gas, ref reference.atmos.GasMixture);
			if (Inflamed)
			{
				reference.atmos.Sparked = true;
			}
		}
	}

	private void GiveLiquids()
	{
		if (TotalVolumeLiquids <= VolumeLitres.Zero)
		{
			return;
		}
		if (CanGiveLiquidTo(_lowerNeighbour))
		{
			VolumeLitres maxVolumeToMove = RocketMath.Min(RocketMath.Max(VolumeLitres.Zero, _lowerNeighbour.Volume - GetMinimumGasVolume(Mode) - _lowerNeighbour.TotalVolumeLiquids), TotalVolumeLiquids);
			AtmosphereHelper.DrainLiquids(this, _lowerNeighbour, maxVolumeToMove, 1f);
		}
		if (!(TotalVolumeLiquids <= LiquidSolver.RenderThreshold(this)))
		{
			VolumeLitres totalVolumeLiquids = TotalVolumeLiquids;
			EqualizeLiquidsToNeighbours(totalVolumeLiquids, _flatNeighbours, mixToEmpty: false, calculateFlowDirection: true);
			if (TotalVolumeLiquids > Volume - Chemistry.MinimumGasVolume)
			{
				VolumeLitres volumeToDistribute = TotalVolumeLiquids - (Volume - GetMinimumGasVolume(Mode));
				EqualizeLiquidsToNeighbours(volumeToDistribute, _verticalNeighbours, mixToEmpty: true, calculateFlowDirection: false);
			}
		}
	}

	private void EqualizeLiquidsToNeighbours(VolumeLitres volumeToDistribute, List<Atmosphere> neighbours, bool mixToEmpty, bool calculateFlowDirection)
	{
		_toMix.Clear();
		foreach (Atmosphere neighbour in neighbours)
		{
			if (CanGiveLiquidTo(neighbour, mixToEmpty) && !(neighbour.TotalVolumeLiquids <= LiquidSolver.RenderThreshold(neighbour) && calculateFlowDirection) && !(LiquidVolumeRatio <= neighbour.LiquidVolumeRatio))
			{
				float item = 1f - neighbour.LiquidVolumeRatio / LiquidVolumeRatio;
				_toMix.Add((neighbour, item));
				VolumeLitres volumeLitres = TotalVolumeLiquids - neighbour.TotalVolumeLiquids;
				float num = 50f;
				if (calculateFlowDirection && volumeLitres.ToFloat() > num)
				{
					FlagLiquidFlowDirection(this, neighbour);
				}
			}
		}
		VolumeLitres volumeLitres2 = volumeToDistribute / (_toMix.Count + 1);
		bool flag = TotalVolumeLiquids > Volume - Chemistry.MinimumGasVolume;
		foreach (var item2 in _toMix)
		{
			if (flag)
			{
				AtmosphereHelper.MoveLiquidVolume(this, item2.atmos, volumeLitres2 * item2.ratio);
			}
			else
			{
				AtmosphereHelper.DrainLiquids(this, item2.atmos, volumeLitres2 * item2.ratio, 1f);
			}
		}
	}

	private bool CanGiveLiquidTo(Atmosphere atmosphere, bool mixToEmpty = false)
	{
		if (atmosphere == null)
		{
			return false;
		}
		if (!mixToEmpty && atmosphere.TotalMolesLiquids.Equals(MoleQuantity.Zero))
		{
			return false;
		}
		if (atmosphere == this)
		{
			return false;
		}
		if (atmosphere.IsGlobalAtmosphere)
		{
			return false;
		}
		return true;
	}

	private void MixLiquids()
	{
		_removedAmounts.Clear();
		_liquidsToMix.Reset();
		for (int i = 0; i < _mixingAtmosCount; i++)
		{
			ref MixData reference = ref _mixingAtmos[i];
			if (!(reference.atmos.TotalVolumeLiquids <= LiquidSolver.RenderThreshold(reference.atmos)))
			{
				MoleQuantity moleQuantity = reference.atmos.TotalMolesLiquids * _mixSpeed;
				reference.atmos.GasMixture.RemoveInto(moleQuantity, AtmosphereHelper.MatterState.Liquid, ref _liquidsToMix);
				_removedAmounts.Add((reference.atmos, moleQuantity));
			}
		}
		foreach (var removedAmount in _removedAmounts)
		{
			_liquidsToMix.RemoveInto(removedAmount.amountRemoved, AtmosphereHelper.MatterState.Liquid, ref removedAmount.atmos.GasMixture);
		}
	}

	private static VolumeLitres EmptyOfLiquidThreshold(Atmosphere atmos)
	{
		return LiquidSolver.RenderThreshold(atmos);
	}

	private void SpawnLiquidSolverParticles()
	{
		if (GameManager.GameTickPaused || !LiquidSolver.SolverEnabled || LiquidSolver.Instance.CapacityReached || TotalMolesLiquids <= MoleQuantity.Zero)
		{
			return;
		}
		if (_lowerNeighbour != null && !_lowerNeighbour.IsGlobalAtmosphere && _lowerNeighbour.TotalVolumeLiquids < EmptyOfLiquidThreshold(_lowerNeighbour))
		{
			VolumeLitres volumeLitres = RocketMath.Min(RocketMath.Max(VolumeLitres.Zero, _lowerNeighbour.Volume * 0.9900000095367432 - _lowerNeighbour.TotalVolumeLiquids), TotalVolumeLiquids);
			if (volumeLitres > VolumeLitres.Zero)
			{
				GasMixture gasMix = AtmosphereHelper.RemoveLiquidVolume(this, volumeLitres);
				LiquidBallSpawns.Add(gasMix, _lowerNeighbour, this);
			}
		}
		if (TotalMolesLiquids <= new MoleQuantity(MIN_MOLES_LIQUID_TO_SOLVE))
		{
			return;
		}
		_emptyNeighbours.Clear();
		foreach (Atmosphere flatNeighbour in _flatNeighbours)
		{
			if (!flatNeighbour.IsGlobalAtmosphere && flatNeighbour.TotalVolumeLiquids < EmptyOfLiquidThreshold(flatNeighbour))
			{
				_emptyNeighbours.Add(flatNeighbour);
			}
		}
		if (_emptyNeighbours.Count == 0)
		{
			return;
		}
		VolumeLitres volumeToRemove = TotalVolumeLiquids * FlowSpeed / _emptyNeighbours.Count;
		foreach (Atmosphere emptyNeighbour in _emptyNeighbours)
		{
			GasMixture gasMix2 = AtmosphereHelper.RemoveLiquidVolume(this, volumeToRemove);
			LiquidBallSpawns.Add(gasMix2, emptyNeighbour, this);
			FlagLiquidParticleDirection(this, emptyNeighbour);
		}
	}

	private void CalculateGiveWeightsMixInWorld()
	{
		_totalMixInWorldGiveWeight = 0f;
		PressurekPa pressureGasses = PressureGasses;
		for (int i = 0; i < _mixingAtmosCount; i++)
		{
			ref MixData reference = ref _mixingAtmos[i];
			float num = reference.PressureRatioPreTake;
			if (!reference.atmos.IsGlobalAtmosphere)
			{
				num = (pressureGasses / reference.atmos.PressureGasses).ToFloat();
			}
			float num2 = 0f;
			if (reference.atmos.Volume < Chemistry.GridVolume)
			{
				num2 = 1f;
			}
			else if (float.IsNaN(num))
			{
				num2 = 10f;
			}
			else
			{
				num2 = num;
				num2 = Mathf.Clamp(num2, 0.1f, 10f);
			}
			if (SimpleRocketExhaust)
			{
				num2 = ((reference.atmos.WorldPosition - WorldPosition == Vector3.down * 2f) ? 500f : ((reference.atmos != this) ? 0.1f : 500f));
			}
			if (reference.atmos.SimpleRocketExhaust && reference.atmos.WorldPosition - WorldPosition != Vector3.down * 2f)
			{
				num2 = 0.1f;
			}
			num2 *= (reference.GasVolumeCached / _totalMixInWorldVolume).ToFloat();
			reference.PressureRatioPostTake = num;
			reference.GiveWeight = num2;
			_totalMixInWorldGiveWeight += num2;
		}
	}

	private void CalculateDirectionVectorsMixInWorld(ReadOnlySpan<Grid3> globalNeighborPositions, int globalNeighborPositionsCount)
	{
		Atmosphere atmosphere = AtmosphericsController.ReadonlyGlobalAtmosphere(Grid);
		for (int i = 0; i < _mixingAtmosCount; i++)
		{
			ref MixData reference = ref _mixingAtmos[i];
			if (reference.atmos != this && !reference.atmos.IsGlobalAtmosphere)
			{
				Vector3 vector = reference.PressureDifferentialPreTake.ToFloat() * (WorldPosition - reference.atmos.WorldPosition);
				vector *= Mathf.Clamp01((reference.atmos.Volume / Chemistry.GridVolume * Volume / Chemistry.GridVolume).ToFloat());
				float num = Mathf.Max(1f, Mathf.Log(vector.magnitude, 4f));
				float num2 = Mathf.Clamp01(1f / num);
				vector *= num2;
				reference.atmos.Direction += -vector + Direction * 0.1f;
			}
		}
		for (int j = 0; j < globalNeighborPositionsCount; j++)
		{
			Grid3 grid = globalNeighborPositions[j];
			Vector3 vector2 = (atmosphere.PressureGasses - MixInWorldStartPressure).ToFloat() * (grid.ToVector3() - WorldPosition);
			vector2 *= Mathf.Clamp01((Volume / Chemistry.GridVolume).ToFloat());
			Direction += -vector2 + Direction * 0.1f;
		}
		if (LiquidVolumeRatio > 0.99f)
		{
			Direction = Vector3.zero;
		}
	}

	private void RecachePressureAfterLiquidMix()
	{
		MixInWorldStartPressure = PressureGasses;
		for (int i = 0; i < _mixingAtmosCount; i++)
		{
			Atmosphere atmos = _mixingAtmos[i].atmos;
			PressurekPa pressureGasses = atmos.PressureGasses;
			float pressureRatioPreTake = (MixInWorldStartPressure / pressureGasses).ToFloat();
			PressurekPa pressureDifferentialPreTake = MixInWorldStartPressure - pressureGasses;
			bool considerVacuum = pressureGasses <= Chemistry.ResetThreshold;
			_mixingAtmos[i] = new MixData(atmos, pressureRatioPreTake, pressureDifferentialPreTake, considerVacuum);
		}
	}

	private void PrepareDataMixInWorld(Span<Grid3> globalNeighborPositions, ref int globalNeighborPositionsCount)
	{
		MixInWorldStartPressure = PressureGasses;
		bool flag = IsCloseToGlobal(new PressurekPa(AtmosphereHelper.GlobalAtmosphereNeighbourThreshold * AtmosphereHelper.NewAtmosSupressionMultiplier()));
		if (AtmosphericsManager.WorldAtmospheresCount >= 20000)
		{
			flag = true;
		}
		_mixingAtmos[_mixingAtmosCount++] = new MixData(this, 1f, PressurekPa.Zero, considerVacuum: false);
		int count = OpenNeighbors.Count;
		while (count-- > 0)
		{
			Grid3 grid = OpenNeighbors[count];
			WorldGrid worldGrid = new WorldGrid(grid);
			Atmosphere atmosphere;
			if (flag)
			{
				atmosphere = AtmosphericsController.SampleGlobalAtmosphere(worldGrid);
				if (atmosphere != null && atmosphere.IsGlobalAtmosphere)
				{
					globalNeighborPositions[globalNeighborPositionsCount++] = grid;
				}
			}
			else
			{
				atmosphere = AtmosphericsController.CloneGlobalAtmosphere(worldGrid, 0L, setStateActive: false);
			}
			if (atmosphere == null || atmosphere.Mode != AtmosphereHelper.AtmosphereMode.Thing)
			{
				float pressureRatioPreTake = (MixInWorldStartPressure / atmosphere.PressureGasses).ToFloat();
				PressurekPa pressureDifferentialPreTake = MixInWorldStartPressure - atmosphere.PressureGasses;
				bool considerVacuum = atmosphere.PressureGasses <= Chemistry.ResetThreshold;
				lock (_mixingAtmosLock)
				{
					_mixingAtmos[_mixingAtmosCount++] = new MixData(atmosphere, pressureRatioPreTake, pressureDifferentialPreTake, considerVacuum);
				}
				if (grid.y > Grid.y)
				{
					_verticalNeighbours.Add(atmosphere);
				}
				else if (grid.y < Grid.y)
				{
					_lowerNeighbour = atmosphere;
					_verticalNeighbours.Add(atmosphere);
				}
				else
				{
					_flatNeighbours.Add(atmosphere);
				}
			}
		}
	}

	private PressurekPa GetFaceMaxPressureDelta(HashSet<Structure> structures)
	{
		PressurekPa zero = PressurekPa.Zero;
		foreach (Structure structure in structures)
		{
			if ((bool)structure && !structure.CanAirPass && !(structure.MaxPressureDelta <= PressurekPa.Zero))
			{
				zero += structure.MaxPressureDelta;
			}
		}
		return zero;
	}

	public virtual void ReactWithStructures()
	{
		if (Cell == null)
		{
			return;
		}
		Span<Grid3>.Enumerator enumerator = ClosedNeighbors.GetEnumerator();
		while (enumerator.MoveNext())
		{
			Grid3 current = enumerator.Current;
			Cell cell = ParentGridController.GetCell(current);
			if (cell != null && cell.IsBlocked)
			{
				continue;
			}
			WorldGrid grid = new WorldGrid(current);
			Atmosphere atmosphere = AtmosphericsController.SampleGlobalAtmosphere(grid);
			PressurekPa pressurekPa = RocketMath.Abs(PressureGassesAndLiquids - (atmosphere?.PressureGassesAndLiquids ?? PressurekPa.Zero));
			HashSet<Structure> faceStructures = ParentGridController.GetFaceStructures(Grid.Middle(current));
			if (!(GetFaceMaxPressureDelta(faceStructures) < pressurekPa))
			{
				continue;
			}
			foreach (Structure item in faceStructures)
			{
				if (!item || item.CanAirPass || item.MaxPressureDelta <= PressurekPa.Zero)
				{
					continue;
				}
				item.Stressed = pressurekPa > item.MaxPressureDelta * Thing.StressedRatio;
				if (!(item.MaxPressureDelta >= pressurekPa))
				{
					float num = Mathf.Clamp(MathF.Log((pressurekPa / item.MaxPressureDelta).ToFloat(), 2f), 0.5f, 20f);
					float num2 = 1f - (float)AtmosphereHelper._random.NextDouble() * 0.2f;
					num *= num2;
					if (item.DamageState.Total + num >= item.DamageState.MaxDamage && item is Wall wall)
					{
						Wall.PlayWallFailSound(wall);
						Achievements.AchieveABitWindy();
					}
					item.DamageState.Damage(ChangeDamageType.Increment, num, DamageUpdateType.Brute);
				}
			}
		}
	}

	public void CheckStressOnStructuresClient()
	{
		if (Cell == null)
		{
			return;
		}
		Span<Grid3>.Enumerator enumerator = ClosedNeighbors.GetEnumerator();
		while (enumerator.MoveNext())
		{
			Grid3 current = enumerator.Current;
			Cell cell = ParentGridController.GetCell(current);
			if (cell != null && cell.IsBlocked)
			{
				continue;
			}
			WorldGrid grid = new WorldGrid(current);
			Atmosphere atmosphere = AtmosphericsController.SampleGlobalAtmosphere(grid);
			PressurekPa pressurekPa = RocketMath.Abs(PressureGassesAndLiquids - (atmosphere?.PressureGassesAndLiquids ?? PressurekPa.Zero));
			HashSet<Structure> faceStructures = ParentGridController.GetFaceStructures(Grid.Middle(current));
			if (!(GetFaceMaxPressureDelta(faceStructures) < pressurekPa))
			{
				continue;
			}
			foreach (Structure item in faceStructures)
			{
				if (!(item == null) && !item.CanAirPass && !(item.MaxPressureDelta <= PressurekPa.Zero))
				{
					item.Stressed = pressurekPa > item.MaxPressureDelta * Thing.StressedRatio;
				}
			}
		}
	}

	public void SetFlame(float quantityBurnedScale = 0f, float cleanBurnRatio = 0f)
	{
		FuelBurnedRatio = quantityBurnedScale;
		CleanBurnRate = cleanBurnRatio;
		if (Inflamed)
		{
			AtmosphericFire.SetFlameParticleValues(this);
		}
	}

	private float CombustableMix()
	{
		return (float)Math.Clamp((GasMixture.Oxygen.Quantity / GasMixture.Methane.Quantity).ToDouble() * 2.0, 0.0, 1.0);
	}

	public void Cleanup()
	{
		if (!NeverReset)
		{
			if (Mode == AtmosphereHelper.AtmosphereMode.Network && TotalMoles < Chemistry.MINIMUM_VALID_TOTAL_MOLES)
			{
				GasMixture.Reset();
				ResetCombustionData();
			}
			else
			{
				GasMixture.Cleanup();
			}
		}
	}

	public void ResetCombustionData()
	{
		Inflamed = false;
		FuelBurnedRatio = 0f;
		CleanBurnRate = 0f;
		Suppressed = Mathf.Max(0, Suppressed - 1);
	}

	public void TryCombust(double rateOverride = 0.0, bool force = false)
	{
		double combustionRatio = ((rateOverride > 0.0) ? rateOverride : GetCombustionMultiplierCurved());
		bool num = Sparked || force || GasMixture.IsAutoIgnition();
		bool flag = GasMixture.TotalHypergolics < Chemistry.MINIMUM_QUANTITY_MOLES && (GasMixture.TotalOxidiser < Chemistry.MINIMUM_QUANTITY_MOLES || GasMixture.TotalFuel < Chemistry.MINIMUM_QUANTITY_MOLES);
		bool num2 = num && !flag;
		_sparked = false;
		if (!num2 || Suppressed > 0)
		{
			ResetCombustionData();
			return;
		}
		CombustionEnergy = GasMixture.Combust(combustionRatio, out var burnedFuel, out var cleanBurnRatio);
		FuelBurnedRatio = Mathf.Clamp01(burnedFuel.ToFloat() / 10f);
		CleanBurnRate = cleanBurnRatio;
		Inflamed = true;
	}

	public double GetCombustionMultiplierCurved()
	{
		MoleQuantity totalFuel = GasMixture.TotalFuel;
		MoleQuantity totalOxidiser = GasMixture.TotalOxidiser;
		MoleQuantity totalHypergolics = GasMixture.TotalHypergolics;
		MoleQuantity moleQuantity = GasMixture.NitrousOxide.Quantity + GasMixture.LiquidNitrousOxide.Quantity;
		MoleQuantity moleQuantity2 = GasMixture.Ozone.Quantity + GasMixture.LiquidOzone.Quantity;
		double result = 1.0;
		if ((totalFuel < AtmosphereHelper.MinimumMolesForProcessing || totalOxidiser < AtmosphereHelper.MinimumMolesForProcessing) && totalHypergolics < AtmosphereHelper.MinimumMolesForProcessing)
		{
			return result;
		}
		result = ((!((moleQuantity / totalOxidiser).ToDouble() > 0.1) && !((moleQuantity2 / totalOxidiser).ToDouble() > 0.1)) ? (0.05 + 1.0 / Math.Pow(0.002 * (GasMixture.Temperature + Chemistry.Temperature.ZeroDegrees).ToDouble(), 1.6)) : (0.05 + 1.0 / Math.Pow(0.0025 * (GasMixture.Temperature + Chemistry.Temperature.ZeroDegrees).ToDouble(), 1.01)));
		return Math.Clamp(result, 0.0, 1.0) / 5.0;
	}

	public GasMixture Remove(MoleQuantity transferMoles, Chemistry.GasType gasType)
	{
		return new GasMixture(GasMixture.Remove(gasType, transferMoles));
	}

	public GasMixture Remove(MoleQuantity transferMoles, AtmosphereHelper.MatterState matterStateToRemove)
	{
		return GasMixture.Remove(transferMoles, matterStateToRemove);
	}

	public GasMixture RemoveAll()
	{
		return GasMixture.Remove(TotalMoles, AtmosphereHelper.MatterState.All);
	}

	public GasMixture Remove(GasMixture gasMixture, AtmosphereHelper.MatterState matterState)
	{
		return GasMixture.RemoveAndReturn(gasMixture, matterState);
	}

	public float RatioOneAtmosphereClamped()
	{
		return Mathf.Clamp01((PressureGassesAndLiquids / Chemistry.OneAtmosphere).ToFloat());
	}

	public float HeatExchangeRatio()
	{
		float b = Mathf.Clamp01(LiquidVolumeRatio / 0.001f);
		return Mathf.Max(RatioOneAtmosphereClamped(), b);
	}

	public float GetGasTypeRatio(Chemistry.GasType gasType)
	{
		return GasMixture.GetGasTypeRatio(gasType);
	}

	public static TemperatureKelvin CalculateCombinedTotalTemperature(Atmosphere a, Atmosphere b)
	{
		return new TemperatureKelvin((b.Temperature.ToDouble() * b.TotalMoles.ToDouble() + a.Temperature.ToDouble() * a.TotalMoles.ToDouble()) / (a.TotalMoles + b.TotalMoles).ToDouble());
	}

	public void ReactWithCell()
	{
		switch (Mode)
		{
		case AtmosphereHelper.AtmosphereMode.World:
			lock (this)
			{
				if (Room == null && Cell == null)
				{
					MoleEnergy radiatedHeat = AtmosphereHelper.GetRadiatedHeat(this, AtmosphericsController.ReadonlyGlobalAtmosphere(Grid), RatioOneAtmosphereUnclamped * 10f, 0.1f * AtmosphereHelper.NewAtmosSupressionMultiplier());
					if (radiatedHeat < MoleEnergy.Zero)
					{
						MoleEnergy energy2 = RocketMath.Abs(radiatedHeat) * GameManager.GameTickSpeedSeconds;
						GasMixture.AddEnergy(energy2);
						PlanetaryAtmosphereSimulation.RemoveEnergy(energy2);
					}
					else
					{
						PlanetaryAtmosphereSimulation.AddEnergy(GasMixture.RemoveEnergy(radiatedHeat * GameManager.GameTickSpeedSeconds));
					}
					EnergyRadiated = radiatedHeat.ToFloat();
				}
				else if (HasLight)
				{
					MoleEnergy energy3 = new MoleEnergy(LightManager.AirSolarIrradiance * AtmosphericsManager.Instance.TickSpeedSeconds * HeatExchangeRatio());
					GasMixture.AddEnergy(energy3);
					EnergyRadiated = 0f;
					SolarEnergy = energy3.ToFloat();
				}
				else
				{
					EnergyRadiated = 0f;
					SolarEnergy = 0f;
				}
				break;
			}
		case AtmosphereHelper.AtmosphereMode.Network:
			lock (AtmosphericsNetwork.StructureList)
			{
				MoleEnergy zero = MoleEnergy.Zero;
				MoleEnergy zero2 = MoleEnergy.Zero;
				int count = AtmosphericsNetwork.StructureList.Count;
				while (count-- > 0)
				{
					if (!(AtmosphericsNetwork.StructureList[count] is INetworkedAtmospherics networkedAtmospherics))
					{
						continue;
					}
					double scale = 1.0 / (double)((networkedAtmospherics.CurrentGrids.Count <= 0) ? 1 : networkedAtmospherics.CurrentGrids.Count);
					MoleEnergy zero3 = MoleEnergy.Zero;
					MoleEnergy zero4 = MoleEnergy.Zero;
					lock (networkedAtmospherics.CurrentGrids)
					{
						foreach (WorldGrid currentGrid in networkedAtmospherics.CurrentGrids)
						{
							if (networkedAtmospherics.GetAsThing.GridController.CanContainAtmos(currentGrid))
							{
								Thing getAsThing = networkedAtmospherics.GetAsThing;
								Atmosphere worldAtmosphere2 = AtmosphericsController.World.SampleGlobalAtmosphere(currentGrid);
								MoleEnergy moleEnergy7 = AtmosphereHelper.CalculateConvection(worldAtmosphere2, AtmosphericsNetwork.Atmosphere, currentGrid, getAsThing.ConvectionFactor, getAsThing.SurfaceArea, scale);
								zero += moleEnergy7;
								zero3 += moleEnergy7;
								MoleEnergy moleEnergy8 = AtmosphereHelper.CalculateEntropy(worldAtmosphere2, AtmosphericsNetwork.Atmosphere, currentGrid, getAsThing.RadiationFactor, getAsThing.SurfaceArea, scale);
								AtmosphereHelper.DoConvection(AtmosphericsNetwork.Atmosphere, worldAtmosphere2, moleEnergy7, currentGrid);
								MoleEnergy moleEnergy9 = moleEnergy8 - ((moleEnergy7 > MoleEnergy.Zero) ? moleEnergy7 : MoleEnergy.Zero);
								if (moleEnergy9 > MoleEnergy.Zero)
								{
									AtmosphereHelper.DoEntropy(AtmosphericsNetwork.Atmosphere, moleEnergy9);
									zero2 += moleEnergy9;
									zero4 += moleEnergy9;
								}
							}
							else
							{
								MoleEnergy moleEnergy10 = AtmosphereHelper.CalculateThingEntropy(networkedAtmospherics.GetAsThing, null, AtmosphericsNetwork.Atmosphere, scale);
								if (moleEnergy10 > MoleEnergy.Zero)
								{
									AtmosphereHelper.DoEntropy(AtmosphericsNetwork.Atmosphere, moleEnergy10);
									zero2 += moleEnergy10;
									zero4 += moleEnergy10;
								}
							}
						}
					}
					networkedAtmospherics.EnergyRadiated = zero4.ToFloat();
					networkedAtmospherics.EnergyConvected = zero3.ToFloat();
				}
				AtmosphericsNetwork.EnergyConvected = zero.ToFloat();
				AtmosphericsNetwork.EnergyRadiated = zero2.ToFloat();
				break;
			}
		case AtmosphereHelper.AtmosphereMode.Thing:
		{
			if (!Thing)
			{
				break;
			}
			MoleEnergy moleEnergy = MoleEnergy.Zero;
			MoleEnergy moleEnergy2 = MoleEnergy.Zero;
			if (Thing.GridController.CanContainAtmos(Thing.WorldGrid))
			{
				Atmosphere worldAtmosphere = AtmosphericsController.World.SampleGlobalAtmosphere(Thing.WorldGrid);
				MoleEnergy moleEnergy3 = AtmosphereHelper.CalculateThingConvection(Thing, worldAtmosphere, this);
				MoleEnergy moleEnergy4 = AtmosphereHelper.CalculateThingEntropy(Thing, worldAtmosphere, this);
				AtmosphereHelper.DoConvection(Thing.InternalAtmosphere, worldAtmosphere, moleEnergy3, Thing.WorldGrid);
				moleEnergy = moleEnergy3;
				MoleEnergy moleEnergy5 = moleEnergy4 - ((moleEnergy3 > MoleEnergy.Zero) ? moleEnergy3 : MoleEnergy.Zero);
				if (moleEnergy5 > MoleEnergy.Zero)
				{
					AtmosphereHelper.DoEntropy(Thing.InternalAtmosphere, moleEnergy5);
					moleEnergy2 = moleEnergy5;
				}
			}
			else
			{
				MoleEnergy moleEnergy6 = AtmosphereHelper.CalculateThingEntropy(Thing, null, this);
				if (moleEnergy6 > MoleEnergy.Zero)
				{
					AtmosphereHelper.DoEntropy(Thing.InternalAtmosphere, moleEnergy6);
					moleEnergy2 = moleEnergy6;
				}
			}
			float num = 0f;
			if (HasLight)
			{
				float solarRatioAt = WeatherManager.GetSolarRatioAt(WorldPosition.y);
				num = Thing.SurfaceArea / LightManager.GridSurfaceArea * OrbitalSimulation.SolarIrradiance * Thing.SolarHeatingFactor * AtmosphericsManager.Instance.TickSpeedSeconds * HeatExchangeRatio() * LightManager.SolarHeatingCurve(Temperature.ToFloat()) * solarRatioAt;
				MoleEnergy energy = new MoleEnergy(num);
				GasMixture.AddEnergy(energy);
				SolarEnergyReceived = num;
			}
			else
			{
				SolarEnergyReceived = 0f;
			}
			Thing.EnergyConvected = moleEnergy.ToFloat();
			Thing.EnergyRadiated = moleEnergy2.ToFloat() - num;
			break;
		}
		}
	}

	public int WorkScore()
	{
		int num = 1;
		switch (Mode)
		{
		case AtmosphereHelper.AtmosphereMode.World:
			num += OpenNeighbors.Count;
			break;
		case AtmosphereHelper.AtmosphereMode.Network:
		{
			int num2 = ((AtmosphericsNetwork != null) ? AtmosphericsNetwork.StructureList.Count : 0);
			num += num2;
			break;
		}
		case AtmosphereHelper.AtmosphereMode.Thing:
			num += 2;
			break;
		}
		return Mathf.Max(num, LastTickScore);
	}

	public void Read(RocketBinaryReader reader, byte networkUpdateFlags)
	{
		if (AtmosphereHelper.IsNetworkUpdateRequired(16, networkUpdateFlags))
		{
			byte b = reader.ReadByte();
			CleanBurnRate = (float)(int)b / 255f;
			FuelBurnedRatio = reader.ReadSingle();
			Inflamed = reader.ReadBoolean();
		}
		FaceFlags = (OutsideFaceFlags)reader.ReadByte();
		if (AtmosphereHelper.IsNetworkUpdateRequired(32, networkUpdateFlags))
		{
			Volume = new VolumeLitres(reader.ReadUInt32());
		}
		if (AtmosphereHelper.IsNetworkUpdateRequired(128, networkUpdateFlags))
		{
			LiquidFlow = (LiquidFlowDirection)reader.ReadByte();
		}
		if (AtmosphereHelper.IsNetworkUpdateRequired(128, networkUpdateFlags))
		{
			LiquidParticleDirection = (LiquidParticleDirection)reader.ReadByte();
		}
		if (AtmosphereHelper.IsNetworkUpdateRequired(64, networkUpdateFlags))
		{
			uint num = reader.ReadUInt32();
			if ((num & 1) != 0)
			{
				GasMixture.Oxygen.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 2) != 0)
			{
				GasMixture.Nitrogen.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 4) != 0)
			{
				GasMixture.CarbonDioxide.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 8) != 0)
			{
				GasMixture.Methane.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x10) != 0)
			{
				GasMixture.Pollutant.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x20) != 0)
			{
				GasMixture.Water.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x10000) != 0)
			{
				GasMixture.PollutedWater.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x40) != 0)
			{
				GasMixture.NitrousOxide.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x80) != 0)
			{
				GasMixture.LiquidNitrogen.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x100) != 0)
			{
				GasMixture.LiquidOxygen.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x200) != 0)
			{
				GasMixture.LiquidMethane.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x400) != 0)
			{
				GasMixture.Steam.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x800) != 0)
			{
				GasMixture.LiquidCarbonDioxide.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x1000) != 0)
			{
				GasMixture.LiquidPollutant.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x2000) != 0)
			{
				GasMixture.LiquidNitrousOxide.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x4000) != 0)
			{
				GasMixture.Hydrogen.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x8000) != 0)
			{
				GasMixture.LiquidHydrogen.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x20000) != 0)
			{
				GasMixture.Hydrazine.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x40000) != 0)
			{
				GasMixture.LiquidHydrazine.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x80000) != 0)
			{
				GasMixture.LiquidAlcohol.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x100000) != 0)
			{
				GasMixture.Helium.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x200000) != 0)
			{
				GasMixture.LiquidSodiumChloride.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x400000) != 0)
			{
				GasMixture.Silanol.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x800000) != 0)
			{
				GasMixture.LiquidSilanol.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x1000000) != 0)
			{
				GasMixture.HydrochloricAcid.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x2000000) != 0)
			{
				GasMixture.LiquidHydrochloricAcid.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x4000000) != 0)
			{
				GasMixture.Ozone.Quantity = new MoleQuantity(reader.ReadSingle());
			}
			if ((num & 0x8000000) != 0)
			{
				GasMixture.LiquidOzone.Quantity = new MoleQuantity(reader.ReadSingle());
			}
		}
		if (AtmosphereHelper.IsNetworkUpdateRequired(4, networkUpdateFlags))
		{
			_temperatureCachedClient = new TemperatureKelvin(reader.ReadSingle());
			GasMixture.TotalEnergy = IdealGas.Energy(GasMixture.HeatCapacity, _temperatureCachedClient);
		}
		if (AtmosphereHelper.IsNetworkUpdateRequired(128, networkUpdateFlags))
		{
			Condensation = reader.ReadBoolean();
			LastTickLatentEnergy = new MoleEnergy(reader.ReadFloatHalf());
		}
		SetFlame(FuelBurnedRatio, CleanBurnRate);
		LastNetworkUpdateTime = DateTime.Now;
		if (Mode == AtmosphereHelper.AtmosphereMode.Thing && (object)Thing != null)
		{
			Thing.OnAtmosphereClient();
		}
	}

	public uint GassesPresentFlags()
	{
		uint num = 0u;
		if (GasMixture.Oxygen.Quantity > MoleQuantity.Zero)
		{
			num |= 1;
		}
		if (GasMixture.Nitrogen.Quantity > MoleQuantity.Zero)
		{
			num |= 2;
		}
		if (GasMixture.CarbonDioxide.Quantity > MoleQuantity.Zero)
		{
			num |= 4;
		}
		if (GasMixture.Methane.Quantity > MoleQuantity.Zero)
		{
			num |= 8;
		}
		if (GasMixture.Pollutant.Quantity > MoleQuantity.Zero)
		{
			num |= 0x10;
		}
		if (GasMixture.Water.Quantity > MoleQuantity.Zero)
		{
			num |= 0x20;
		}
		if (GasMixture.PollutedWater.Quantity > MoleQuantity.Zero)
		{
			num |= 0x10000;
		}
		if (GasMixture.NitrousOxide.Quantity > MoleQuantity.Zero)
		{
			num |= 0x40;
		}
		if (GasMixture.LiquidNitrogen.Quantity > MoleQuantity.Zero)
		{
			num |= 0x80;
		}
		if (GasMixture.LiquidOxygen.Quantity > MoleQuantity.Zero)
		{
			num |= 0x100;
		}
		if (GasMixture.LiquidMethane.Quantity > MoleQuantity.Zero)
		{
			num |= 0x200;
		}
		if (GasMixture.Steam.Quantity > MoleQuantity.Zero)
		{
			num |= 0x400;
		}
		if (GasMixture.LiquidCarbonDioxide.Quantity > MoleQuantity.Zero)
		{
			num |= 0x800;
		}
		if (GasMixture.LiquidPollutant.Quantity > MoleQuantity.Zero)
		{
			num |= 0x1000;
		}
		if (GasMixture.LiquidNitrousOxide.Quantity > MoleQuantity.Zero)
		{
			num |= 0x2000;
		}
		if (GasMixture.Hydrogen.Quantity > MoleQuantity.Zero)
		{
			num |= 0x4000;
		}
		if (GasMixture.LiquidHydrogen.Quantity > MoleQuantity.Zero)
		{
			num |= 0x8000;
		}
		if (GasMixture.Hydrazine.Quantity > MoleQuantity.Zero)
		{
			num |= 0x20000;
		}
		if (GasMixture.LiquidHydrazine.Quantity > MoleQuantity.Zero)
		{
			num |= 0x40000;
		}
		if (GasMixture.LiquidAlcohol.Quantity > MoleQuantity.Zero)
		{
			num |= 0x80000;
		}
		if (GasMixture.Helium.Quantity > MoleQuantity.Zero)
		{
			num |= 0x100000;
		}
		if (GasMixture.LiquidSodiumChloride.Quantity > MoleQuantity.Zero)
		{
			num |= 0x200000;
		}
		if (GasMixture.Silanol.Quantity > MoleQuantity.Zero)
		{
			num |= 0x400000;
		}
		if (GasMixture.LiquidSilanol.Quantity > MoleQuantity.Zero)
		{
			num |= 0x800000;
		}
		if (GasMixture.HydrochloricAcid.Quantity > MoleQuantity.Zero)
		{
			num |= 0x1000000;
		}
		if (GasMixture.LiquidHydrochloricAcid.Quantity > MoleQuantity.Zero)
		{
			num |= 0x2000000;
		}
		if (GasMixture.Ozone.Quantity > MoleQuantity.Zero)
		{
			num |= 0x4000000;
		}
		if (GasMixture.LiquidOzone.Quantity > MoleQuantity.Zero)
		{
			num |= 0x8000000;
		}
		return num;
	}

	public void Write(RocketBinaryWriter writer, byte networkUpdateFlags, uint quantitiesDirtiedFlag = 0u)
	{
		Network.WritePackedId(writer, this);
		writer.WriteByte(networkUpdateFlags);
		writer.WriteByte((byte)Mode);
		if (AtmosphereHelper.IsNetworkUpdateRequired(1, networkUpdateFlags))
		{
			switch (Mode)
			{
			case AtmosphereHelper.AtmosphereMode.Network:
				Network.WritePackedId(writer, AtmosphericsNetwork);
				break;
			case AtmosphereHelper.AtmosphereMode.Thing:
				Network.WritePackedId(writer, Thing);
				break;
			default:
				Network.WritePackedId(writer, 0L);
				break;
			}
		}
		AtmosphereHelper.AtmosphereMode mode = Mode;
		if (mode == AtmosphereHelper.AtmosphereMode.World || mode == AtmosphereHelper.AtmosphereMode.Thing)
		{
			if (AtmosphereHelper.IsNetworkUpdateRequired(2, networkUpdateFlags))
			{
				writer.WriteWorldGrid(WorldGrid);
			}
			if (AtmosphereHelper.IsNetworkUpdateRequired(8, networkUpdateFlags))
			{
				writer.WriteVector3Half(Direction);
			}
		}
		if (AtmosphereHelper.IsNetworkUpdateRequired(16, networkUpdateFlags))
		{
			writer.WriteByte((byte)Mathf.RoundToInt(CleanBurnRate * 255f));
			writer.WriteSingle(FuelBurnedRatio);
			writer.WriteBoolean(Inflamed);
		}
		writer.WriteByte((byte)FaceFlags);
		if (AtmosphereHelper.IsNetworkUpdateRequired(32, networkUpdateFlags))
		{
			writer.WriteUInt32((uint)Volume.ToFloat());
		}
		if (AtmosphereHelper.IsNetworkUpdateRequired(128, networkUpdateFlags))
		{
			writer.WriteByte((byte)LiquidFlow);
		}
		if (AtmosphereHelper.IsNetworkUpdateRequired(128, networkUpdateFlags))
		{
			writer.WriteByte((byte)LiquidParticleDirection);
		}
		if (AtmosphereHelper.IsNetworkUpdateRequired(64, networkUpdateFlags))
		{
			writer.WriteUInt32(quantitiesDirtiedFlag);
			if ((quantitiesDirtiedFlag & 1) != 0)
			{
				writer.WriteSingle(GasMixture.Oxygen.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 2) != 0)
			{
				writer.WriteSingle(GasMixture.Nitrogen.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 4) != 0)
			{
				writer.WriteSingle(GasMixture.CarbonDioxide.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 8) != 0)
			{
				writer.WriteSingle(GasMixture.Methane.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x10) != 0)
			{
				writer.WriteSingle(GasMixture.Pollutant.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x20) != 0)
			{
				writer.WriteSingle(GasMixture.Water.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x10000) != 0)
			{
				writer.WriteSingle(GasMixture.PollutedWater.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x40) != 0)
			{
				writer.WriteSingle(GasMixture.NitrousOxide.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x80) != 0)
			{
				writer.WriteSingle(GasMixture.LiquidNitrogen.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x100) != 0)
			{
				writer.WriteSingle(GasMixture.LiquidOxygen.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x200) != 0)
			{
				writer.WriteSingle(GasMixture.LiquidMethane.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x400) != 0)
			{
				writer.WriteSingle(GasMixture.Steam.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x800) != 0)
			{
				writer.WriteSingle(GasMixture.LiquidCarbonDioxide.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x1000) != 0)
			{
				writer.WriteSingle(GasMixture.LiquidPollutant.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x2000) != 0)
			{
				writer.WriteSingle(GasMixture.LiquidNitrousOxide.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x4000) != 0)
			{
				writer.WriteSingle(GasMixture.Hydrogen.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x8000) != 0)
			{
				writer.WriteSingle(GasMixture.LiquidHydrogen.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x20000) != 0)
			{
				writer.WriteSingle(GasMixture.Hydrazine.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x40000) != 0)
			{
				writer.WriteSingle(GasMixture.LiquidHydrazine.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x80000) != 0)
			{
				writer.WriteSingle(GasMixture.LiquidAlcohol.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x100000) != 0)
			{
				writer.WriteSingle(GasMixture.Helium.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x200000) != 0)
			{
				writer.WriteSingle(GasMixture.LiquidSodiumChloride.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x400000) != 0)
			{
				writer.WriteSingle(GasMixture.Silanol.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x800000) != 0)
			{
				writer.WriteSingle(GasMixture.LiquidSilanol.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x1000000) != 0)
			{
				writer.WriteSingle(GasMixture.HydrochloricAcid.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x2000000) != 0)
			{
				writer.WriteSingle(GasMixture.LiquidHydrochloricAcid.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x4000000) != 0)
			{
				writer.WriteSingle(GasMixture.Ozone.Quantity.ToFloat());
			}
			if ((quantitiesDirtiedFlag & 0x8000000) != 0)
			{
				writer.WriteSingle(GasMixture.LiquidOzone.Quantity.ToFloat());
			}
		}
		if (AtmosphereHelper.IsNetworkUpdateRequired(4, networkUpdateFlags))
		{
			writer.WriteSingle(Temperature.ToFloat());
		}
		if (AtmosphereHelper.IsNetworkUpdateRequired(128, networkUpdateFlags))
		{
			writer.WriteBoolean(Condensation);
			writer.WriteFloatHalf(LastTickLatentEnergy.ToFloat());
		}
		LastNetworkUpdateTime = DateTime.Now;
	}

	public bool IsNaN()
	{
		return GasMixture.IsNaN();
	}

	public void StateChange()
	{
		if (Mode == AtmosphereHelper.AtmosphereMode.Global || ((object)Thing != null && Thing.PreventStateChange) || (AtmosphericsNetwork != null && AtmosphericsNetwork.PreventStateChange))
		{
			return;
		}
		MoleEnergy totalEnergy = GasMixture.TotalEnergy;
		GasMixture gasMixture = GasMixture.StateChange(AtmosphereHelper.MatterState.All, PressureGasses, GetGasVolume());
		Add(gasMixture);
		LastTickLatentEnergy = GasMixture.TotalEnergy - totalEnergy;
		bool condensation;
		switch (Mode)
		{
		case AtmosphereHelper.AtmosphereMode.World:
			condensation = gasMixture.GetTotalMolesLiquids > GameConstants.MinimumMolesForCondensationEffect;
			break;
		case AtmosphereHelper.AtmosphereMode.Network:
		case AtmosphereHelper.AtmosphereMode.Thing:
			condensation = gasMixture.GetTotalMolesGassesAndLiquids > MoleQuantity.Zero;
			break;
		default:
			condensation = Condensation;
			break;
		}
		Condensation = condensation;
		if (Mode != AtmosphereHelper.AtmosphereMode.World)
		{
			return;
		}
		MoleQuantity moleQuantity = new MoleQuantity(50.0);
		if (Room != null)
		{
			GasMixture gasMixture2 = GasMixture.CheckForFreezing(PressureGasses);
			if (gasMixture2.GetTotalMolesGassesAndLiquids > MoleQuantity.Zero && gasMixture2.GetTotalMolesGassesAndLiquids < moleQuantity)
			{
				lock (Room.FrozenContentsLock)
				{
					GasMixture gasMixture3 = Room.FrozenContents.CheckForIceFormation(PressureGasses, new MoleQuantity(50.0));
					if (gasMixture3.GetTotalMolesGassesAndLiquids <= MoleQuantity.Zero)
					{
						Room.FrozenContents.Add(GasMixture.RemoveAndReturn(gasMixture2, AtmosphereHelper.MatterState.All));
					}
					else
					{
						GasMixture.Add(Room.FrozenContents.RemoveAndReturn(gasMixture3, AtmosphereHelper.MatterState.All));
					}
				}
			}
		}
		GasMixture solidifiedGasses = GasMixture.CheckForIceFormation(PressureGasses, new MoleQuantity(50.0));
		if (solidifiedGasses.GetTotalMolesGassesAndLiquids > MoleQuantity.Zero)
		{
			AtmosphereHelper.FreezeWorldAtmosphere(this, solidifiedGasses);
		}
	}

	public void Extinguish(int suppressedTicks, MoleEnergy energyToRemove)
	{
		Suppressed = suppressedTicks;
		for (int num = OpenNeighbors.Count - 1; num >= 0; num--)
		{
			Atmosphere atmosphereLocal = AtmosphericsController.World.GetAtmosphereLocal(new WorldGrid(OpenNeighbors[num]));
			if (atmosphereLocal != null)
			{
				atmosphereLocal.Suppressed = suppressedTicks;
			}
		}
		if (Temperature > Chemistry.Temperature.TwentyDegrees)
		{
			GasMixture.RemoveEnergy(energyToRemove);
		}
		for (int num2 = AllDynamicThings.Count - 1; num2 >= 0; num2--)
		{
			DynamicThing dynamicThing = AllDynamicThings[num2];
			if (dynamicThing is Entity)
			{
				dynamicThing.ExtinguishSelfAndChildren();
			}
		}
	}

	public void PrepareForWrite()
	{
		if (GasMixture.GasQuantitiesDirtied() != 0)
		{
			NetworkUpdateFlags |= 64;
		}
		if (GasMixture.EnergyDirty())
		{
			NetworkUpdateFlags |= 4;
		}
	}

	public static Color GetLiquidColor(GasMixture gasMixture)
	{
		float num = gasMixture.LiquidNitrogen.Quantity.ToFloat();
		float num2 = gasMixture.LiquidOxygen.Quantity.ToFloat();
		float num3 = gasMixture.LiquidMethane.Quantity.ToFloat();
		float num4 = gasMixture.LiquidCarbonDioxide.Quantity.ToFloat();
		float num5 = gasMixture.Water.Quantity.ToFloat();
		float num6 = gasMixture.PollutedWater.Quantity.ToFloat();
		float num7 = gasMixture.LiquidNitrousOxide.Quantity.ToFloat();
		float num8 = gasMixture.LiquidPollutant.Quantity.ToFloat();
		float num9 = gasMixture.LiquidHydrogen.Quantity.ToFloat();
		float num10 = gasMixture.LiquidHydrazine.Quantity.ToFloat();
		float num11 = gasMixture.LiquidAlcohol.Quantity.ToFloat();
		float num12 = gasMixture.LiquidSodiumChloride.Quantity.ToFloat();
		float num13 = gasMixture.LiquidSilanol.Quantity.ToFloat();
		float num14 = gasMixture.LiquidHydrochloricAcid.Quantity.ToFloat();
		float num15 = gasMixture.LiquidOzone.Quantity.ToFloat();
		float num16 = num + num2 + num3 + num4 + num5 + num6 + num7 + num8 + num10 + num11 + num12 + num13 + num14 + num15;
		float num17 = num / num16;
		float num18 = num2 / num16;
		float num19 = num3 / num16;
		float num20 = num4 / num16;
		float num21 = num5 / num16;
		float num22 = num6 / num16;
		float num23 = num7 / num16;
		float num24 = num8 / num16;
		float num25 = num9 / num16;
		float num26 = num10 / num16;
		float num27 = num11 / num16;
		float num28 = num12 / num16;
		float num29 = num13 / num16;
		float num30 = num14 / num16;
		float num31 = num15 / num16;
		return NitrogenColor * num17 + OxygenColor * num18 + MethaneColor * num19 + CarbonDioxideColor * num20 + WaterColor * num21 + PollutedWaterColor * num22 + NitrousOxideColor * num23 + PollutantColor * num24 + HydrogenColor * num25 + HydrazineColor * num26 + AlcoholColor * num27 + MoltenSaltColor * num28 + SilanolColor * num29 + HydrochloricAcidColor * num30 + OzoneColor * num31;
	}

	public Color GetGasColor()
	{
		float num = GasMixture.Nitrogen.Quantity.ToFloat();
		float num2 = GasMixture.Oxygen.Quantity.ToFloat();
		float num3 = GasMixture.Methane.Quantity.ToFloat();
		float num4 = GasMixture.CarbonDioxide.Quantity.ToFloat();
		float num5 = GasMixture.NitrousOxide.Quantity.ToFloat();
		float num6 = GasMixture.Pollutant.Quantity.ToFloat();
		float num7 = GasMixture.Hydrogen.Quantity.ToFloat();
		float num8 = GasMixture.Steam.Quantity.ToFloat();
		float num9 = GasMixture.Hydrazine.Quantity.ToFloat();
		float num10 = GasMixture.Helium.Quantity.ToFloat();
		float num11 = GasMixture.Silanol.Quantity.ToFloat();
		float num12 = GasMixture.HydrochloricAcid.Quantity.ToFloat();
		float num13 = GasMixture.Ozone.Quantity.ToFloat();
		float num14 = num + num2 + num3 + num4 + num5 + num6 + num7 + num8 + num9 + num10 + num11 + num12 + num13;
		float num15 = num / num14;
		float num16 = num2 / num14;
		float num17 = num3 / num14;
		float num18 = num4 / num14;
		float num19 = num5 / num14;
		float num20 = num6 / num14;
		float num21 = num7 / num14;
		float num22 = num8 / num14;
		float num23 = num9 / num14;
		float num24 = num10 / num14;
		float num25 = num11 / num14;
		float num26 = num12 / num14;
		float num27 = num13 / num14;
		return NitrogenColor * num15 + OxygenColor * num16 + MethaneColor * num17 + CarbonDioxideColor * num18 + PollutantColor * num20 + NitrousOxideColor * num19 + HydrogenColor * num21 + WaterColor * num22 + HydrazineColor * num23 + HeliumColor * num24 + SilanolColor * num25 + HydrochloricAcidColor * num26 + OzoneColor * num27;
	}

	public bool MarkedForRemoval()
	{
		if (BeingDestroyed)
		{
			return false;
		}
		if (!IsLive() && AtmosLifeState == AtmosLifeState.MarkedForRemoval)
		{
			return true;
		}
		return false;
	}

	public bool IsAtmosphereToCleanUp()
	{
		switch (Mode)
		{
		case AtmosphereHelper.AtmosphereMode.World:
			if (IsInvalidWorld())
			{
				return true;
			}
			break;
		case AtmosphereHelper.AtmosphereMode.Network:
			if (IsInvalidNetwork())
			{
				return true;
			}
			break;
		case AtmosphereHelper.AtmosphereMode.Thing:
			if (IsInvalidThing())
			{
				return true;
			}
			break;
		}
		if (Volume.IsDenormalOrNegative())
		{
			return true;
		}
		if (Mode == AtmosphereHelper.AtmosphereMode.World && GridController.World.IsBlockedAirGrid(WorldGrid))
		{
			return true;
		}
		return false;
	}

	public bool OnAddToPool(object densePool, int slot)
	{
		if (_densePoolReference.CanAddToPool(densePool))
		{
			return _densePoolReference.AddToPool(densePool, slot);
		}
		return false;
	}

	public void OnRemoveFromPool(object densePool)
	{
		_densePoolReference.OnRemovedFrom(densePool);
		AtmosphericsManager.Deregister(this);
	}
}
