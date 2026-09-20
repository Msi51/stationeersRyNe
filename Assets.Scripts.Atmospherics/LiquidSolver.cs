using System.Collections.Concurrent;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Util;
using Rendering.BatchRendering;
using UnityEngine;

namespace Assets.Scripts.Atmospherics;

public class LiquidSolver : ManagerBase
{
	private class WaterRenderBuffer
	{
		public int RenderIndex;

		public readonly Matrix4x4[] Matrices = new Matrix4x4[1023];

		public readonly Vector4[] Colors = new Vector4[1023];

		public readonly Vector4[] FlowDirections = new Vector4[1023];
	}

	[SerializeField]
	private LiquidParticle _liquidParticlePrefab;

	[SerializeField]
	private ParticleSystem _particleSystem;

	[SerializeField]
	private Transform _particleSystemTransform;

	[SerializeField]
	private int _maxParticles;

	[SerializeField]
	private float _visualizerParticleSpawnChance;

	private const float LIQUID_RENDER_SQR_DISTANCE = 5000f;

	private const float OPEN_FACE_SPAWN_SIZE = 0.7f;

	private Queue<LiquidParticle> _particlePool;

	private ConcurrentQueue<LiquidBallSpawnInfo> _spawns = new ConcurrentQueue<LiquidBallSpawnInfo>();

	private ParticleSystem.ShapeModule _shapeModule;

	private ParticleSystem.MainModule _mainModule;

	public static LiquidSolver Instance;

	private const string PARTICLE_NAME = "~liquidParticle_";

	private const int DISTANT_WATER_SIZE = 150;

	private const int GLOBAL_RENDER_DISTANCE = 12;

	private const int GLOBAL_RENDER_HEIGHT = 3;

	[SerializeField]
	private Material _waterMaterial;

	[SerializeField]
	private Mesh _waterMesh;

	private static readonly int COLOR_PROPERTY = Shader.PropertyToID("_Color");

	public const double LiquidRenderThresholdLitres = 0.3;

	public static readonly VolumeLitres DEEP_THRESHOLD = new VolumeLitres(1333.3333333333333);

	private static readonly int FLOW_DIRECTION_PROPERTY = Shader.PropertyToID("_FlowDirection");

	private const int MAX_BATCH_SIZE = 1023;

	private readonly WaterRenderBuffer _renderBufferA = new WaterRenderBuffer();

	private readonly WaterRenderBuffer _renderBufferB = new WaterRenderBuffer();

	private bool _swapBuffer;

	private readonly object _waterRenderLockObject = new object();

	private const float MINIMUM_SOLVER_PARTICLE_VELOCITY = 0.005f;

	private const float FACE_BLOCKED_OFFSET = 0.025f;

	private const float FACE_BLOCKED_SCALE = 0.05f;

	public int MaxParticles => _maxParticles;

	public List<LiquidParticle> ActiveParticles { get; private set; }

	public bool CapacityReached => _particlePool.Count == 0;

	public static bool RenderingEnabled { get; set; } = true;

	public static bool SolverEnabled { get; set; } = true;

	private WaterRenderBuffer Read
	{
		get
		{
			if (!_swapBuffer)
			{
				return _renderBufferB;
			}
			return _renderBufferA;
		}
	}

