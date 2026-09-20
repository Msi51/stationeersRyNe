using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using Networks;
using UnityEngine;
using UnityEngine.Rendering;
using Weather;

namespace Assets.Scripts;

public class AtmosphericsController
{
	public enum DebugMode
	{
		Off,
		Data,
		Velocity,
		Pressure,
		Temperature,
		Liquids,
		Pollutants,
		Movement,
		Networking
	}

	public enum ManagerState
	{
		Idle,
		Thread,
		Main
	}

	public delegate void AtmosphericsEvent();

	public const float UpdateDistance = 20f;

	public static AtmosphericsController World;

	[NonSerialized]
	public GridController GridController;

	public static WaterVoxelVisualizer VisualizerPrefab;

	public WaterVoxelVisualizer WaterVisualizer;

	public GameObject VoxelWaterEffect;

	public static readonly object LockAllControllers = new object();

	[ReadOnly]
	public ManagerState State;

	public ParticleSystem GasVisualizerParticleSystem;

	public static ParticleSystem GasVisualizerPrefab;

	public Vector3[] ParticlePositions;

	public Vector3[] AtmosphereVelocity;

	public ParticleSystem.Particle[] _particles;

	public int AtmosphericParticleNumber;

	private HashSet<Atmosphere> _assigned = new HashSet<Atmosphere>(1024);

	private static List<Atmosphere>[] MixGroups = new List<Atmosphere>[27];

	private static readonly Action<Atmosphere> AtmosphereMixJobAction = delegate(Atmosphere atmos)
	{
		AtmosphereHelper.AtmosphereMode? atmosphereMode = atmos?.Mode;
		if (atmosphereMode.HasValue && atmosphereMode.GetValueOrDefault() == AtmosphereHelper.AtmosphereMode.World && !atmos.BeingDestroyed)
		{
			atmos.ResetLiquidFlowFlags();
			MixGroups[atmos.WorkerJobIndex].Add(atmos);
		}
	};

	private static readonly Action<Atmosphere> AtmosphereInternalReactionsJobAction = delegate(Atmosphere atmosphere)
	{
		if (atmosphere != null && !atmosphere.BeingDestroyed)
		{
			AtmosphericsWorker.Assign(atmosphere);
		}
	};

	private static readonly Action<Atmosphere> RunOpenNeighboursJobsAction = delegate(Atmosphere atmosphere)
	{
		if (atmosphere != null && !atmosphere.BeingDestroyed && atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World)
		{
			AtmosphericsWorker.Assign(atmosphere, weighted: false);
		}
	};

	private static float _directionDampTimeMs = 5000f;

	public readonly Queue<AtmosphericsNetwork> RefreshPendingAtmosphericEvent = new Queue<AtmosphericsNetwork>();

	public static Atmosphere ReadonlyGlobalAtmosphere(Grid3 grid)
	{
		return PlanetaryAtmosphereSimulation.ReadOnlyGlobal(new WorldGrid(grid));
	}

	public void Clear()
	{
		if (!GameManager.IsBatchMode)
		{
			if (GasVisualizerParticleSystem != null)
			{
				UnityEngine.Object.Destroy(GasVisualizerParticleSystem.gameObject);
			}
			if (WaterVisualizer != null)
			{
				UnityEngine.Object.Destroy(WaterVisualizer.gameObject);
				WaterVisualizer = null;
			}
		}
	}

