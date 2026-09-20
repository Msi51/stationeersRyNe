using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Rockets;
using Rendering;
using Rendering.BatchRendering;
using TerrainSystem;
using TerrainSystem.Lods;
using Trading;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Assets.Scripts;

public class OcclusionManager : ThreadedManager
{
	public static OcclusionManager Instance;

	public static int LastOnServerTick;

	private const int SERVER_TICK_MS = 100;

	private const int CLIENT_TICK_MS = 1000;

	private const int ALL_TICK_MS = 1000;

	public static int TotalDynamicThings;

	public static ShadowQuality ShadowQualitySetting;

	public static int MaxShadowedLights = 8;

	private const int MAX_SHADOW_LIGHTS = 8192;

	public static readonly DensePool<IShadowLight> AllShadowLights = new DensePool<IShadowLight>("AllShadowLights", 8192);

	[Header("Distant Structure Rendering")]
	[Tooltip("Unit cube mesh (centred at origin, size 1) drawn as a proxy for distant LargeStructures.")]
	[SerializeField]
	private Mesh _distantCubeMesh;

	[Tooltip("GPU-instancing material exposing a per-instance '_Color' property.")]
	[SerializeField]
	private Material _distantMaterial;

	[SerializeField]
	private bool _distantRenderingEnabled = true;

	private const int DISTANT_PROXY_CAP = 1023;

	private const float DISTANT_PROXY_MIN_DISTANCE = 100f;

	private static readonly int DistantColorPropertyId = Shader.PropertyToID("_Color");

	private static readonly int DistantSpaceLineId = Shader.PropertyToID("_GlobalSpaceLine");

	private readonly Matrix4x4[][] _distantMatrices = new Matrix4x4[2][]
	{
		new Matrix4x4[1023],
		new Matrix4x4[1023]
	};

	private readonly Vector4[][] _distantColors = new Vector4[2][]
	{
		new Vector4[1023],
		new Vector4[1023]
	};

	private readonly int[] _distantCounts = new int[2];

	private volatile int _distantFront = -1;

	private volatile bool _distantAssetsReady;

	private bool _distantDebugDraw;

	private volatile int _diagTotalLarge;

	private volatile int _diagCandidates;

	private volatile int _diagDrawn;

	private volatile float _diagNearestDistSq;

	private volatile float _diagFarthestDistSq;

	public const int MAX_THING_COUNT = 65535;

	public static readonly ConcurrentDensePool<DynamicThing> AllDynamicThings = new ConcurrentDensePool<DynamicThing>("AllDynamicThings", 65535);

	public static readonly List<Thing> AllLodFlareThings = new List<Thing>(65535);

	public static readonly ConcurrentDensePool<Thing> AllThings = new ConcurrentDensePool<Thing>("AllThings", 65535);

	public static readonly HashSet<IOnEachDay> OnNewDayThings = new HashSet<IOnEachDay>(65535);

	private const int MAX_UPDATING_THING_COUNT = 16384;

	public static readonly DensePool<Thing> UpdatingThings = new DensePool<Thing>("UpdatingThings", 16384);

	public static readonly HashSet<Thing> UpdatingThings100MS = new HashSet<Thing>(65535);

	public static readonly HashSet<Thing> UpdatingThings1000MS = new HashSet<Thing>(65535);

	public static readonly HashSet<Thing> UpdatingAudioThings = new HashSet<Thing>(65535);

	public const float LOD_GENERATION_MOVE_DISTANCE = 8f;

	private static Action<IPerishable> ItemDecayClientAction = delegate(IPerishable perishable)
	{
		if (perishable != null && perishable.CanItemDecay())
		{
			perishable.OnDecayClient(1f);
		}
	};

	private static readonly Action<Structure> ServerTickStructureAction = delegate(Structure structure)
	{
		if (!(structure == null) && !structure.IsBeingDestroyed)
		{
			structure.OnServerTick(_serverTickDeltaTime);
		}
	};

	private static float _serverTickDeltaTime;

	private static float _lastServerTickTime;