	private WaterRenderBuffer Write
	{
		get
		{
			if (!_swapBuffer)
			{
				return _renderBufferA;
			}
			return _renderBufferB;
		}
	}

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		if (Instance == null)
		{
			Instance = this;
		}
		InstantiateParticles();
	}

	private void InstantiateParticles()
	{
		_shapeModule = _particleSystem.shape;
		_mainModule = _particleSystem.main;
		if (GameManager.RunSimulation)
		{
			ActiveParticles = new List<LiquidParticle>(_maxParticles);
			_particlePool = new Queue<LiquidParticle>(_maxParticles);
			for (int i = 0; i < _maxParticles; i++)
			{
				LiquidParticle liquidParticle = Object.Instantiate(_liquidParticlePrefab);
				liquidParticle.GameObject.name = "~liquidParticle_" + StringManager.Get(i);
				liquidParticle.Deactivate();
				_particlePool.Enqueue(liquidParticle);
			}
		}
	}

	private Vector3 RandomPointOnFace(Vector3 center, Vector3 normal, Vector2 size)
	{
		return PointOnFace(center, normal, Random.Range(0f - size.x, size.x), Random.Range(0f - size.y, size.y));
	}

	private Vector3 PointOnFace(Vector3 center, Vector3 normal, float x, float y)
	{
		Vector3 vector = new Vector3(x, y, 0f);
		Quaternion quaternion = Quaternion.FromToRotation(Vector3.forward, normal);
		return center + quaternion * vector;
	}

	public void QueueSpawns(LiquidBallSpawnCollection liquidBallSpawns)
	{
		foreach (LiquidBallSpawnInfo item in liquidBallSpawns.SpawnInfo)
		{
			_spawns.Enqueue(item);
		}
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		if (GameManager.GameState == GameState.None)
		{
			return;
		}
		if (GameManager.RunSimulation && _spawns.Count > 0)
		{
			LiquidBallSpawnInfo result;
			while (_spawns.TryDequeue(out result))
			{
				SpawnBalls(result);
			}
		}
		if (RenderingEnabled && !GameManager.IsBatchMode)
		{
			IterateAtmospheres();
			AfterIterateAtmospheres();
		}
	}

	private void FixedUpdate()
	{
		if (GameManager.GameState == GameState.Running)
		{
			UpdateParticles();
		}
	}

	private void SpawnBalls(LiquidBallSpawnInfo spawnInfo)
	{
		if (spawnInfo.Origin == null)
		{
			return;
		}
		int b = Mathf.Clamp(1 + (int)(spawnInfo.GasMix.GetTotalMolesLiquids / 1000.0).ToFloat(), 1, 20);
		int num = Mathf.Min(_particlePool.Count, b);
		if (num == 0)
		{
			AtmosphericEventInstance.CreateAdd(spawnInfo.Origin, spawnInfo.GasMix);
			return;
		}
		spawnInfo.GasMix.Divide(num);
		float time = Time.time;
		float maxInclusive = (spawnInfo.Origin.LiquidVolumeRatio + 0.5f) * 2f;
		for (int i = 0; i < num; i++)
		{
			LiquidParticle liquidParticle = _particlePool.Dequeue();
			Vector3 position = spawnInfo.Direction * 0.05f + RandomPointOnFace(spawnInfo.Position, spawnInfo.Direction, Vector2.one * 0.7f);
			Vector3 velocity = spawnInfo.Direction * Random.Range(0.1f, maxInclusive);
			liquidParticle.Initialize(position, velocity, spawnInfo.GasMix, time, spawnInfo.Origin);
			ActiveParticles.Add(liquidParticle);
		}
	}

	private void UpdateParticles()
	{
		float time = Time.time;
		for (int num = ActiveParticles.Count - 1; num >= 0; num--)
		{
			LiquidParticle liquidParticle = ActiveParticles[num];
			if (liquidParticle.HasRipened(time))
			{
				Atmosphere atmosphere = AtmosphericsManager.Find(new WorldGrid(liquidParticle.Transform.position));
				if (atmosphere != null && atmosphere.TotalVolumeLiquids > RenderThreshold(atmosphere))
				{
					AtmosphericEventInstance.CreateAdd(atmosphere, liquidParticle.GetGasMix());
					ActiveParticles.RemoveAt(num);
					ReturnToPool(liquidParticle);
				}
				else if (liquidParticle.HasExpired(time) || liquidParticle.VelocitySqrMagnitude < 0.005f)
				{
					if (atmosphere != null)
					{
						AtmosphericEventInstance.CreateAdd(atmosphere, liquidParticle.GetGasMix());
					}
					ActiveParticles.RemoveAt(num);
					ReturnToPool(liquidParticle);
				}
			}
		}
	}

	private void ReturnToPool(LiquidParticle particle)
	{
		particle.Deactivate();
		_particlePool.Enqueue(particle);
	}

	public void ClearAll()
	{
		foreach (LiquidParticle activeParticle in ActiveParticles)
		{
			ReturnToPool(activeParticle);
		}
		ActiveParticles.Clear();
		_spawns.Clear();
		_particleSystem.Clear();
	}

	public void ReturnLiquidsAndReset()
	{
		foreach (LiquidParticle activeParticle in ActiveParticles)
		{
			activeParticle.ReturnToOrigin();
		}
		ClearAll();
	}

	private void IterateAtmospheres()
	{
		lock (AtmosphericsManager.LiquidAtmospheres)
		{
			int count = AtmosphericsManager.LiquidAtmospheres.Count;
			while (count-- > 0)
			{
				Atmosphere atmosphere = AtmosphericsManager.LiquidAtmospheres[count];
				if (atmosphere != null && !atmosphere.BeingDestroyed && !(atmosphere.SquareDistanceToPlayer > 5000f))
				{
					DoVisualizerParticles(atmosphere);
				}
			}
		}
	}

	public static void PrepareLiquidRenderBatches()
	{
		Instance.Write.RenderIndex = -1;
		Instance.BuildDistantGlobalWater(CameraController.CameraPosition);
		Instance.BuildGlobalRenderBatches(CameraController.CameraPosition);
		int count = AtmosphericsManager.LiquidAtmospheres.Count;
		while (count-- > 0)
		{
			Atmosphere atmosphere = AtmosphericsManager.LiquidAtmospheres[count];
			if (atmosphere != null && !atmosphere.BeingDestroyed && !(atmosphere.SquareDistanceToPlayer > 5000f))
			{
				Instance.BuildLiquidRenderBatches(atmosphere);
			}
		}
		Instance.FlipWaterRenderBuffer();
	}

	private void BuildDistantGlobalWater(Vector3 cameraPosition)
	{
		if (GlobalAtmosphereLiquid.IsRendered)
		{
			float liquidHeight = GlobalAtmosphereLiquid.LiquidHeight;
			WorldGrid wgrid = new WorldGrid(new Vector3(cameraPosition.x, liquidHeight, cameraPosition.z));
			float worldGridLiquidVolumeRatio = GlobalAtmosphereLiquid.GetWorldGridLiquidVolumeRatio(wgrid);
			Color liquidColor = Atmosphere.GetLiquidColor(PlanetaryAtmosphereSimulation.GetGlobalGasMixCopy(AtmosphereHelper.MatterState.All));
			float x = 300f;
			float num = Mathf.Clamp(worldGridLiquidVolumeRatio * 2f, 0f, 2f);
			float z = 48f;
			float num2 = 174f;
			float y = -1f + num / 2f;
			Vector3 pos = wgrid.Value.ToVector3() + new Vector3(num2, y, 0f);
			Vector3 s = new Vector3(x, num, z);
			Write.RenderIndex++;
			Write.Matrices[Write.RenderIndex] = Matrix4x4.TRS(pos, Quaternion.identity, s);
			Write.Colors[Write.RenderIndex] = liquidColor;
			Write.FlowDirections[Write.RenderIndex] = Vector4.zero;
			pos = wgrid.Value.ToVector3() + new Vector3(0f - num2, y, 0f);
			Write.RenderIndex++;
			Write.Matrices[Write.RenderIndex] = Matrix4x4.TRS(pos, Quaternion.identity, s);
			Write.Colors[Write.RenderIndex] = liquidColor;
			Write.FlowDirections[Write.RenderIndex] = Vector4.zero;
			x = 648f;
			num = Mathf.Clamp(worldGridLiquidVolumeRatio * 2f, 0f, 2f);
			z = 300f;
			float num3 = 174f;
			pos = wgrid.Value.ToVector3() + new Vector3(0f, y, num3);
			s = new Vector3(x, num, z);
			Write.RenderIndex++;
			Write.Matrices[Write.RenderIndex] = Matrix4x4.TRS(pos, Quaternion.identity, s);
			Write.Colors[Write.RenderIndex] = liquidColor;
			Write.FlowDirections[Write.RenderIndex] = Vector4.zero;
			pos = wgrid.Value.ToVector3() + new Vector3(0f, y, 0f - num3);
			Write.RenderIndex++;
			Write.Matrices[Write.RenderIndex] = Matrix4x4.TRS(pos, Quaternion.identity, s);
			Write.Colors[Write.RenderIndex] = liquidColor;
			Write.FlowDirections[Write.RenderIndex] = Vector4.zero;
		}
	}

	private void BuildGlobalRenderBatches(Vector3 cameraPosition)
	{
		if (!GlobalAtmosphereLiquid.IsRendered)
		{
			return;
		}
		float liquidHeight = GlobalAtmosphereLiquid.LiquidHeight;
		Vector3 vector = new Vector3(cameraPosition.x, Mathf.Min(liquidHeight, cameraPosition.y), cameraPosition.z);
		Color liquidColor = Atmosphere.GetLiquidColor(PlanetaryAtmosphereSimulation.GetGlobalGasMixCopy(AtmosphereHelper.MatterState.All));
		for (int i = 0; i <= 3; i++)
		{
			for (int j = -12; j <= 12; j++)
			{
				for (int k = -12; k <= 12; k++)
				{
					if (Write.RenderIndex >= 1022)
					{
						return;
					}
					float x = vector.x + (float)j * 2f;
					float y = vector.y + (float)i * 2f;
					float z = vector.z + (float)k * 2f;
					WorldGrid worldGrid = new WorldGrid(new Vector3(x, y, z));
					if (RoomController.World.GetRoom(worldGrid) != null)
					{
						continue;
					}
					Cell cell = GridController.World.GetCell(worldGrid);
					if (cell == null || !cell.IsBlocked)
					{
						float worldGridLiquidVolumeRatio = GlobalAtmosphereLiquid.GetWorldGridLiquidVolumeRatio(worldGrid);
						if (worldGridLiquidVolumeRatio <= 0f)
						{
							return;
						}
						Write.RenderIndex++;
						bool flag = false;
						bool flag2 = false;
						bool flag3 = false;
						bool flag4 = false;
						bool flag5 = false;
						bool flag6 = false;
						if (cell != null)
						{
							flag = cell.Lookup[Grid3.Face.Up] != null;
							flag2 = cell.Lookup[Grid3.Face.Down] != null;
							flag3 = cell.Lookup[Grid3.Face.North] != null;
							flag4 = cell.Lookup[Grid3.Face.South] != null;
							flag5 = cell.Lookup[Grid3.Face.East] != null;
							flag6 = cell.Lookup[Grid3.Face.West] != null;
						}
						float num = (flag ? (-0.025f) : 0f);
						num += (flag2 ? 0.025f : 0f);
						float num2 = (flag3 ? (-0.025f) : 0f);
						num2 += (flag4 ? 0.025f : 0f);
						float num3 = (flag5 ? (-0.025f) : 0f);
						num3 += (flag6 ? 0.025f : 0f);
						float num4 = 1f - (flag ? 0.025f : 0f);
						num4 -= (flag2 ? 0.025f : 0f);
						float num5 = 1f - (flag3 ? 0.025f : 0f);
						num5 -= (flag4 ? 0.025f : 0f);
						float num6 = 1f - (flag5 ? 0.025f : 0f);
						num6 -= (flag6 ? 0.025f : 0f);
						float num7 = Mathf.Clamp(worldGridLiquidVolumeRatio * 2f, 0f, 2f) * num4;
						Vector3 s = new Vector3(2f * num6, num7, 2f * num5);
						Vector3 vector2 = new Vector3(num3, num + (-1f + num7 / (2f * num4)), num2);
						Write.Matrices[Write.RenderIndex] = Matrix4x4.TRS(worldGrid.Value.ToVector3() + vector2, Quaternion.identity, s);
						Write.Colors[Write.RenderIndex] = liquidColor;
						Write.FlowDirections[Write.RenderIndex] = Vector4.zero;
					}
				}
			}
		}
	}

	private void AfterIterateAtmospheres()
	{
		lock (_waterRenderLockObject)
		{
			if (Read.RenderIndex >= 0)
			{
				BatchRenderer.Render(_waterMaterial, _waterMesh, Read.Matrices, Read.Colors, COLOR_PROPERTY, Read.FlowDirections, FLOW_DIRECTION_PROPERTY, Read.RenderIndex + 1);
			}
		}
	}

	private void DoVisualizerParticles(Atmosphere atmosphere)
	{
		if (atmosphere.LiquidParticleDirection != LiquidParticleDirection.None)
		{
			if ((atmosphere.LiquidParticleDirection & LiquidParticleDirection.Left) != LiquidParticleDirection.None)
			{
				EmitVisualizerParticles(atmosphere, Vector3.left);
			}
			if ((atmosphere.LiquidParticleDirection & LiquidParticleDirection.Right) != LiquidParticleDirection.None)
			{
				EmitVisualizerParticles(atmosphere, Vector3.right);
			}
			if ((atmosphere.LiquidParticleDirection & LiquidParticleDirection.Forward) != LiquidParticleDirection.None)
			{
				EmitVisualizerParticles(atmosphere, Vector3.forward);
			}
			if ((atmosphere.LiquidParticleDirection & LiquidParticleDirection.Back) != LiquidParticleDirection.None)
			{
				EmitVisualizerParticles(atmosphere, Vector3.back);
			}
		}
	}

	private void EmitVisualizerParticles(Atmosphere atmosphere, Vector3 direction)
	{
		if (!(Random.Range(0f, 1f) > _visualizerParticleSpawnChance))
		{
			float num = Mathf.Min(2f, atmosphere.LiquidVolumeRatio * 2f);
			float num2 = 1f - num / 2f;
			_particleSystemTransform.position = atmosphere.WorldPosition + direction + Vector3.down * num2;
			_particleSystemTransform.forward = direction;
			_shapeModule.scale = new Vector3(2f, num, 2f);
			_mainModule.startSpeedMultiplier = atmosphere.LiquidVolumeRatio + 1f;
			ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
			{
				startColor = Atmosphere.GetLiquidColor(atmosphere.GasMixture)
			};
			_particleSystem.Emit(emitParams, 1);
		}
	}

	public static VolumeLitres RenderThreshold(Atmosphere atmosphere)
	{
		return new VolumeLitres(0.3) * atmosphere.LiquidWorldVolumeScale;
	}

	private void FlipWaterRenderBuffer()
	{
		lock (_waterRenderLockObject)
		{
			_swapBuffer = !_swapBuffer;
		}
	}

	private void BuildLiquidRenderBatches(Atmosphere atmosphere)
	{
		if (Write.RenderIndex < 1022)
		{
			Write.RenderIndex++;
			Cell cell = atmosphere.Cell;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			bool flag6 = false;
			if (cell != null)
			{
				flag = cell.Lookup[Grid3.Face.Up] != null;
				flag2 = cell.Lookup[Grid3.Face.Down] != null;
				flag3 = cell.Lookup[Grid3.Face.North] != null;
				flag4 = cell.Lookup[Grid3.Face.South] != null;
				flag5 = cell.Lookup[Grid3.Face.East] != null;
				flag6 = cell.Lookup[Grid3.Face.West] != null;
			}
			float num = (flag ? (-0.025f) : 0f);
			num += (flag2 ? 0.025f : 0f);
			float num2 = (flag3 ? (-0.025f) : 0f);
			num2 += (flag4 ? 0.025f : 0f);
			float x = (flag5 ? (-0.025f) : 0f) + (flag6 ? 0.025f : 0f);
			float num3 = 1f - (flag ? 0.05f : 0f);
			num3 -= (flag2 ? 0.05f : 0f);
			float num4 = 1f - (flag3 ? 0.05f : 0f);
			Vector3 vector = new Vector3(z: num4 - (flag4 ? 0.05f : 0f), x: 1f - (flag5 ? 0.05f : 0f) - (flag6 ? 0.05f : 0f), y: num3) * 2f;
			Vector3 vector2 = new Vector3(x, num, num2) * 2f;
			float num5 = Mathf.Clamp01(atmosphere.LiquidVolumeRatio);
			float y = vector.y;
			vector = new Vector3(vector.x, vector.y * num5, vector.z);
			vector2 = new Vector3(vector2.x, vector2.y - (y - vector.y) * 0.5f, vector2.z);
			Write.Matrices[Write.RenderIndex] = Matrix4x4.TRS(atmosphere.WorldPosition + vector2, Quaternion.identity, vector);
			Write.Colors[Write.RenderIndex] = Atmosphere.GetLiquidColor(atmosphere.GasMixture);
			Write.FlowDirections[Write.RenderIndex] = atmosphere.GetSmoothedFlowDirection(GameManager.GameTickSpeedSeconds);
		}
	}
}
