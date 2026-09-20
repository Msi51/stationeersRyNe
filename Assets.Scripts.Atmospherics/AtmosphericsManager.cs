using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Networks;
using Objects;
using Objects.Rockets;
using UnityEngine;
using Weather;

namespace Assets.Scripts.Atmospherics;

public class AtmosphericsManager : ThreadedManager
{
	public enum ParticleMode
	{
		None,
		Thread,
		Main
	}

	private const int MAX_ATMOS_THINGS = 32768;

	public static readonly DensePool<Thing> AtmosphericThings = new DensePool<Thing>("AtmosphericThings", 32768);

	public static readonly ConcurrentDensePool<Atmosphere> AllAtmospheres = new ConcurrentDensePool<Atmosphere>("AllAtmospheres", 32768);

	private static readonly ConcurrentDictionary<WorldGrid, Atmosphere> AllWorldAtmospheresLookUp = new ConcurrentDictionary<WorldGrid, Atmosphere>(Environment.ProcessorCount, 32768);

	private static readonly object _worldAtmosphereCreationLock = new object();

	public static readonly List<Atmosphere> LiquidAtmospheres = new List<Atmosphere>(32768);

	public static List<long> DeregisteredAtmospheres = new List<long>();

	public static int ThreadId;

	[Header("Atmospherics Manager")]
	public Gradient TemperatureGradient;

	public ParticleSystem DistortionParticleSystem;

	public ParticleSystem ThingFireParticleSystem;

	public ParticleSystem AtmosphereFireParticleSystem;

	public ParticleSystem AtmosphereFogParticleSystem;

	public ParticleSystem PipeLeakParticleSystem;

	public ShowerParticle ShowerParticleSystem;

	public ExtinguisherParticle ExtinguisherParticleSystem;

	public GeyserParticles GeyserParticles;

	[SerializeField]
	private AnimationCurve entropyRadiationCurve;

	public static AtmosphericsManager Instance;

	private Thread _particleThread;

	private const int TEMPORARY_ATMOS_CAPACITY = 1024;

	private static readonly List<Atmosphere> AtmospheresToDeregister = new List<Atmosphere>(1024);

	private static readonly List<Atmosphere> AtmospheresToRegister = new List<Atmosphere>(1024);

	private static readonly Action<Atmosphere> AssignToWorkerAction = delegate(Atmosphere atmosphere)
	{
		if (atmosphere != null && !atmosphere.BeingDestroyed)
		{
			AtmosphericsWorker.Assign(atmosphere);
		}
	};

	private static readonly Action<Thing> ThingAtmosphereTickAction = delegate(Thing thing)
	{
		if ((object)thing != null && !thing.IsBeingDestroyed)
		{
			thing.OnAtmosphericTick();
		}
	};

	private static Action<Thing> _thingFireWorkerAssign = delegate(Thing thing)
	{
		if (thing != null && !thing.IsCursor)
		{
			AtmosphericsWorker.Assign(thing);
		}
	};

	private static readonly Action<PipeNetwork> BeforeAtmosphericsTickAction = delegate(PipeNetwork network)
	{
		network?.BeforeAtmosphericTick();
	};

	private static readonly Action<Atmosphere> ProcessAtmospheresClientAction = delegate(Atmosphere atmosphere)
	{
		if (atmosphere != null && !atmosphere.BeingDestroyed)
		{
			if ((object)atmosphere.Thing == null && atmosphere.ParentThingReferenceId != 0L)
			{
				AtmosphereHelper.TryRelinkThingAtmosphere(atmosphere);
			}
			atmosphere.UpdateCache();
			if ((bool)InventoryManager.Parent)
			{
				atmosphere.SquareDistanceToPlayer = InventoryManager.WorldPosition.DistanceSquared(atmosphere.WorldPosition);
			}
			if (!atmosphere.IsGlobalAtmosphere)
			{
				atmosphere.IsCachable = true;
			}
			if (!atmosphere.IsInvalidWorld() && (atmosphere.GasMixture.GetTotalMolesGassesAndLiquids > MoleQuantity.Zero || atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World))
			{
				atmosphere.CheckStressOnStructuresClient();
			}
		}
	};

	private static WaitForSeconds _waitForTick;

	private static WaitForSeconds _waitForHalfTick;

	private static WaitForEndOfFrame _waitForEndOfFrame;

	private static ParticleSystem.Particle[] _distortionParticles;

	private static readonly TemperatureKelvin HeatDistortionMinTemp = new TemperatureKelvin(800.0);