	private static float _lastTime;

	private static float _deltaTime;

	private static int _lastFrame;

	private DateTime _forceKinematicTimeStamp;

	private bool _initializedTimeStamp;

	private double _forceKinematicInterval = 0.4000000059604645;

	private float _bedrockScaleForObjectCleanup = 1.1f;

	private const float DEFAULT_LAVA_DAMAGE_PER_SECOND = 25f;

	private static ThingShadowMode _thingShadowMode;

	private static readonly Func<DynamicThing, bool> OcclusionDynamicThingAction = delegate(DynamicThing dynamicThing)
	{
		if (GameManager.GameState != GameState.Running)
		{
			return true;
		}
		if ((object)dynamicThing == null)
		{
			return false;
		}
		Vector3 centerPosition = dynamicThing.CenterPosition;
		Slot parentSlot = dynamicThing.ParentSlot;
		TotalDynamicThings++;
		if (dynamicThing is Human human)
		{
			human.IsUnderLava = WorldSetting.Current.IsUnderLava(human.Position);
		}
		if ((dynamicThing is Human || parentSlot == null) && RocketMath.SquareDistanceComparison(centerPosition, dynamicThing.PreviousLodRequestPosition, 8f) > 0f)
		{
			LodManager.EnqueueRequesterToUpdate(dynamicThing);
			dynamicThing.PreviousLodRequestPosition = centerPosition;
		}
		if (dynamicThing is IGenerateMinables iGenerateMinables)
		{
			VoxelTerrain.GenerateMinables(iGenerateMinables);
		}
		if (GameManager.RunSimulation)
		{
			if (parentSlot == null && !RocketMath.Approximately(dynamicThing.Position, Vector3.zero))
			{
				dynamicThing.IsUnderLava = WorldSetting.Current.IsUnderLava(dynamicThing.Position);
				if (dynamicThing.ParentSlot == null && dynamicThing.IsInLava && !DifficultySetting.Current.Creative)
				{
					dynamicThing.ApplyLavaDamage();
				}
			}
			if (dynamicThing.ShouldResetPosition())
			{
				OnServer.ResetObjectYPos(dynamicThing).Forget();
			}
		}
		WorldGrid worldGrid = dynamicThing.WorldGrid;
		if ((object)parentSlot?.Parent != null)
		{
			Thing rootParent = parentSlot.Parent.RootParent;
			if (rootParent is DynamicThing { WorldCenterOfMass: var worldCenterOfMass })
			{
				dynamicThing.Position = worldCenterOfMass;
				dynamicThing.WorldCenterOfMass = worldCenterOfMass;
			}
			dynamicThing.WorldGrid = new WorldGrid(rootParent.CenterPosition);
			dynamicThing.SetWorldAtmosphere();
			CheckForRoom(dynamicThing);
		}
		else
		{
			dynamicThing.WorldGrid = new WorldGrid(centerPosition);
			CheckForRoom(dynamicThing);
			Atmosphere worldAtmosphere = dynamicThing.WorldAtmosphere;
			dynamicThing.SetWorldAtmosphere();
			if (worldGrid != dynamicThing.WorldGrid)
			{
				dynamicThing.GridController.AlertGridWatchers(worldGrid, dynamicThing, GridEvent.GridEventType.Leave);
				dynamicThing.GridController.AlertGridWatchers(dynamicThing.WorldGrid, dynamicThing, GridEvent.GridEventType.Enter);
				worldAtmosphere?.AllDynamicThings.Remove(dynamicThing);
				dynamicThing.WorldAtmosphere?.AllDynamicThings.Add(dynamicThing);
			}
			if (GameManager.RunSimulation)
			{
				dynamicThing.CheckForCollectorProximity();
			}
			if (GameManager.RunSimulation && WorldManager.HasGravity && !dynamicThing.IsEntity && dynamicThing.Position.y < -500f)
			{
				dynamicThing.IsOutOfBounds = true;
			}
		}
		if (GameManager.IsBatchMode)
		{
			dynamicThing.OnThreadUpdate();
		}
		else if ((bool)InventoryManager.ParentBrain)
		{
			if (GameManager.IsRunning)
			{
				dynamicThing.OcclusionTimeout -= _deltaTime;
			}
			if (!GameManager.IsRunning || dynamicThing.OcclusionTimeout <= 0f)
			{
				dynamicThing.SetOcclusion();
				dynamicThing.OcclusionTimeout = 0.1f;
			}
			dynamicThing.OnThreadUpdate();
		}
		return false;
	};