	public AtmosphericsController(GridController parentController)
	{
		GridController = parentController;
		if (!GameManager.IsBatchMode)
		{
			if (VisualizerPrefab == null)
			{
				VisualizerPrefab = Resources.Load<WaterVoxelVisualizer>("Voxels/VoxelWaterVisualizer");
			}
			if (GasVisualizerPrefab == null)
			{
				GasVisualizerPrefab = Resources.Load<ParticleSystem>("Effects/Gas");
			}
			GasVisualizerParticleSystem = UnityEngine.Object.Instantiate(GasVisualizerPrefab);
			int maxParticles = GasVisualizerParticleSystem.main.maxParticles;
			ParticlePositions = new Vector3[maxParticles];
			AtmosphereVelocity = new Vector3[maxParticles];
			if (_particles == null)
			{
				_particles = new ParticleSystem.Particle[GasVisualizerParticleSystem.main.maxParticles];
			}
			if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D9 && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLCore && !WaterVoxelVisualizer.Disabled)
			{
				if (VisualizerPrefab == null)
				{
					VisualizerPrefab = Resources.Load<WaterVoxelVisualizer>("Voxels/VoxelWaterVisualizer");
				}
				WaterVisualizer = UnityEngine.Object.Instantiate(VisualizerPrefab);
				WaterVisualizer.AtmosController = this;
				WaterVisualizer.GridController = GridController;
			}
		}
		if (parentController.IsWorld)
		{
			PlanetaryAtmosphereSimulation.CreateGlobalAtmosphere(WorldSetting.Current.Data.GlobalAtmosphereData);
		}
		for (int i = 0; i < MixGroups.Length; i++)
		{
			MixGroups[i] = new List<Atmosphere>(64);
		}
		State = ManagerState.Idle;
	}

	public void DoAtmosphereMixJobs()
	{
		AtmosphericsWorker.ClearScores();
		_assigned.Clear();
		List<Atmosphere>[] mixGroups = MixGroups;
		for (int i = 0; i < mixGroups.Length; i++)
		{
			mixGroups[i].Clear();
		}
		AtmosphericsManager.AllAtmospheres.ForEach(AtmosphereMixJobAction);
		int num = 0;
		int num2 = int.MaxValue;
		for (int j = 0; j < MixGroups.Length; j++)
		{
			int count = MixGroups[j].Count;
			if (count == 0)
			{
				continue;
			}
			if (count > num)
			{
				num = count;
			}
			if (count < num2)
			{
				num2 = count;
			}
			foreach (Atmosphere item in MixGroups[j])
			{
				AtmosphericsWorker.Assign(item);
			}
			AtmosphericsWorker.Execute(AtmosphericsWorker.Job.Mix);
			AtmosphericsWorker.WaitForCompletion();
			AtmosphericsWorker.ClearScores();
		}
	}

	public void RunInternalReactionsJobs()
	{
		AtmosphericsWorker.ClearScores();
		AtmosphericsManager.AllAtmospheres.ForEach(AtmosphereInternalReactionsJobAction);
		AtmosphericsWorker.Execute(AtmosphericsWorker.Job.InternalReactions);
		AtmosphericsWorker.WaitForCompletion();
	}

	public void RunOpenNeighboursJobs()
	{
		AtmosphericsWorker.ClearScores();
		AtmosphericsManager.AllAtmospheres.ForEach(RunOpenNeighboursJobsAction);
		AtmosphericsWorker.Execute(AtmosphericsWorker.Job.CalculateOpenNeighbours);
		AtmosphericsWorker.WaitForCompletion();
		if (RoomController.RegenStormCurtain)
		{
			RoomController.RegenStormCurtain = false;
			WeatherManager.RegenStormCurtain = true;
		}
	}

	public static void AtmosphereJob(Atmosphere atmosphere)
	{
		if (atmosphere == null)
		{
			return;
		}
		switch (atmosphere.Mode)
		{
		case AtmosphereHelper.AtmosphereMode.World:
			atmosphere.EnsureWorldPositionFromGrid();
			if ((bool)InventoryManager.Parent)
			{
				atmosphere.SquareDistanceToPlayer = InventoryManager.WorldPosition.DistanceSquared(atmosphere.WorldPosition);
			}
			break;
		case AtmosphereHelper.AtmosphereMode.Thing:
			atmosphere.WorldPosition = (((object)atmosphere.Thing != null) ? atmosphere.Thing.CenterPosition : GridController.World.LocalToWorld(atmosphere.Grid));
			break;
		}
		if (atmosphere.Mode != AtmosphereHelper.AtmosphereMode.World || LightManager.SunPathTraceWorldAtmos || (atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World && atmosphere.Room != null))
		{
			LightManager.CheckLightOcclusion(atmosphere);
		}
		atmosphere.CombustionEnergy = MoleEnergy.Zero;
		if (atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World)
		{
			atmosphere.Direction = Vector3.Lerp(atmosphere.Direction, Vector3.zero, (float)GameManager.GameTickSpeedMs / _directionDampTimeMs);
		}
		if (!atmosphere.IsInvalidWorld())
		{
			if (atmosphere.GasMixture.GetTotalMolesGassesAndLiquids > MoleQuantity.Zero || atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World)
			{
				atmosphere.ReactWithStructures();
				atmosphere.TryCombust();
				atmosphere.ReactWithCell();
				atmosphere.StateChange();
				atmosphere.EqualiseInternalEnergy();
				atmosphere.Cleanup();
			}
			else
			{
				atmosphere.Sparked = false;
				atmosphere.ResetCombustionData();
			}
		}
	}

	public void RefreshNetworks()
	{
		while (RefreshPendingAtmosphericEvent.Count > 0)
		{
			RefreshPendingAtmosphericEvent.Dequeue()?.RefreshNetwork();
		}
	}

	public bool HasAtmosphere(WorldGrid localGrid)
	{
		if (localGrid == WorldGrid.INVALID)
		{
			return false;
		}
		return SampleGlobalAtmosphere(localGrid)?.IsActive() ?? false;
	}

	public Atmosphere CloneGlobalAtmosphere(WorldGrid worldGrid, long referenceId = 0L, bool setStateActive = true, bool allowCrewModules = true)
	{
		if (NetworkManager.IsClient)
		{
			throw new Exception("Clone Global Atmosphere called on Client");
		}
		Atmosphere atmosphere = GetAtmosphereLocal(worldGrid, allowCrewModules);
		if (referenceId > 0 && atmosphere != null && atmosphere.ReferenceId != referenceId)
		{
			ConsoleWindow.PrintError("Tried to Create a new World Atmosphere with reference Id '" + StringManager.Get((int)referenceId) + "' at '" + StringManager.GetGridString(worldGrid) + "' Where one already exists");
		}
		if (atmosphere != null && setStateActive)
		{
			atmosphere.AtmosLifeState = AtmosLifeState.Active;
		}
		if (atmosphere == null)
		{
			atmosphere = new Atmosphere(worldGrid, referenceId);
			if (!AtmosphericEventInstance.IsGridAwaiting(worldGrid))
			{
				PlanetaryAtmosphereSimulation.CloneGlobalGasMix(atmosphere);
			}
		}
		return atmosphere;
	}

	public Atmosphere SampleGlobalAtmosphere(WorldGrid grid)
	{
		if (grid == WorldGrid.INVALID)
		{
			return null;
		}
		return GetAtmosphereLocal(grid) ?? PlanetaryAtmosphereSimulation.ReadOnlyGlobal(grid);
	}

	public Atmosphere GetAtmosphereLocal(WorldGrid worldGrid, bool allowCrewModules = true)
	{
		if (allowCrewModules && Cell.IsInCrewModule(worldGrid, out var crewModule))
		{
			return crewModule.InternalAtmosphere;
		}
		return AtmosphericsManager.Find(worldGrid);
	}

	public void SetRoom(WorldGrid grid, Room room)
	{
		Atmosphere atmosphereLocal = GetAtmosphereLocal(grid);
		if (atmosphereLocal != null)
		{
			atmosphereLocal.Room = room;
		}
	}

	public void CheckAtmosphereConnections(WorldGrid worldGrid)
	{
		GetAtmosphereLocal(worldGrid)?.CalculateWorldVolume();
	}

	public void IgniteAtmosphere(WorldGrid grid, MoleEnergy energyReleased)
	{
		Atmosphere atmosphereLocal = GetAtmosphereLocal(grid);
		if (atmosphereLocal != null && !atmosphereLocal.IsGlobalAtmosphere)
		{
			AtmosphericEventInstance.CreateAddEnergy(atmosphereLocal, energyReleased, spark: true);
		}
	}

	public static void HandleMainThreadEvents()
	{
		NetworkAtmosphereEvent.HandleNetworkChangedEvents();
		AtmosphericEventInstance.HandleNetworkChangedEvents();
		AtmosphericEventInstance.ClearAwaiting();
	}

	public static void PreSaveCleanup()
	{
		HandleMainThreadEvents();
	}
}