	private static readonly PressurekPa HeatDistortionMinPressure = new PressurekPa(50.0);

	public static float GasVisualizerMaxRenderDistanceSquared = 400f;

	public static float GasVisualizerMaxSpeed = 5f;

	public static float GasVisualizerLerpRate = 1f;

	public static ParticleSystem.EmitParams emitParams;

	[Header("Atmospheric Particle Visualizer")]
	[ReadOnly]
	public int AtmosphericFireParticleNumber;

	[ReadOnly]
	public int ThingFireParticleNumber;

	[ReadOnly]
	public int AtmosphericFogParticleNumber;

	public bool IsParticleThread = true;

	private static ParticleMode _particleMode = ParticleMode.None;

	private static Atmosphere _particleAtmos;

	private static readonly int MaxEmitters = 50;

	private static readonly Predicate<Atmosphere> AirVisualizerEmitCondition = (Atmosphere atmosphere) => atmosphere != null && atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World && atmosphere.SquareDistanceToPlayer <= GasVisualizerMaxRenderDistanceSquared && atmosphere.Direction.sqrMagnitude > 0.01f;

	private static readonly Predicate<Atmosphere> DistortionEmitCondition = (Atmosphere atmosphere) => atmosphere != null && atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World && atmosphere.SquareDistanceToPlayer <= GasVisualizerMaxRenderDistanceSquared && atmosphere.Temperature > HeatDistortionMinTemp && atmosphere.PressureGasses > HeatDistortionMinPressure;

	private static readonly Predicate<Atmosphere> ThingFireCondition = (Atmosphere atmosphere) => atmosphere != null && atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World;

	private static readonly Action<Atmosphere> AtmosphericProcessingAction = delegate(Atmosphere atmosphere)
	{
		if (atmosphere != null && !atmosphere.BeingDestroyed)
		{
			if (!atmosphere.IsLive())
			{
				atmosphere.SetFlame();
				switch (atmosphere.AtmosLifeState)
				{
				case AtmosLifeState.Passive:
					atmosphere.AtmosLifeState = AtmosLifeState.MarkedForRemoval;
					break;
				case AtmosLifeState.Active:
					atmosphere.AtmosLifeState = AtmosLifeState.Passive;
					break;
				case AtmosLifeState.MarkedForRemoval:
					break;
				}
			}
			else
			{
				atmosphere.SetFlame(atmosphere.FuelBurnedRatio, atmosphere.CleanBurnRate);
				atmosphere.AtmosLifeState = AtmosLifeState.Passive;
			}
		}
	};

	private static readonly Action<Thing> ThingPreAtmosphereAction = delegate(Thing thing)
	{
		if (!(thing == null) && !thing.IsBeingDestroyed)
		{
			if (!thing.HasRunOnAtmospherics)
			{
				thing.OnAtmosphericsBegin();
				thing.HasRunOnAtmospherics = true;
			}
			thing.OnPreAtmosphere();
		}
	};

	private const int MAX_NEIGHBORS = 8;

	private static readonly Func<RocketBinaryWriter, Atmosphere, bool> SerializeOnJoinAction = delegate(RocketBinaryWriter writer, Atmosphere atmos)
	{
		if (atmos == null || atmos.BeingDestroyed)
		{
			return false;
		}
		if (!AtmosphereHelper.IsValidForNetworkSend(atmos))
		{
			return false;
		}
		uint quantitiesDirtiedFlag = atmos.GassesPresentFlags();
		atmos.Write(writer, byte.MaxValue, quantitiesDirtiedFlag);
		return true;
	};

	private int _lastTickSent;

	private static readonly Func<RocketBinaryWriter, Atmosphere, bool> SerialiseDeltaStateAction = delegate(RocketBinaryWriter writer, Atmosphere atmos)
	{
		if (!AtmosphereHelper.IsValidForNetworkSend(atmos))
		{
			return false;
		}
		atmos.PrepareForWrite();
		if (atmos.NetworkUpdateFlags == 0)
		{
			return false;
		}
		ushort networkUpdateFlags = atmos.NetworkUpdateFlags;
		atmos.NetworkUpdateFlags = 0;
		atmos.Write(writer, (byte)networkUpdateFlags, atmos.GasMixture.GasQuantitiesDirtied());
		atmos.GasMixture.UndirtyMoles();
		return true;
	};