	private const float OCCLUSION_TIMEOUT = 0.1f;

	private static readonly Action<Structure> ThreadedStructureWorkSync = delegate(Structure structure)
	{
		if (GameManager.GameState == GameState.Running && (bool)structure && !structure.IsCursor)
		{
			structure.HasAtmosphere = structure.GridController.AtmosphericsController.HasAtmosphere(structure.WorldGrid);
			structure.OcclusionTimeout -= _deltaTime;
			if (!GameManager.IsBatchMode && structure.OcclusionTimeout <= 0f)
			{
				structure.SetOcclusion();
				structure.OcclusionTimeout = 0.1f;
			}
			structure.OnThreadUpdate();
		}
	};

	private static readonly Action<Thing> SetOcclusionAction = delegate(Thing thing)
	{
		thing?.SetOcclusion();
	};

	private const float DEFAULT_RENDER_DISTANCE_MULTIPLIER = 1f;

	private const float LOWEST_RENDER_DISTANCE_MULTIPLIER = 0.8f;

	private const float LOW_RENDER_DISTANCE_MULTIPLIER = 1f;

	private const float MEDIUM_RENDER_DISTANCE_MULTIPLIER = 1.5f;

	private const float HIGH_RENDER_DISTANCE_MULTIPLIER = 2f;

	private const float EXTREME_RENDER_DISTANCE_MULTIPLIER = 2.5f;

	public static int LightShadowDistanceSq => (Settings.CurrentData?.LightShadowDistance * Settings.CurrentData?.LightShadowDistance) ?? 900;

	public bool DistantProxyDebugDraw => _distantDebugDraw;

	public static float DeltaTime => _deltaTime;

	public static float LavaDamage => 25f * Mathf.Min(GameManager.DeltaTime, 1f);

	public static ThingShadowMode ThingShadowMode
	{
		get
		{
			return _thingShadowMode;
		}
		set
		{
			_thingShadowMode = value;
			switch (ThingShadowMode)
			{
			case ThingShadowMode.Extreme:
				WorldManager.ShadowCullSize = 0.3f;
				break;
			case ThingShadowMode.High:
				WorldManager.ShadowCullSize = 0.5f;
				break;
			case ThingShadowMode.Low:
			case ThingShadowMode.Medium:
				WorldManager.ShadowCullSize = 2f;
				break;
			default:
				WorldManager.ShadowCullSize = 0.3f;
				break;
			}
		}
	}

	public static float RenderDistanceMultiplier { get; private set; } = 1f;

	public static int HelmetLightShadowDistanceSq()
	{
		int num = (Settings.CurrentData?.LightShadowDistance ?? 30) / 2;
		return num * num;
	}

	private static void ResolveShadowLightBudget()
	{
		int num = Math.Min(MaxShadowedLights, 64);
		DensePool<IShadowLight>.ActiveEnumerable activeEnumerable;
		DensePool<IShadowLight>.ActiveEnumerable.Enumerator enumerator;
		if (num <= 0)
		{
			activeEnumerable = AllShadowLights.Active();
			enumerator = activeEnumerable.GetEnumerator();
			while (enumerator.MoveNext())
			{
				enumerator.Current?.SetShadowCasting(shouldCastShadows: false);
			}
			return;
		}
		Span<float> span = stackalloc float[num];
		int num2 = 0;
		activeEnumerable = AllShadowLights.Active();
		enumerator = activeEnumerable.GetEnumerator();
		while (enumerator.MoveNext())
		{
			IShadowLight current = enumerator.Current;
			if (current == null || !current.IsShadowCandidate)
			{
				continue;
			}
			float shadowDistanceSquared = current.ShadowDistanceSquared;
			if (num2 != num || !(shadowDistanceSquared >= span[num2 - 1]))
			{
				int num3 = ((num2 == num) ? (num2 - 1) : num2++);
				while (num3 > 0 && span[num3 - 1] > shadowDistanceSquared)
				{
					span[num3] = span[num3 - 1];
					num3--;
				}
				span[num3] = shadowDistanceSquared;
			}
		}
		float num4 = ((num2 > 0) ? span[num2 - 1] : float.MinValue);
		int num5 = 0;
		activeEnumerable = AllShadowLights.Active();
		enumerator = activeEnumerable.GetEnumerator();
		while (enumerator.MoveNext())
		{
			IShadowLight current2 = enumerator.Current;
			if (current2 != null)
			{
				bool flag = current2.IsShadowCandidate && num5 < num && current2.ShadowDistanceSquared <= num4;
				if (flag)
				{
					num5++;
				}
				current2.SetShadowCasting(flag);
			}
		}
	}