	private static readonly Action<Atmosphere> CalculateLiquidAtmosphereActions = delegate(Atmosphere atmosphere)
	{
		if (atmosphere != null && !atmosphere.BeingDestroyed && atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World)
		{
			if (atmosphere.TotalVolumeLiquids <= LiquidSolver.RenderThreshold(atmosphere))
			{
				atmosphere.LiquidRenderState = LiquidRenderState.None;
			}
			else
			{
				atmosphere.LiquidRenderState = atmosphere.LiquidRenderState switch
				{
					LiquidRenderState.None => LiquidRenderState.Unstable, 
					LiquidRenderState.Unstable => LiquidRenderState.Rendered, 
					_ => atmosphere.LiquidRenderState, 
				};
				if (atmosphere.LiquidRenderState == LiquidRenderState.Rendered)
				{
					_workingList.Add(atmosphere);
				}
			}
		}
	};

	private static List<Atmosphere> _workingList = new List<Atmosphere>(1023);

	public static int WorldAtmospheresCount => AllWorldAtmospheresLookUp.Count;

	public static AnimationCurve EntropyCurve => Instance?.entropyRadiationCurve;

	public static bool SendToClients { get; set; }

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		if (Instance == null)
		{
			Instance = this;
		}
		if (!GameManager.IsBatchMode)
		{
			emitParams = new ParticleSystem.EmitParams
			{
				applyShapeToPosition = true
			};
			AtmosphericFire.Initialise(AtmosphereFireParticleSystem);
			ThingFire.Initialise(ThingFireParticleSystem);
			AtmosphericFog.Initialise(AtmosphereFogParticleSystem);
			PipeLeak.Initialise(PipeLeakParticleSystem);
		}
	}

	public override void StartManager()
	{
		base.StartManager();
		ThreadId = WorkingThread.ManagedThreadId;
		if (!GameManager.IsBatchMode)
		{
			IsParticleThread = true;
			_particleThread = new Thread(ParticleThread);
			_particleThread.Priority = Settings.FrameCriticalThreadPriority;
			_particleThread.Start();
		}
	}

	public static Atmosphere Find(WorldGrid worldGrid)
	{
		AllWorldAtmospheresLookUp.TryGetValue(worldGrid, out var value);
		return value;
	}

	public static void DeregisterFromMainThead(Atmosphere atmosphere)
	{
		lock (AtmospheresToDeregister)
		{
			AtmospheresToDeregister.Add(atmosphere);
		}
	}

	public static void RegisterFromMainThread(Atmosphere atmosphere)
	{
		lock (AtmospheresToRegister)
		{
			AtmospheresToRegister.Add(atmosphere);
		}
	}

	public static void HandleMainThreadRegistrations()
	{
		lock (AtmospheresToDeregister)
		{
			for (int num = AtmospheresToDeregister.Count - 1; num >= 0; num--)
			{
				if (AtmospheresToDeregister[num] != null)
				{
					AllAtmospheres.Remove(AtmospheresToDeregister[num]);
				}
			}
			AtmospheresToDeregister.Clear();
		}
		lock (AtmospheresToRegister)
		{
			for (int i = 0; i < AtmospheresToRegister.Count; i++)
			{
				if (AtmospheresToRegister[i] != null)
				{
					Register(AtmospheresToRegister[i]);
				}
			}
			AtmospheresToRegister.Clear();
		}
	}

	public static void Register(Atmosphere atmosphere)
	{
		if (AllAtmospheres.Add(atmosphere))
		{
			atmosphere.RegisteredWorldGrid = atmosphere.WorldGrid;
			if (atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World && !AllWorldAtmospheresLookUp.TryAdd(atmosphere.WorldGrid, atmosphere))
			{
				ConsoleWindow.PrintError("Duplicate world atmosphere registered at " + StringManager.GetGridString(atmosphere.WorldGrid));
			}
		}
	}

	public static void Deregister(Atmosphere atmosphere)
	{
		if (atmosphere == null)
		{
			return;
		}
		atmosphere.BeingDestroyed = true;
		if (atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World)
		{
			PlanetaryAtmosphereSimulation.GiveToGlobal(atmosphere.GasMixture);
			atmosphere.GasMixture.Reset();
			AtmosphericFire.DeRegister(atmosphere);
			AtmosphericFog.DeRegister(atmosphere);
			KeyValuePair<WorldGrid, Atmosphere> item = new KeyValuePair<WorldGrid, Atmosphere>(atmosphere.WorldGrid, atmosphere);
			if (!((ICollection<KeyValuePair<WorldGrid, Atmosphere>>)AllWorldAtmospheresLookUp).Remove(item))
			{
				return;
			}
			atmosphere.RegisteredWorldGrid = WorldGrid.INVALID;
		}
		Referencable.Deregister(atmosphere);
		if (!NetworkManager.IsServer || !NetworkServer.HasClients())
		{
			return;
		}
		lock (DeregisteredAtmospheres)
		{
			DeregisteredAtmospheres.Add(atmosphere.ReferenceId);
		}
	}

	public static void CleanUpAllAtmospheresList()
	{
		AllAtmospheres.Cleanup();
	}

	public static void CleanUpInvalidAtmospheres()
	{
		AllAtmospheres.RemoveWhere((Atmosphere x) => x.IsAtmosphereToCleanUp());
	}

	public static void RunCacheAtmosphereDataJobs()
	{
		AtmosphericsWorker.ClearScores();
		AllAtmospheres.ForEach(AssignToWorkerAction);
		AtmosphericsWorker.Execute(AtmosphericsWorker.Job.CacheData);
		AtmosphericsWorker.WaitForCompletion();
	}

	public static void ThingAtmosphereTick()
	{
		AtmosphericThings.ForEach(ThingAtmosphereTickAction);
	}

	public static void RunThingFireTickJobs()
	{
		AtmosphericsWorker.ClearScores();
		OcclusionManager.AllThings.ForEach(_thingFireWorkerAssign);
		AtmosphericsWorker.Execute(AtmosphericsWorker.Job.ThingFireTick);
		AtmosphericsWorker.WaitForCompletion();
	}

	public static void BeforeAtmosphericsTick()
	{
		PipeNetwork.AllPipeNetworks.ForEach(BeforeAtmosphericsTickAction);
	}

	public static void AtmosphericsNetworksTick()
	{
		AtmosphericsWorker.ClearScores();
		DensePool<PipeNetwork>.ActiveEnumerable.Enumerator enumerator = PipeNetwork.AllPipeNetworks.Active().GetEnumerator();
		while (enumerator.MoveNext())
		{
			AtmosphericsWorker.Assign(enumerator.Current);
		}
		for (int num = LandingPadNetwork.AllLandingPadNetworks.Count - 1; num >= 0; num--)
		{
			AtmosphericsWorker.Assign(LandingPadNetwork.AllLandingPadNetworks[num]);
		}
		AtmosphericsWorker.Execute(AtmosphericsWorker.Job.NetworkTick);
		AtmosphericsWorker.WaitForCompletion();
		AtmosphericsWorker.ClearScores();
	}

	public static void ProcessAtmospheresClient()
	{
		try
		{
			HandleMainThreadRegistrations();
			CleanUpAllAtmospheresList();
			PlanetaryAtmosphereSimulation.TickPlanetarySimulation();
			AtmosphericsNetworksTick();
			AllAtmospheres.ForEach(ProcessAtmospheresClientAction);
			Rocket.RocketAtmosphericsClient();
			StatusUpdates.RefreshAtmosphereValues();
			if (RoomController.RegenStormCurtain)
			{
				RoomController.RegenStormCurtain = false;
				WeatherManager.RegenStormCurtain = true;
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	public static void RegisterBurningThing(Thing thing)
	{
		ThingFire thingFire = new ThingFire(thing);
		if (ThingFire.ThingFireLookup.TryAdd(thing, thingFire))
		{
			ThingFire.Register(thingFire);
		}
	}

	public static void DeregisterBurningThing(Thing thing)
	{
		if (ThingFire.ThingFireLookup.TryRemove(thing, out var value))
		{
			ThingFire.DeRegister(value);
		}
	}

	public void Register(Thing thing)
	{
		Structure structure = thing as Structure;
		if (!(structure != null) || !structure.IsCursor)
		{
			AtmosphericThings.Add(thing);
		}
	}

	public void Deregister(Thing thing)
	{
		AtmosphericThings.Remove(thing);
	}

	private void OnApplicationQuit()
	{
		IsParticleThread = false;
	}

	private void ParticleThread()
	{
		try
		{
			while (IsParticleThread)
			{
				if (_manualReset != null)
				{
					_manualReset.WaitOne();
				}
				if (GameManager.GameState != GameState.Running || InventoryManager.ParentBrain == null)
				{
					Thread.Sleep(1);
					continue;
				}
				AtmosphericsController world = AtmosphericsController.World;
				CalculateParticleAtmospheres(world.ParticlePositions, world.AtmosphereVelocity, world, world.AtmosphericParticleNumber);
				CalculateParticleAtmospheres(AtmosphericFire.FireParticlePositions, AtmosphericFire.FireAtmosphereVelocities, AtmosphericFireParticleNumber);
				CalculateParticleAtmospheres(ThingFire.ThingParticlePositions, ThingFire.ThingAtmosphereVelocities, ThingFireParticleNumber);
				CalculateParticleAtmospheres(AtmosphericFog.FogParticlePositions, AtmosphericFog.FogAtmosphereVelocities, AtmosphericFogParticleNumber);
				Thread.Sleep(1);
			}
			_particleThread = null;
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
	}

	private void FixedUpdate()
	{
		if (GameManager.GameState == GameState.Running && !(InventoryManager.ParentBrain == null) && !WorldManager.IsGamePaused)
		{
			if (_distortionParticles == null)
			{
				_distortionParticles = new ParticleSystem.Particle[DistortionParticleSystem.main.maxParticles];
			}
			AtmosphericsController world = AtmosphericsController.World;
			for (int i = 0; i < 50; i++)
			{
				EmitAirVisualizerParticles();
			}
			if (world.GasVisualizerParticleSystem != null)
			{
				SetParticlePositions(out world.AtmosphericParticleNumber, world.GasVisualizerParticleSystem, world._particles, world.ParticlePositions, world.AtmosphereVelocity);
			}
			for (int j = 0; j < 20; j++)
			{
				EmitDistortionParticles();
			}
			AtmosphericFire.EmitAtmosphericFireParticles();
			AtmosphericFog.EmitAtmosphericFogParticles();
			ThingFire.EmitThingFireParticles();
			PipeLeak.Emit();
			SetParticlePositions(out AtmosphericFireParticleNumber, AtmosphereFireParticleSystem, AtmosphericFire.AtmosphereFireParticles, AtmosphericFire.FireParticlePositions, AtmosphericFire.FireAtmosphereVelocities);
			SetParticlePositions(out AtmosphericFogParticleNumber, AtmosphereFogParticleSystem, AtmosphericFog.AtmosphereFogParticles, AtmosphericFog.FogParticlePositions, AtmosphericFog.FogAtmosphereVelocities);
			EmitThing();
			SetParticlePositions(out ThingFireParticleNumber, ThingFireParticleSystem, ThingFire.ThingFireParticles, ThingFire.ThingParticlePositions, ThingFire.ThingAtmosphereVelocities);
		}
	}

	private static void SetParticlePositions(out int particleCount, ParticleSystem emitterParticleSystem, ParticleSystem.Particle[] particleArray, Vector3[] particlePositions, Vector3[] atmosphereVelocity)
	{
		_particleMode = ParticleMode.Main;
		particleCount = emitterParticleSystem.GetParticles(particleArray);
		for (int i = 0; i < particleCount; i++)
		{
			particlePositions[i] = particleArray[i].position;
			if (float.IsNaN(atmosphereVelocity[i].x))
			{
				particleArray[i].remainingLifetime = Mathf.Lerp(particleArray[i].remainingLifetime, 0f, Time.fixedDeltaTime * GasVisualizerLerpRate);
				particleArray[i].velocity = Vector3.Lerp(particleArray[i].velocity, Vector3.zero, Time.fixedDeltaTime * GasVisualizerLerpRate);
			}
			else
			{
				particleArray[i].velocity = Vector3.Lerp(particleArray[i].velocity, Vector3.ClampMagnitude(atmosphereVelocity[i], GasVisualizerMaxSpeed), Time.deltaTime * GasVisualizerLerpRate);
			}
		}
		emitterParticleSystem.SetParticles(particleArray, particleCount);
		_particleMode = ParticleMode.None;
	}

	private static void CalculateParticleAtmospheres(Vector3[] particlePositions, Vector3[] atmosphereVelocities, AtmosphericsController atmosController, int particleCount)
	{
		if (particlePositions == null || atmosphereVelocities == null)
		{
			return;
		}
		Quaternion identity = Quaternion.identity;
		_particleMode = ParticleMode.Thread;
		for (int i = 0; i < particleCount; i++)
		{
			Grid3 grid = particlePositions[i].GridCenter().ToGrid();
			Atmosphere atmosphere = atmosController.SampleGlobalAtmosphere(new WorldGrid(particlePositions[i]));
			if (atmosphere != null)
			{
				atmosphereVelocities[i] = identity * atmosphere.Direction;
				Grid3 grid2 = (particlePositions[i] + atmosphereVelocities[i] * GameManager.DeltaTime).ToGrid();
				if (grid != grid2 && !atmosphere.OpenNeighbors.Contains(grid2))
				{
					atmosphereVelocities[i].x = float.NaN;
				}
			}
			else
			{
				atmosphereVelocities[i].x = float.NaN;
			}
		}
		_particleMode = ParticleMode.None;
	}

	private static void CalculateParticleAtmospheres(Vector3[] particlePositions, Vector3[] atmosphereVelocities, int particleCount)
	{
		if (particlePositions == null || atmosphereVelocities == null)
		{
			return;
		}
		_particleMode = ParticleMode.Thread;
		for (int i = 0; i < particleCount; i++)
		{
			Atmosphere atmosphere = AtmosphericsController.World.SampleGlobalAtmosphere(new WorldGrid(particlePositions[i]));
			if (atmosphere != null)
			{
				atmosphereVelocities[i] = atmosphere.Direction;
			}
			else
			{
				atmosphereVelocities[i].x = float.NaN;
			}
		}
		_particleMode = ParticleMode.None;
	}

	private static void Emit(DensePool<Atmosphere> targetContainer, ParticleSystem emitter, Vector3 particleAtmosphereSpawnOffset, Predicate<Atmosphere> emitCondition, bool localSpace = false)
	{
		if (targetContainer.ActiveCount <= 0)
		{
			return;
		}
		int num = 0;
		bool flag = false;
		while (num < MaxEmitters && !flag)
		{
			try
			{
				Atmosphere atmosphere = null;
				atmosphere = targetContainer.Pick();
				num++;
				if (atmosphere == null || atmosphere.BeingDestroyed || atmosphere.Mode != AtmosphereHelper.AtmosphereMode.World || !emitCondition(atmosphere))
				{
					break;
				}
				if (localSpace)
				{
					emitParams.position = particleAtmosphereSpawnOffset + atmosphere.Grid.ToVector3();
				}
				else
				{
					emitParams.position = particleAtmosphereSpawnOffset + atmosphere.ParentGridController.LocalToWorld(atmosphere.Grid);
				}
				emitter.Emit(emitParams, 1);
				flag = true;
			}
			catch (ArgumentOutOfRangeException)
			{
			}
			catch (IndexOutOfRangeException)
			{
			}
		}
	}

	private static void EmitAirVisualizerParticles()
	{
		Emit(AllAtmospheres, AtmosphericsController.World.GasVisualizerParticleSystem, UnityEngine.Random.insideUnitSphere, AirVisualizerEmitCondition, localSpace: true);
	}

	private void EmitDistortionParticles()
	{
		Emit(AllAtmospheres, DistortionParticleSystem, Vector3.zero, DistortionEmitCondition);
	}

	private void EmitThing()
	{
	}

	public static void AtmosphericProcessing()
	{
		AllAtmospheres.ForEach(AtmosphericProcessingAction);
	}

	public static void ProcessMarkedForRemoval()
	{
		AllAtmospheres.RemoveWhere((Atmosphere x) => x.MarkedForRemoval());
	}

	public static void PlantLifeTick(float lastTickTime)
	{
		for (int num = Plant.AllPlants.Count - 1; num >= 0; num--)
		{
			if (!(Plant.AllPlants[num] == null))
			{
				Plant.AllPlants[num].OnLifeTick(lastTickTime);
			}
		}
	}

	public static void ThingPreAtmosphere()
	{
		AtmosphericThings.ForEach(ThingPreAtmosphereAction);
	}

	public static void LifeTicksTick()
	{
		for (int num = Entity.AllEntities.Count - 1; num >= 0; num--)
		{
			if ((bool)Entity.AllEntities[num])
			{
				Entity.AllEntities[num].OnLifeTick();
			}
		}
		if (!GameManager.IsBatchMode)
		{
			StatusUpdates.RefreshAtmosphereValues();
		}
	}

	public static Atmosphere CloneGlobalAtmosphereThreadSafe(WorldGrid worldGrid)
	{
		Atmosphere atmosphere = Find(worldGrid);
		if (atmosphere != null)
		{
			atmosphere.AtmosLifeState = AtmosLifeState.Active;
			return atmosphere;
		}
		lock (_worldAtmosphereCreationLock)
		{
			return AtmosphericsController.World.CloneGlobalAtmosphere(worldGrid, 0L);
		}
	}

	public static void DisplayGas(StringBuilder stringBuilder, GasMixture gasMixture, Mole mole, bool hideIfZero = true, bool indent = false)
	{
		if (!(mole.Quantity <= MoleQuantity.Zero && hideIfZero))
		{
			if (indent)
			{
				stringBuilder.Append(StringManager.Indent);
			}
			stringBuilder.Append(mole.DisplayName.AsColor("#44AD83"));
			stringBuilder.Append(" ");
			stringBuilder.Append((mole.Quantity / gasMixture.GetTotalMolesGassesAndLiquids * 100.0).ToFloat().ToStringPercent("yellow"));
			stringBuilder.Append(" ");
			stringBuilder.Append(mole.Quantity.ToFloat().ToStringPrefix("mol"));
			stringBuilder.AppendLine();
		}
	}

	public static void DisplayGas(StringBuilder stringBuilder, SpawnGas spawnGas, float scale = 1f)
	{
		if (!(spawnGas.Quantity <= float.Epsilon))
		{
			stringBuilder.Append(EnumCollections.GasTypesProper.GetName(spawnGas.Type).AsColor("#44AD83"));
			stringBuilder.Append(" ");
			stringBuilder.Append((spawnGas.Quantity * scale).ToStringPrefix("mol", "yellow"));
			stringBuilder.AppendLine();
		}
	}

	public static int CountIrradiatingGrids(WorldGrid grid)
	{
		int num = 0;
		if (AtmosphericsController.World.GetAtmosphereLocal(grid) == null && GridController.World.GetCell(grid) == null)
		{
			num++;
		}
		Span<WorldGrid> obj = stackalloc WorldGrid[8];
		int count = 0;
		RocketGrid.PopulateGridNeighbours(obj, ref count, grid);
		Span<WorldGrid> span = obj;
		Span<WorldGrid> span2 = span.Slice(0, count);
		for (int i = 0; i < span2.Length; i++)
		{
			WorldGrid localGrid = span2[i];
			if (AtmosphericsController.World.GetAtmosphereLocal(grid) == null && GridController.World.GetCell(localGrid) == null)
			{
				num++;
			}
		}
		return num;
	}

	public static void ClearAll()
	{
		AllAtmospheres.Clear();
		AllWorldAtmospheresLookUp.Clear();
		ThingFire.Clear();
		AtmosphericFire.Clear();
		AtmosphericFog.Clear();
		PipeLeak.Clear();
		AtmosphericThings.Clear();
		Plant.AllPlants.Clear();
		Entity.AllEntities.Clear();
	}

	public static void SerialiseOnJoin(RocketBinaryWriter writer)
	{
		Network.WriteIndex<uint>(writer, out var count, out var bufferIndex);
		AllAtmospheres.ForEach(writer, SerializeOnJoinAction, ref count);
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public static async UniTask DeserializeOnJoin(RocketBinaryReader reader)
	{
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenDeserializingAtmospherics.DisplayString);
		Network.ReadIndex<uint>(reader, out var count);
		for (int i = 0; i < count; i++)
		{
			AtmosphereHelper.ReadStatic(reader);
			await ImGuiLoadingScreen.SetProgress((float)i / (float)count);
		}
		ProcessAtmospheresClient();
	}

	public static void SerialiseDeltaState(RocketBinaryWriter writer)
	{
		lock (DeregisteredAtmospheres)
		{
			Network.WriteIndex<ushort>(writer, out var count, out var bufferIndex);
			foreach (long deregisteredAtmosphere in DeregisteredAtmospheres)
			{
				Network.WritePackedId(writer, deregisteredAtmosphere);
				count++;
			}
			Network.WriteIndex(writer, count, bufferIndex);
			DeregisteredAtmospheres.Clear();
		}
		Network.WriteIndex<ushort>(writer, out var count2, out var bufferIndex2);
		AllAtmospheres.ForEach(writer, SerialiseDeltaStateAction, ref count2);
		Network.WriteIndex(writer, count2, bufferIndex2);
	}

	public static void DeserialiseDeltaState(RocketBinaryReader reader)
	{
		Network.ReadIndex<ushort>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			Network.ReadPackedId(reader, out var referenceId);
			Atmosphere atmosphere = Referencable.Find<Atmosphere>(referenceId);
			if (atmosphere != null)
			{
				AllAtmospheres.Remove(atmosphere);
			}
		}
		Network.ReadIndex<ushort>(reader, out var value2);
		for (int j = 0; j < value2; j++)
		{
			AtmosphereHelper.ReadStatic(reader);
		}
		ProcessAtmospheresClient();
	}

	public static void MakeGasTooltip(GasMixture gasMixture, StringBuilder stringBuilder, bool indent = false)
	{
		DisplayGas(stringBuilder, gasMixture, gasMixture.Oxygen, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.Nitrogen, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.CarbonDioxide, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.Methane, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.Pollutant, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.NitrousOxide, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.Steam, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.Hydrogen, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.LiquidNitrogen, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.LiquidOxygen, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.LiquidMethane, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.LiquidCarbonDioxide, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.LiquidPollutant, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.LiquidNitrousOxide, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.Water, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.PollutedWater, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.LiquidHydrogen, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.Hydrazine, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.LiquidHydrazine, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.LiquidAlcohol, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.Helium, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.LiquidSodiumChloride, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.Silanol, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.LiquidSilanol, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.HydrochloricAcid, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.LiquidHydrochloricAcid, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.Ozone, hideIfZero: true, indent);
		DisplayGas(stringBuilder, gasMixture, gasMixture.LiquidOzone, hideIfZero: true, indent);
	}

	public static void MakeGasTooltip(Atmosphere atmosphere, StringBuilder stringBuilder, bool indent = false)
	{
		if (atmosphere == null || atmosphere.BeingDestroyed)
		{
			if (indent)
			{
				stringBuilder.Append(StringManager.Indent);
			}
			stringBuilder.AppendLine(GameStrings.Vacuum);
		}
		else
		{
			GasMixture gasMixture = atmosphere.GasMixture;
			DisplayBasicAtmosphere(atmosphere, stringBuilder, Pipe.ContentType.All, indent);
			MakeGasTooltip(gasMixture, stringBuilder, indent);
		}
	}

	public static string DisplayBasicAtmosphere(Atmosphere atmosphere)
	{
		StringBuilder stringBuilder = new StringBuilder();
		DisplayBasicAtmosphere(atmosphere, stringBuilder);
		return stringBuilder.ToString();
	}

	public static void DisplayBasicAtmosphere(TemperatureKelvin temperatureK, PressurekPa pressureKPa, StringBuilder stringBuilder, Pipe.ContentType contentType = Pipe.ContentType.All, bool indent = false, bool includeCelsius = true)
	{
		string value = GameStrings.None.AsColor("yellow");
		float value2 = pressureKPa.ToFloat() * 1000f;
		if (contentType != Pipe.ContentType.Gas && contentType != Pipe.ContentType.All)
		{
			return;
		}
		if (pressureKPa > PressurekPa.Zero)
		{
			StringManager.AddKeyValueLine(stringBuilder, GameStrings.Pressure, value2.ToStringPrefix("Pa", "yellow"), indent);
			StringManager.AddKeyValueLine(stringBuilder, GameStrings.Temperature, temperatureK.AsStringUnit(), indent);
			return;
		}
		StringManager.AddKeyValueLine(stringBuilder, GameStrings.Pressure, value, indent);
		if (contentType == Pipe.ContentType.Gas)
		{
			StringManager.AddKeyValueLine(stringBuilder, GameStrings.Temperature, value, indent);
		}
		else
		{
			StringManager.AddKeyValueLine(stringBuilder, GameStrings.Temperature, temperatureK.AsStringUnit(), indent);
		}
	}

	public static void DisplayBasicAtmosphere(Atmosphere atmosphere, StringBuilder stringBuilder, Pipe.ContentType contentType = Pipe.ContentType.All, bool indent = false)
	{
		if (atmosphere == null)
		{
			return;
		}
		DisplayBasicAtmosphere(atmosphere.Temperature, atmosphere.PressureGassesAndLiquids, stringBuilder, contentType, indent);
		VolumeLitres volume = atmosphere.Volume;
		if (indent)
		{
			stringBuilder.Append(StringManager.Indent);
		}
		stringBuilder.Append(GameStrings.AtmosphereVolume).Append(" ").Append(volume.ToFloat().ToStringPrefix("L", "yellow"))
			.AppendLine();
		if (contentType == Pipe.ContentType.Liquid || contentType == Pipe.ContentType.All)
		{
			VolumeLitres totalVolumeLiquids = atmosphere.TotalVolumeLiquids;
			if (indent)
			{
				stringBuilder.Append(StringManager.Indent);
			}
			stringBuilder.Append(GameStrings.LiquidsVolume).Append(" ").Append(totalVolumeLiquids.ToFloat().ToStringPrefix("L", "yellow"))
				.AppendLine();
		}
	}

	public static void CalculateLiquidAtmospheres()
	{
		_workingList.Clear();
		AllAtmospheres.ForEach(CalculateLiquidAtmosphereActions);
		lock (LiquidAtmospheres)
		{
			LiquidAtmospheres.Clear();
			LiquidAtmospheres.AddRange(_workingList);
		}
	}

	public static void Populate(Span<DensePool<Atmosphere>.Ref> atmosBuf, ref int atmosCount)
	{
		AllAtmospheres.Populate(atmosBuf, ref atmosCount);
	}
}