	private void ThreadedWorkDistantStructures()
	{
		if (!_distantRenderingEnabled || !_distantAssetsReady)
		{
			return;
		}
		float num = 10000f;
		Span<BoundRef<LargeStructure>> span = stackalloc BoundRef<LargeStructure>[1023];
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		DensePool<Structure>.ActiveEnumerable.Enumerator enumerator = GridController.AllStructuresPool.Active().GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (!(enumerator.Current is LargeStructure largeStructure))
			{
				continue;
			}
			num3++;
			float currentCameraDistanceSquared = largeStructure.CurrentCameraDistanceSquared;
			if (currentCameraDistanceSquared < num)
			{
				continue;
			}
			num4++;
			if (num2 != 1023 || !(currentCameraDistanceSquared >= span[num2 - 1].DistanceSquaredToCamera))
			{
				int colorIndex = largeStructure.CustomColor?.Index ?? (-1);
				Vector3 worldPosition = largeStructure.Position + largeStructure.Rotation * largeStructure.Bounds.center;
				BoundRef<LargeStructure> boundRef = new BoundRef<LargeStructure>(largeStructure.ReferenceId, largeStructure.Bounds, worldPosition.ToGridPosition(), largeStructure.Rotation, colorIndex, currentCameraDistanceSquared);
				int num5 = ((num2 == 1023) ? (num2 - 1) : num2++);
				while (num5 > 0 && span[num5 - 1].DistanceSquaredToCamera > currentCameraDistanceSquared)
				{
					span[num5] = span[num5 - 1];
					num5--;
				}
				span[num5] = boundRef;
			}
		}
		int num6 = ((_distantFront == 0) ? 1 : 0);
		Matrix4x4[] array = _distantMatrices[num6];
		Vector4[] array2 = _distantColors[num6];
		for (int i = 0; i < num2; i++)
		{
			BoundRef<LargeStructure> boundRef2 = span[i];
			Vector3 pos = boundRef2.Position.ToVector3();
			array[i] = Matrix4x4.TRS(pos, boundRef2.Rotation, boundRef2.Bounds.size);
			Vector4 vector = GameManager.GetColorSwatch(boundRef2.ColorIndex)?.Color ?? ((Color)new Vector4(1f, 1f, 1f, 1f));
			array2[i] = new Vector4(vector.x * 0.7f, vector.y * 0.7f, vector.z * 0.7f, vector.w);
		}
		_diagTotalLarge = num3;
		_diagCandidates = num4;
		_diagDrawn = num2;
		_diagNearestDistSq = ((num2 > 0) ? span[0].DistanceSquaredToCamera : 0f);
		_diagFarthestDistSq = ((num2 > 0) ? span[num2 - 1].DistanceSquaredToCamera : 0f);
		_distantCounts[num6] = num2;
		_distantFront = num6;
	}

	public void SetDistantProxyEnabled(bool enabled)
	{
		_distantRenderingEnabled = enabled;
	}

	public bool ToggleDistantProxyDebugDraw()
	{
		return _distantDebugDraw = !_distantDebugDraw;
	}

	public bool TryGetPublishedProxies(out Matrix4x4[] matrices, out int count)
	{
		int distantFront = _distantFront;
		if (!_distantRenderingEnabled || distantFront < 0)
		{
			matrices = null;
			count = 0;
			return false;
		}
		matrices = _distantMatrices[distantFront];
		count = _distantCounts[distantFront];
		return true;
	}

	public void PrintProxyDebugReport()
	{
		int distantFront = _distantFront;
		int num = ((distantFront >= 0) ? _distantCounts[distantFront] : 0);
		TreeString treeString = new TreeString("distant structure proxy");
		TreeString.Node($"enabled: {_distantRenderingEnabled}", treeString);
		TreeString.Node($"debug draw: {_distantDebugDraw}", treeString);
		TreeString myParent = TreeString.Node("assets", treeString);
		TreeString.Node($"ready: {_distantAssetsReady}", myParent);
		TreeString.Node("mesh: " + (_distantCubeMesh ? _distantCubeMesh.name : "<none>"), myParent);
		TreeString.Node("material: " + (_distantMaterial ? _distantMaterial.name : "<none>"), myParent);
		TreeString myParent2 = TreeString.Node("config", treeString);
		TreeString.Node($"switchover: {100f:0}m", myParent2);
		TreeString.Node($"batch cap: {1023}", myParent2);
		TreeString.Node($"space line: {1000f:0}m", myParent2);
		TreeString myParent3 = TreeString.Node("live", treeString);
		TreeString.Node(string.Format("drawn: {0} / {1}{2}", num, 1023, (num >= 1023) ? "  (SATURATED)" : ""), myParent3);
		if (_diagDrawn > 0)
		{
			TreeString.Node($"nearest: {Mathf.Sqrt(_diagNearestDistSq):0.0}m", myParent3);
			TreeString.Node($"farthest: {Mathf.Sqrt(_diagFarthestDistSq):0.0}m  (cull radius)", myParent3);
		}
		TreeString myParent4 = TreeString.Node("world", treeString);
		TreeString.Node($"large total: {_diagTotalLarge}", myParent4);
		TreeString.Node($"candidates (>{100f:0}m): {_diagCandidates}", myParent4);
		TreeString.Node($"camera ref: {InventoryManager.WorldPosition}", myParent4);
		treeString.ToConsole();
	}

	private void LateUpdate()
	{
		_distantAssetsReady = _distantCubeMesh != null && _distantMaterial != null;
		if (!_distantRenderingEnabled || !_distantAssetsReady)
		{
			return;
		}
		int distantFront = _distantFront;
		if (distantFront >= 0)
		{
			int num = _distantCounts[distantFront];
			if (num > 0)
			{
				Shader.SetGlobalFloat(DistantSpaceLineId, 1000f);
				Rendering.BatchRendering.BatchRenderer.Render(_distantMaterial, _distantCubeMesh, _distantMatrices[distantFront], _distantColors[distantFront], DistantColorPropertyId, num, ShadowCastingMode.Off);
			}
		}
	}

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		if (Instance == null)
		{
			Instance = this;
		}
	}

	public override void StartManager()
	{
		base.StartManager();
		if (GameManager.RunSimulation)
		{
			ServerTick().Forget();
		}
		else
		{
			ClientTick().Forget();
		}
		AllTick().Forget();
	}

	public override void StopManager()
	{
		base.StopManager();
		_initializedTimeStamp = false;
		StopAllCoroutines();
	}

	private static async UniTaskVoid ClientTick()
	{
		await UniTask.Delay(1000);
		while (GameManager.GameState != GameState.None)
		{
			while (GameManager.GameState != GameState.Running)
			{
				await UniTask.Delay(1000);
			}
			try
			{
				Item.AllDecayingItems.ForEach(ItemDecayClientAction);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			await UniTask.Delay(1000);
		}
	}

	private static async UniTaskVoid AllTick()
	{
		await UniTask.Delay(1000);
		while (GameManager.GameState != GameState.None)
		{
			while (GameManager.GameState != GameState.Running)
			{
				await UniTask.Delay(1000);
			}
			await UniTask.WaitForEndOfFrame();
		}
	}

	private static async UniTaskVoid ServerTick()
	{
		await UniTask.Delay(1000);
		_lastServerTickTime = GameManager.GameTime;
		while (GameManager.GameState != GameState.None)
		{
			while (GameManager.GameState != GameState.Running)
			{
				await UniTask.Delay(1000);
				_lastServerTickTime = GameManager.GameTime;
			}
			float a = GameManager.GameTime - _lastServerTickTime;
			_lastServerTickTime = GameManager.GameTime;
			a = (_serverTickDeltaTime = Mathf.Max(a, 0.1f));
			try
			{
				GridController.AllServerTickStructures.ForEach(ServerTickStructureAction);
				for (int num = Rocket.AllRockets.Count - 1; num >= 0; num--)
				{
					Rocket.AllRockets[num]?.OnServerTick(a);
				}
				HelperHintsManager.EvaluateObjectives(a);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			if (LastOnServerTick < int.MaxValue)
			{
				LastOnServerTick++;
			}
			else
			{
				LastOnServerTick = 0;
			}
			await UniTask.Delay(100);
		}
	}

	public override void ThreadedWork()
	{
		base.ThreadedWork();
		if (GameManager.GameState != GameState.Running)
		{
			return;
		}
		float gameTime = GameManager.GameTime;
		_deltaTime = gameTime - _lastTime;
		int frameCount = GameManager.FrameCount;
		if (_lastFrame == frameCount)
		{
			Profiler.EndThreadProfiling();
			return;
		}
		_lastFrame = frameCount;
		_lastTime = gameTime;
		try
		{
			ThreadedWorkDynamicThingSync();
			ThreadedWorkStructureSync();
			ThreadedWorkDistantStructures();
			ThreadedWorkNonThingOcclusionSync();
			ResolveShadowLightBudget();
		}
		catch (Exception ex)
		{
			Profiler.EndThreadProfiling();
			ConsoleWindow.PrintError("[EXCEPTION] Occlusion Manager: " + ex.Message);
		}
		Profiler.EndThreadProfiling();
	}

	private void ThreadedWorkDynamicThingSync()
	{
		if (!_initializedTimeStamp)
		{
			_initializedTimeStamp = true;
			_forceKinematicTimeStamp = DateTime.Now.AddSeconds(_forceKinematicInterval);
		}
		if (DateTime.Compare(_forceKinematicTimeStamp, DateTime.Now) < 0)
		{
			_forceKinematicTimeStamp = DateTime.Now;
			_forceKinematicTimeStamp = _forceKinematicTimeStamp.AddSeconds(_forceKinematicInterval);
		}
		TotalDynamicThings = 0;
		AllDynamicThings.ForEach(OcclusionDynamicThingAction);
	}

	private static async UniTaskVoid DestroyThingEnumerator(Thing thing)
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		if (!thing.IsBeingDestroyed)
		{
			OnServer.Destroy(thing);
		}
	}

	private void ThreadedWorkStructureSync()
	{
		GridController.AllStructuresPool.ForEach(ThreadedStructureWorkSync);
	}

	private void ThreadedWorkNonThingOcclusionSync()
	{
		int count = NonThingOcclusionHandler.AllNonThingOcclusionHandlers.Count;
		while (count-- > 0 && GameManager.GameState == GameState.Running)
		{
			NonThingOcclusionHandler nonThingOcclusionHandler = NonThingOcclusionHandler.AllNonThingOcclusionHandlers[count];
			if ((bool)nonThingOcclusionHandler)
			{
				nonThingOcclusionHandler.OcclusionTimeout -= _deltaTime;
				if (!GameManager.IsBatchMode && nonThingOcclusionHandler.OcclusionTimeout <= 0f)
				{
					nonThingOcclusionHandler.SetOcclusion();
					nonThingOcclusionHandler.OcclusionTimeout = 0.1f;
				}
			}
		}
	}

	private static void CheckForRoom(DynamicThing dynamicThing, DynamicThing dynamicParent = null)
	{
		if ((bool)dynamicParent && dynamicParent.Room != null)
		{
			dynamicThing.Room = dynamicParent.Room;
			dynamicThing.Cell = GridController.World.GetCell(dynamicThing.WorldGrid);
		}
		else
		{
			dynamicThing.Room = RoomController.World.GetRoom(dynamicThing.WorldGrid);
			dynamicThing.Cell = GridController.World.GetCell(dynamicThing.WorldGrid);
		}
	}

	public static void Register(DynamicThing dynamicThing)
	{
		AllDynamicThings.Add(dynamicThing);
	}

	public static void Deregister(DynamicThing dynamicThing)
	{
		AllDynamicThings.Remove(dynamicThing);
	}

	public static void Register(Thing thing)
	{
		if (!thing.IsCursor)
		{
			if (thing.ReferenceId == 0L)
			{
				throw new Exception("Error: Registered a new thing with referenceId of 0: " + thing.DisplayName);
			}
			AllThings.Add(thing);
			if (thing is IOnEachDay item)
			{
				OnNewDayThings.Add(item);
			}
			if (thing.IsUpdateEachFrame)
			{
				UpdatingThings.Add(thing);
			}
			if (thing.IsUpdate100MS)
			{
				UpdatingThings100MS.Add(thing);
			}
			if (thing.IsUpdate1000MS)
			{
				UpdatingThings1000MS.Add(thing);
			}
			if (thing.IsUpdateAudio)
			{
				UpdatingAudioThings.Add(thing);
			}
			List<LensFlare> lodFlares = thing.lodFlares;
			if (lodFlares != null && lodFlares.Count > 0)
			{
				AllLodFlareThings.Add(thing);
			}
		}
	}

	public static void Deregister(Thing thing)
	{
		if (!thing.IsCursor)
		{
			AllThings.Remove(thing);
			if (thing is IOnEachDay item)
			{
				OnNewDayThings.Remove(item);
			}
			if (thing.IsUpdateEachFrame)
			{
				UpdatingThings.Remove(thing);
			}
			if (thing.IsUpdate100MS)
			{
				UpdatingThings100MS.Remove(thing);
			}
			if (thing.IsUpdate1000MS)
			{
				UpdatingThings1000MS.Remove(thing);
			}
			if (thing.IsUpdateAudio)
			{
				UpdatingAudioThings.Remove(thing);
			}
			List<LensFlare> lodFlares = thing.lodFlares;
			if (lodFlares != null && lodFlares.Count > 0)
			{
				AllLodFlareThings.Remove(thing);
			}
		}
	}

	public static void ClearAll()
	{
		_lastFrame = Time.frameCount;
		_lastTime = Time.time;
		_deltaTime = 0f;
		AllThings.Clear();
		AllDynamicThings.Clear();
		UpdatingThings.Clear();
		OnNewDayThings.Clear();
		AllLodFlareThings.Clear();
		AllShadowLights.Clear();
		LogicDisplayDigitRenderer.ClearAll();
	}

	public static void CheckAllOcclusion()
	{
		GridController.AllStructuresPool.ForEach(SetOcclusionAction);
		AllDynamicThings.ForEach(SetOcclusionAction);
	}

	public void UpdateRenderDistanceMultiplier()
	{
		Enum.TryParse<RenderDistance>(Settings.CurrentData.RenderDistance, out var result);
		RenderDistanceMultiplier = result switch
		{
			RenderDistance.Default => 1f, 
			RenderDistance.Lowest => 0.8f, 
			RenderDistance.Low => 1f, 
			RenderDistance.Medium => 1.5f, 
			RenderDistance.High => 2f, 
			RenderDistance.Extreme => 2.5f, 
			_ => 1f, 
		};
		if (GameManager.GameState != GameState.None)
		{
			CheckAllOcclusion();
		}
	}
}
