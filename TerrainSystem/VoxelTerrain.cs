using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Assets.Scripts.Voxel;
using Cysharp.Threading.Tasks;
using Rooms;
using TerrainSystem.Lods;
using ThingImport;
using UI.ImGuiUi.Debug;
using UnityEngine;
using UnityEngine.Serialization;

namespace TerrainSystem;

public class VoxelTerrain : MonoBehaviour
{
	[Serializable]
	public class MineableTypeInfo
	{
		[FormerlySerializedAs("Type")]
		public MinableType MinableType;

		[FormerlySerializedAs("Mesh")]
		public Mesh mesh;

		[FormerlySerializedAs("Material")]
		public Material material;

		public MinableVisualiser dummyObject;
	}

	public readonly struct VoxelChangeEvent : ISyncListable
	{
		private readonly Vector3Int _octTreePosition;

		private readonly byte _density;

		public void Serialize(RocketBinaryWriter writer)
		{
			writer.WriteUInt16((ushort)_octTreePosition.x);
			writer.WriteUInt16((ushort)_octTreePosition.y);
			writer.WriteUInt16((ushort)_octTreePosition.z);
			writer.WriteByte(_density);
		}

		private void Execute()
		{
			Octree.SetDensity(_octTreePosition, _density);
			Vector3Int vector3Int = OctreeToWorldSpace(_octTreePosition, VoxelConstants.OriginOffsetInt);
			if (Vein.ShouldReleaseMinables(_density))
			{
				Vein.MineAtPositionClient(vector3Int);
			}
			if (GameManager.GameState != GameState.None)
			{
				LodManager.Instance.DirtyLods(vector3Int, dirtyNeighbours: true);
			}
			ApplyVoxelWorldEffects(vector3Int, RoomChangeSource.VoxelRemove);
		}

		public static VoxelChangeEvent Create(Vector3 positionWorldSpace, byte density)
		{
			Vector3Int vector3Int = WorldToOctreeSpace(positionWorldSpace, VoxelConstants.OriginOffsetInt);
			return new VoxelChangeEvent(new Vector3Int(vector3Int.x, vector3Int.y, vector3Int.z), density);
		}

		private VoxelChangeEvent(Vector3Int octTreePosition, byte density)
		{
			_octTreePosition = octTreePosition;
			_density = density;
		}

		private VoxelChangeEvent(RocketBinaryReader reader)
		{
			_octTreePosition = new Vector3Int(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
			_density = reader.ReadByte();
		}

		public static void Deserialize(RocketBinaryReader reader)
		{
			new VoxelChangeEvent(reader).Execute();
		}
	}

	private enum MinableGenerationState
	{
		None,
		Seeding,
		Generating,
		Deduplicating,
		RenderMinables
	}

	public static readonly SyncList<VoxelChangeEvent> VoxelChangeEvents = new SyncList<VoxelChangeEvent>(VoxelChangeEvent.Deserialize);

	public static VoxelTerrain Instance;

	[FormerlySerializedAs("_mineableTypeInfo")]
	[SerializeField]
	private List<MineableTypeInfo> mineableTypeInfo;

	[SerializeField]
	public Material oreVisualiserMaterial;

	[SerializeField]
	public LavaMesh LavaMesh;

	[Header("Debug")]
	[SerializeField]
	private bool _debugNodes;

	[SerializeField]
	private int _debugNodeLevel;

	[SerializeField]
	private bool _debugSkipEmptyNodes;

	[SerializeField]
	private bool _debugOctreeRaycast;

	private Vector3 _reusableVector;

	public Material TerrainMaterial;

	public LavaObject LavaObjectPrefab;

	public static OctTreeCluster ReadOnlyOctree;

	public static VoxelOctree Octree;

	public static Vector3Int[] LargeGridVoxelOffsets;

	private float _checkSunCooldown;

	private static MinableDrawCallCollection BufferA;

	private static MinableDrawCallCollection BufferB;

	private static object _bufferFlipLock = new object();

	private static bool _isWriteBufferA;

	private const float MAX_RAY_DISTANCE = 64f;

	private const float MAX_RAY_DISTANCE_SQ = 4096f;

	private const byte OCTREE_RAYCAST_DENSITY_THRESHOLD = 230;

	private static int _terrainEditCondition = Animator.StringToHash("TerrainEdit");

	private static List<Grid3> _offsets = new List<Grid3>
	{
		Grid3.zero,
		Grid3.Up,
		Grid3.Down,
		Grid3.East,
		Grid3.West,
		Grid3.North,
		Grid3.South
	};

	private static MinableGenerationState _state = MinableGenerationState.Seeding;

	private static readonly HashSet<Vector3Int> _generateClusterPositions = new HashSet<Vector3Int>(4096);

	public const float MINABLES_SQUARE_DISTANCE = 32f;

	private static ConcurrentQueue<IGenerateMinables> _generateMinablesQueue = new ConcurrentQueue<IGenerateMinables>();

	private static readonly UniqueQueue<MinableRenderJob> _minableRenderJobs = new UniqueQueue<MinableRenderJob>();

	private const float DIRTY_MINABLE_RENDER_DISTANCE = 9216f;

	public static float[] DensityArray = new float[256]
	{
		0f,
		0.00390625f,
		1f / 128f,
		0.01171875f,
		1f / 64f,
		0.01953125f,
		3f / 128f,
		0.02734375f,
		1f / 32f,
		0.03515625f,
		5f / 128f,
		0.04296875f,
		3f / 64f,
		0.05078125f,
		7f / 128f,
		0.05859375f,
		0.0625f,
		0.06640625f,
		9f / 128f,
		0.07421875f,
		5f / 64f,
		0.08203125f,
		11f / 128f,
		0.08984375f,
		3f / 32f,
		0.09765625f,
		13f / 128f,
		0.10546875f,
		7f / 64f,
		0.11328125f,
		15f / 128f,
		0.12109375f,
		0.125f,
		0.12890625f,
		17f / 128f,
		0.13671875f,
		9f / 64f,
		0.14453125f,
		19f / 128f,
		0.15234375f,
		5f / 32f,
		0.16015625f,
		21f / 128f,
		0.16796875f,
		11f / 64f,
		0.17578125f,
		23f / 128f,
		0.18359375f,
		0.1875f,
		0.19140625f,
		25f / 128f,
		0.19921875f,
		13f / 64f,
		0.20703125f,
		27f / 128f,
		0.21484375f,
		7f / 32f,
		0.22265625f,
		29f / 128f,
		0.23046875f,
		15f / 64f,
		0.23828125f,
		31f / 128f,
		0.24609375f,
		0.25f,
		0.25390625f,
		33f / 128f,
		0.26171875f,
		17f / 64f,
		0.26953125f,
		35f / 128f,
		0.27734375f,
		9f / 32f,
		0.28515625f,
		37f / 128f,
		0.29296875f,
		19f / 64f,
		0.30078125f,
		39f / 128f,
		0.30859375f,
		0.3125f,
		0.31640625f,
		41f / 128f,
		0.32421875f,
		21f / 64f,
		0.33203125f,
		43f / 128f,
		0.33984375f,
		11f / 32f,
		0.34765625f,
		45f / 128f,
		0.35546875f,
		23f / 64f,
		0.36328125f,
		47f / 128f,
		0.37109375f,
		0.375f,
		0.37890625f,
		49f / 128f,
		0.38671875f,
		25f / 64f,
		0.39453125f,
		51f / 128f,
		0.40234375f,
		13f / 32f,
		0.41015625f,
		53f / 128f,
		0.41796875f,
		27f / 64f,
		0.42578125f,
		55f / 128f,
		0.43359375f,
		0.4375f,
		0.44140625f,
		57f / 128f,
		0.44921875f,
		29f / 64f,
		0.45703125f,
		59f / 128f,
		0.46484375f,
		15f / 32f,
		0.47265625f,
		61f / 128f,
		0.48046875f,
		31f / 64f,
		0.48828125f,
		63f / 128f,
		0.49609375f,
		0.5f,
		0.50390625f,
		65f / 128f,
		0.51171875f,
		33f / 64f,
		0.51953125f,
		67f / 128f,
		0.52734375f,
		17f / 32f,
		0.53515625f,
		69f / 128f,
		0.54296875f,
		35f / 64f,
		0.55078125f,
		71f / 128f,
		0.55859375f,
		0.5625f,
		0.56640625f,
		73f / 128f,
		0.57421875f,
		37f / 64f,
		0.58203125f,
		75f / 128f,
		0.58984375f,
		19f / 32f,
		0.59765625f,
		77f / 128f,
		0.60546875f,
		39f / 64f,
		0.61328125f,
		79f / 128f,
		0.62109375f,
		0.625f,
		0.62890625f,
		81f / 128f,
		0.63671875f,
		41f / 64f,
		0.64453125f,
		83f / 128f,
		0.65234375f,
		21f / 32f,
		0.66015625f,
		85f / 128f,
		0.66796875f,
		43f / 64f,
		0.67578125f,
		87f / 128f,
		0.68359375f,
		0.6875f,
		0.69140625f,
		89f / 128f,
		0.69921875f,
		45f / 64f,
		0.70703125f,
		91f / 128f,
		0.71484375f,
		23f / 32f,
		0.72265625f,
		93f / 128f,
		0.73046875f,
		47f / 64f,
		0.73828125f,
		95f / 128f,
		0.74609375f,
		0.75f,
		0.75390625f,
		97f / 128f,
		0.76171875f,
		49f / 64f,
		0.76953125f,
		99f / 128f,
		0.77734375f,
		25f / 32f,
		0.78515625f,
		101f / 128f,
		0.79296875f,
		51f / 64f,
		0.80078125f,
		103f / 128f,
		0.80859375f,
		0.8125f,
		0.81640625f,
		105f / 128f,
		0.82421875f,
		53f / 64f,
		0.83203125f,
		107f / 128f,
		0.83984375f,
		27f / 32f,
		0.84765625f,
		109f / 128f,
		0.85546875f,
		55f / 64f,
		0.86328125f,
		111f / 128f,
		0.87109375f,
		0.875f,
		0.87890625f,
		113f / 128f,
		0.88671875f,
		57f / 64f,
		0.89453125f,
		115f / 128f,
		0.90234375f,
		29f / 32f,
		0.91015625f,
		117f / 128f,
		0.91796875f,
		59f / 64f,
		0.92578125f,
		119f / 128f,
		0.93359375f,
		0.9375f,
		0.94140625f,
		121f / 128f,
		0.94921875f,
		61f / 64f,
		0.95703125f,
		123f / 128f,
		0.96484375f,
		31f / 32f,
		0.97265625f,
		125f / 128f,
		0.98046875f,
		63f / 64f,
		0.98828125f,
		127f / 128f,
		1f
	};

	public static int MaxDepth { get; private set; }

	public static bool TerrainIsBlockingSun { get; private set; }

	public static MinableDrawCallCollection Write
	{
		get
		{
			lock (_bufferFlipLock)
			{
				return _isWriteBufferA ? BufferA : BufferB;
			}
		}
	}

	public static MinableDrawCallCollection Read
	{
		get
		{
			lock (_bufferFlipLock)
			{
				return _isWriteBufferA ? BufferB : BufferA;
			}
		}
	}

	public static void SetMaxDepth(IOctTree masterTree)
	{
		MaxDepth = masterTree.MaxDepth;
	}

	private void Start()
	{
		Instance = this;
	}

	public static void Initialize()
	{
		WorkerCollections.Initialize();
		CreateLargeGridVoxelOffsets();
		BufferA = new MinableDrawCallCollection();
		BufferB = new MinableDrawCallCollection();
	}

	public static async UniTaskVoid UpdateRenderDistance()
	{
		if (GameManager.GameState != GameState.None)
		{
			WorkerCollections.MinableRenderWorkers.AbortAll();
			while (WorkerCollections.MinableRenderWorkers.IsAnyWorking())
			{
				await UniTask.Yield();
			}
			DirtyAllMinables(InventoryManager.ParentPosition).Forget();
		}
	}

	private void OnValidate()
	{
		Instance = this;
	}

	public static async UniTask LoadTerrain(TerrainSettings terrainSettings, bool newGame)
	{
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenLoadingTerrain);
		ClearAll();
		VoxelConstants.SetWorldSizeOffset(terrainSettings.Size);
		StreamingAssetLoader.GetExistingDirectory(terrainSettings.RelativePath, out var fullPath);
		ReadOnlyOctree = new OctTreeCluster(terrainSettings.Size);
		await ReadOnlyOctree.Deserialize(fullPath);
		Octree = new VoxelOctree(ReadOnlyOctree);
		if (newGame)
		{
			Octree.PrepareOctree();
		}
		if (terrainSettings.MaterialSettings != null)
		{
			TerrainShaderScript.ApplyMaterialSettings(Instance.TerrainMaterial, terrainSettings);
			TerrainShaderScript.SetTerrainDetail(Settings.CurrentData.TerrainDetail);
		}
		if (terrainSettings.TerrainProps != null)
		{
			foreach (LavaLakeProp lavaLake in terrainSettings.TerrainProps.LavaLakeList)
			{
				LavaObject lavaObject = UnityEngine.Object.Instantiate(Instance.LavaObjectPrefab);
				lavaObject.Initialize();
				lavaObject.transform.position = lavaLake.Position.ToVector3();
				lavaObject.transform.rotation = lavaLake.Rotation.ToQuaternion();
				lavaObject.transform.localScale = lavaLake.Scale.ToVector3();
			}
		}
		CreateLavaMesh(terrainSettings.LavaData);
	}

	public static void CreateLavaMesh(LavaData lavaData)
	{
		if (lavaData != null)
		{
			Mesh mesh = MeshCreatorHelper.CreateLavaMesh(lavaData);
			Instance.LavaMesh.SetActive(active: true);
			Instance.LavaMesh.SetMesh(mesh);
			Instance.LavaMesh.SetData(lavaData);
			Instance.LavaMesh.Transform.position = -VoxelConstants.OriginOffsetInt;
		}
		else
		{
			Instance.LavaMesh.Clear();
		}
	}

	private static void CreateLargeGridVoxelOffsets()
	{
		int num = 0;
		LargeGridVoxelOffsets = new Vector3Int[8];
		for (int i = 0; (float)i < 2f; i++)
		{
			for (int j = 0; (float)j < 2f; j++)
			{
				for (int k = 0; (float)k < 2f; k++)
				{
					LargeGridVoxelOffsets[num] = new Vector3Int(i, j, k);
					num++;
				}
			}
		}
	}

	private void OnDestroy()
	{
		WorkerCollections.Destroy();
		ReadOnlyOctree?.Clear();
	}

	private void Update()
	{
		if (GameManager.GameState == GameState.Running)
		{
			if (!GameManager.IsBatchMode)
			{
				Read.RenderMinables();
				CheckSunVector();
			}
			while (RunMinablesGeneration())
			{
			}
			DebugDraw();
		}
	}

	public static void UpdateCanAirPass(Vector3 position)
	{
		Grid3 localGrid = position.ToGrid();
		Vector3[] allDirections = RocketGrid.GridVoxel.AllDirections;
		foreach (Vector3 vector in allDirections)
		{
			if (GetDensityWorldSpace(position + vector) >= 0.49803922f)
			{
				GridController.World.UpdateVoxelAirState(localGrid, canAirPass: false);
				break;
			}
			GridController.World.UpdateVoxelAirState(localGrid, canAirPass: true);
		}
	}

	private void CheckSunVector()
	{
		if (_checkSunCooldown > 0f)
		{
			_checkSunCooldown -= Time.deltaTime;
			return;
		}
		_checkSunCooldown = 0.2f;
		Transform mainCameraTransform = CameraController.Instance.MainCameraTransform;
		TerrainIsBlockingSun = Instance.OctreeRaycast(mainCameraTransform.position, OrbitalSimulation.WorldSunVector.normalized);
	}

	public static PartnerNode GetPartnerNode(Node node)
	{
		return ReadOnlyOctree.GetPartnerNode(node);
	}

	private void DebugDraw()
	{
		if (_debugNodes && Octree != null)
		{
			Vector3 point = InputHelpers.GetCameraRay().GetPoint(2f);
			DrawNodeDensity(point, 1);
		}
	}

	public bool OctreeRaycast(Vector3 origin, Vector3 direction)
	{
		if (Octree == null)
		{
			return false;
		}
		int num = 500;
		int num2 = 0;
		Vector3 startOrigin = origin;
		float prevOffset = 0f;
		while (num2 < num)
		{
			num2++;
			Vector3Int vector3Int = WorldToOctreeSpace(origin, VoxelConstants.OriginOffsetInt);
			if (Octree.OutsideBounds(vector3Int))
			{
				return false;
			}
			NodeInfo nodeInfo = Get(vector3Int);
			if (nodeInfo.Density > 230)
			{
				return true;
			}
			if (!OctreeRaycastWork(ref origin, direction, nodeInfo, vector3Int, startOrigin, ref prevOffset))
			{
				return false;
			}
		}
		return false;
	}

	public bool OctreeRaycast(Vector3 origin, Vector3 direction, out Vector3? hitPoint)
	{
		hitPoint = null;
		if (Octree == null)
		{
			return false;
		}
		int num = 500;
		int num2 = 0;
		Vector3 startOrigin = origin;
		float prevOffset = 0f;
		while (num2 < num)
		{
			num2++;
			Vector3Int vector3Int = WorldToOctreeSpace(origin, VoxelConstants.OriginOffsetInt);
			if (Octree.OutsideBounds(vector3Int))
			{
				return false;
			}
			NodeInfo nodeInfo = Get(vector3Int);
			if (nodeInfo.Density > 230)
			{
				hitPoint = OctreeToWorldSpace(vector3Int);
				return true;
			}
			if (!OctreeRaycastWork(ref origin, direction, nodeInfo, vector3Int, startOrigin, ref prevOffset))
			{
				return false;
			}
		}
		return false;
	}

	private bool OctreeRaycastWork(ref Vector3 origin, Vector3 direction, NodeInfo nodeInfo, Vector3Int octreePos, Vector3 startOrigin, ref float prevOffset)
	{
		int size = GetSize(nodeInfo.Depth);
		Vector3Int vector3Int = OctreeToWorldSpace(FloorToNodeSize(octreePos, size), VoxelConstants.OriginOffsetInt);
		if (!GetNodeBoxIntersection(size, vector3Int, origin, direction, out var exitPoint))
		{
			return false;
		}
		Vector3 vector = origin;
		origin = exitPoint + direction * 0.01f;
		if (Vector3.SqrMagnitude(origin - vector) > 4096f)
		{
			origin = Vector3.Lerp(vector, origin, (origin - vector).magnitude / 64f);
		}
		float curvatureOffset = VoxelConstants.GetCurvatureOffset(startOrigin, origin);
		Vector3 vector2 = new Vector3(0f, curvatureOffset - prevOffset, 0f);
		origin -= vector2;
		prevOffset = curvatureOffset;
		return true;
	}

	private static bool GetNodeBoxIntersection(float size, Vector3 worldPos, Vector3 rayOrigin, Vector3 rayDirection, out Vector3 exitPoint)
	{
		Vector3 vector = Vector3.one * size / 2f;
		Vector3 vector2 = worldPos + vector;
		Vector3 vector3 = rayOrigin - vector2;
		Vector2 vector4 = RocketMath.BoxIntersection(vector3, rayDirection, vector);
		if (vector4.y == -1f)
		{
			exitPoint = Vector3.zero;
			return false;
		}
		exitPoint = vector3 + vector4.y * rayDirection + vector2;
		return true;
	}

	private void DrawNodeDensity(Vector3 worldPos, int id)
	{
		INode node_DEBUG = GetNode_DEBUG(WorldToOctreeSpace(worldPos, VoxelConstants.OriginOffsetInt), 1);
		byte value = node_DEBUG?.Density ?? 0;
		string text = ((node_DEBUG == null || !node_DEBUG.IsValid) ? "null" : node_DEBUG.NodeType.ToString());
		Vector3 vector = worldPos.FloorToInt() + VoxelConstants.TerrainMeshOffset;
		ImGuiDebugHelper.DrawWireSphere(vector, 0.05f, ImGuiColor.Integer.White);
		ImGuiDebugHelper.DrawText(vector, "Type: " + text + " Density: " + StringManager.Get(value), id);
		ImGuiDebugHelper.DrawLine(vector, vector + Vector3.forward, ImGuiColor.Integer.Blue);
		ImGuiDebugHelper.DrawLine(vector, vector + Vector3.up, ImGuiColor.Integer.Green);
		ImGuiDebugHelper.DrawLine(vector, vector + Vector3.right, ImGuiColor.Integer.Red);
	}

	public static bool GetMineableInfo(MinableType type, out MineableTypeInfo info)
	{
		foreach (MineableTypeInfo item in Instance.mineableTypeInfo)
		{
			if (item.MinableType == type)
			{
				info = item;
				return true;
			}
		}
		info = null;
		return false;
	}

	public static int OrePrefabHash(MinableType type)
	{
		return type switch
		{
			MinableType.None => PrefabHashmap.Invalid, 
			MinableType.Stone => PrefabHashmap.Invalid, 
			MinableType.Iron => PrefabHashmap.ItemIronOre, 
			MinableType.Ice => PrefabHashmap.ItemIce, 
			MinableType.Gold => PrefabHashmap.ItemGoldOre, 
			MinableType.Coal => PrefabHashmap.ItemCoalOre, 
			MinableType.Copper => PrefabHashmap.ItemCopperOre, 
			MinableType.Uranium => PrefabHashmap.ItemUraniumOre, 
			MinableType.Nickel => PrefabHashmap.ItemNickelOre, 
			MinableType.Lead => PrefabHashmap.ItemLeadOre, 
			MinableType.Silver => PrefabHashmap.ItemSilverOre, 
			MinableType.Silicon => PrefabHashmap.ItemSiliconOre, 
			MinableType.Oxite => PrefabHashmap.ItemOxite, 
			MinableType.Volatiles => PrefabHashmap.ItemVolatiles, 
			MinableType.GeyserHydrogen => PrefabHashmap.Invalid, 
			MinableType.Cobalt => PrefabHashmap.ItemCobaltOre, 
			MinableType.Nitrice => PrefabHashmap.ItemNitrice, 
			MinableType.LastMinable => PrefabHashmap.Invalid, 
			MinableType.Crater => PrefabHashmap.Invalid, 
			MinableType.Bedrock => PrefabHashmap.Invalid, 
			_ => PrefabHashmap.Invalid, 
		};
	}

	public static void GetNodesAtDepth(Node node, List<Node> nodesAtLevel, int depth)
	{
		if (node.Children == null)
		{
			nodesAtLevel.Add(node);
			return;
		}
		Node[] children = node.Children;
		foreach (Node node2 in children)
		{
			if (node2.Depth == depth)
			{
				nodesAtLevel.Add(node2);
			}
			else
			{
				GetNodesAtDepth(node2, nodesAtLevel, depth);
			}
		}
	}

	public static void ClearAll()
	{
		LodMeshWorker.AbortAll();
		TerrainIsBlockingSun = false;
		BufferA.ClearAll();
		BufferB.ClearAll();
		LodObjectCache.ClearAll();
		LodManager.Instance.Clear();
		LavaObject.ClearAll();
		Instance.LavaMesh.Clear();
		Instance.LavaMesh.SetActive(active: false);
		ResetDummyObjects();
		Vein.ClearAll();
		_generateClusterPositions.Clear();
		_generateMinablesQueue.Clear();
		_minableRenderJobs.Clear();
		_state = MinableGenerationState.Seeding;
		Octree?.Clear();
		ReadOnlyOctree?.Clear();
		Octree = null;
		ReadOnlyOctree = null;
	}

	public static Vector3Int FloorToNodeSize(Vector3 point, int size)
	{
		return (point / size).FloorToInt() * size;
	}

	public static Vector3Int WorldToOctreeSpace(Vector3 position, Vector3Int originOffset)
	{
		return WorldToOctreeSpace(position.FloorToInt(), originOffset);
	}

	public static Vector3 OctreeToWorldSpace(Vector3 position, Vector3Int originOffset)
	{
		return position - originOffset;
	}

	public static Vector3Int WorldToOctreeSpaceClamped(Vector3Int position)
	{
		Vector3Int vector3Int = position + VoxelConstants.OriginOffsetInt;
		return new Vector3Int(Mathf.Clamp(vector3Int.x, 0, VoxelConstants.Size), Mathf.Clamp(vector3Int.y, 0, VoxelConstants.Size), Mathf.Clamp(vector3Int.z, 0, VoxelConstants.Size));
	}

	public static Vector3Int WorldToOctreeSpace(Vector3Int position)
	{
		return position + VoxelConstants.OriginOffsetInt;
	}

	public static Vector3Int WorldToOctreeSpace(Vector3Int position, Vector3Int originOffset)
	{
		return position + originOffset;
	}

	public static Vector3Int OctreeToWorldSpace(Vector3Int position)
	{
		return OctreeToWorldSpace(position, VoxelConstants.OriginOffsetInt);
	}

	public static Vector3 OctreeToWorldSpace(Vector3 position)
	{
		return OctreeToWorldSpace(position, VoxelConstants.OriginOffsetInt);
	}

	public static Vector3Int OctreeToWorldSpace(Vector3Int position, Vector3Int originOffset)
	{
		return position - originOffset;
	}

	public static float GetDensityWorldSpace(Vector3 worldPosition)
	{
		return Get(WorldToOctreeSpace(worldPosition, VoxelConstants.OriginOffsetInt)).DensityAsFloat();
	}

	public static NodeInfo GetNodeInfoWorldSpace(Vector3 worldPosition)
	{
		Vector3Int vector3Int = WorldToOctreeSpace(worldPosition, VoxelConstants.OriginOffsetInt);
		return Get(vector3Int.x, vector3Int.y, vector3Int.z, (sbyte)MaxDepth);
	}

	public static INode GetNodeWorldSpace_DEBUG(Vector3 worldPosition, int size = 1)
	{
		return GetNode_DEBUG(WorldToOctreeSpace(worldPosition, VoxelConstants.OriginOffsetInt), size);
	}

	public static float GetDensityWorldSpace(Vector3Int worldPositionInt)
	{
		return Get(WorldToOctreeSpace(worldPositionInt, VoxelConstants.OriginOffsetInt)).DensityAsFloat();
	}

	public static byte GetReadonlyDensityWorldSpace(Vector3Int worldPositionInt)
	{
		return GetReadonlyDensityOctreeSpace(WorldToOctreeSpace(worldPositionInt, VoxelConstants.OriginOffsetInt));
	}

	public static float GetDensityAtSize(Vector3 worldPosition, int size)
	{
		if (Octree == null)
		{
			return 0f;
		}
		size = Mathf.Max(1, size);
		Vector3Int vector3Int = WorldToOctreeSpace(worldPosition, VoxelConstants.OriginOffsetInt);
		sbyte depth = ReadonlyVoxelOctree.GetDepth(size);
		Node foundNode;
		NodeInfo nodeInfo = Octree.Get(vector3Int, out foundNode, depth);
		if (foundNode != null && foundNode.IsModified)
		{
			return nodeInfo.DensityAsFloat();
		}
		return ReadOnlyOctree.Get(vector3Int, depth).DensityAsFloat();
	}

	public static int GetSize(int depth)
	{
		return (int)Mathf.Pow(2f, MaxDepth - depth);
	}

	public static sbyte GetDepth(int size)
	{
		return (sbyte)(MaxDepth - (int)Mathf.Log(size, 2f));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DensityToFloat(byte density)
	{
		return DensityArray[density];
	}

	public static byte DensityToByte(float density)
	{
		return (byte)(Mathf.Clamp01(density) * 255f);
	}

	public static void SetDensityWorldSpace(Vector3 worldPosition, float density, RoomChangeSource source = RoomChangeSource.VoxelRemove, bool dirtyLods = true, bool setNodeType = false, VoxelNodeType voxelType = VoxelNodeType.None)
	{
		if (Octree != null && !LodManager.IsAtOrUnderBedrock(worldPosition))
		{
			byte density2 = DensityToByte(density);
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				VoxelChangeEvents.Add(VoxelChangeEvent.Create(worldPosition, density2));
			}
			Octree.SetDensity(WorldToOctreeSpace(worldPosition, VoxelConstants.OriginOffsetInt), density2, setNodeType, voxelType);
			UpdateCanAirPass(worldPosition);
			if (GameManager.GameState != GameState.None && dirtyLods)
			{
				LodManager.Instance.DirtyLods(worldPosition, dirtyNeighbours: true);
			}
			if (WorldSetting.Current.StartConditionData.IdHash != _terrainEditCondition)
			{
				ApplyVoxelWorldEffects(worldPosition, source);
			}
		}
	}

	private static INode GetNode_DEBUG(Vector3Int nodePositionOctreeSpaceInt, int size)
	{
		if (Octree == null)
		{
			return null;
		}
		Octree.Get(nodePositionOctreeSpaceInt.x, nodePositionOctreeSpaceInt.y, nodePositionOctreeSpaceInt.z, out var foundNode, out var foundPos, GetDepth(size));
		if (foundNode != null && foundNode.IsModified)
		{
			return foundNode;
		}
		return ReadOnlyOctree.GetNode(foundNode.PartnerNode, foundPos, GetDepth(size));
	}

	private static NodeInfo Get(Vector3Int nodePositionOctreeSpaceInt)
	{
		if (Octree == null)
		{
			return NodeInfo.Invalid;
		}
		Node foundNode;
		Vector3Int foundPosition;
		NodeInfo result = Octree.Get(nodePositionOctreeSpaceInt, out foundNode, out foundPosition);
		if (foundNode != null && foundNode.IsModified)
		{
			return result;
		}
		return ReadOnlyOctree.Get(foundNode.PartnerNode, foundPosition, (sbyte)MaxDepth);
	}

	private static byte GetReadonlyDensityOctreeSpace(Vector3Int nodePositionOctreeSpaceInt)
	{
		return ReadOnlyOctree.Get(nodePositionOctreeSpaceInt, (sbyte)MaxDepth).Density;
	}

	public static VoxelNodeType GetReadonlyNodeTypeWorldSpace(Vector3 position, sbyte desiredDepth)
	{
		Vector3Int vector3Int = WorldToOctreeSpace(position.FloorToInt());
		return GetReadonlyNodeTypeOctreeSpace(vector3Int.x, vector3Int.y, vector3Int.z, desiredDepth);
	}

	private static VoxelNodeType GetReadonlyNodeTypeOctreeSpace(int x, int y, int z, sbyte desiredDepth)
	{
		return ReadOnlyOctree.Get(x, y, z, desiredDepth).NodeType;
	}

	public static NodeInfo Get(int x, int y, int z, sbyte desiredDepth)
	{
		if (Octree == null)
		{
			return NodeInfo.Invalid;
		}
		Node foundNode;
		Vector3Int foundPos;
		NodeInfo result = Octree.Get(x, y, z, out foundNode, out foundPos, desiredDepth);
		if (foundNode != null && foundNode.IsModified)
		{
			return result;
		}
		if (foundNode == null)
		{
			return ReadOnlyOctree.Get(x, y, z, desiredDepth);
		}
		return ReadOnlyOctree.Get(foundNode.PartnerNode, foundPos, desiredDepth);
	}

	public static void ApplyVoxelWorldEffects(Vector3 worldVoxelPosition, RoomChangeSource source)
	{
		Grid3 grid = GridController.WorldToLargeGridCenter(worldVoxelPosition);
		switch (source)
		{
		case RoomChangeSource.VoxelRemove:
			AtmosphericEventInstance.CreateEmpty(new WorldGrid(grid));
			break;
		case RoomChangeSource.VoxelAdd:
			AtmosphericEventInstance.CloneGlobal(new WorldGrid(grid), MoleEnergy.Zero);
			break;
		}
		foreach (Grid3 offset in _offsets)
		{
			Grid3 grid2 = grid + offset;
			RoomEvaluator.Instance.CheckGrid(grid2);
			AtmosphericsController.World.CheckAtmosphereConnections(new WorldGrid(grid2));
		}
	}

	public static void Serialize(Stream memoryStream)
	{
		Octree.SerializeDeltaTerrain(memoryStream);
	}

	public static bool Deserialize(string filePath)
	{
		return Octree.DeserializeDeltaTerrain(filePath);
	}

	public static void SerializeOnJoin(RocketBinaryWriter writer)
	{
		Octree.SerializeOnJoin(writer);
	}

	public static void DeSerializeOnJoin(RocketBinaryReader reader)
	{
		Octree.DeserializeOnJoin(reader);
	}

	public static void PrepareArea(Vector3Int position, int size)
	{
		Octree.CopyArea(position, size);
	}

	public static MinableVisualiser GetDummyObject(MinableType type)
	{
		foreach (MineableTypeInfo item in Instance.mineableTypeInfo)
		{
			if (item.MinableType == type)
			{
				return item.dummyObject;
			}
		}
		return null;
	}

	public static void ResetDummyObjects()
	{
		foreach (MineableTypeInfo item in Instance.mineableTypeInfo)
		{
			item.dummyObject.Reset();
		}
	}

	private static bool RunMinablesGeneration()
	{
		return _state switch
		{
			MinableGenerationState.Seeding => SeedMinables(), 
			MinableGenerationState.Generating => GenerateMinables(), 
			MinableGenerationState.Deduplicating => DeduplicateMinables(), 
			MinableGenerationState.RenderMinables => RefreshMinableRenderBuffers(), 
			_ => throw new NotImplementedException(), 
		};
	}

	public static bool SeedMinables()
	{
		if (_generateMinablesQueue.Count > 0 && _generateClusterPositions.Count == 0)
		{
			ClusterSeeding();
			if (_generateClusterPositions.Count > 0)
			{
				VeinGenerationWorker.ExecuteAll();
			}
			return false;
		}
		_state = MinableGenerationState.Generating;
		return true;
	}

	private static bool GenerateMinables()
	{
		if (VeinGenerationWorker.IsAnyWorking())
		{
			return false;
		}
		_state = MinableGenerationState.Deduplicating;
		return true;
	}

	private static bool DeduplicateMinables()
	{
		if (VeinDeduplicationWorker.IsAnyWorking())
		{
			return false;
		}
		if (_generateClusterPositions.Count == 0)
		{
			_state = MinableGenerationState.RenderMinables;
			return true;
		}
		foreach (Vector3Int generateClusterPosition in _generateClusterPositions)
		{
			if (VeinCluster.GetCluster(generateClusterPosition, out VeinCluster cluster) && cluster.Status == VeinClusterStatus.Generating)
			{
				cluster.Status = VeinClusterStatus.Active;
				if (cluster.HasVeins())
				{
					VeinDeduplicationWorker.Assign(cluster);
				}
			}
		}
		VeinDeduplicationWorker.ExecuteAll();
		_generateClusterPositions.Clear();
		return false;
	}

	public static void ClusterSeeding()
	{
		IGenerateMinables result;
		while (_generateMinablesQueue.TryDequeue(out result))
		{
			Vector3Int vector3Int = result.GeneratePosition.FloorToInt();
			vector3Int /= 32;
			vector3Int *= 32;
			for (int i = -result.MinablesGenerationRange.x; i < result.MinablesGenerationRange.x; i += 32)
			{
				for (int j = -result.MinablesGenerationRange.y; j < result.MinablesGenerationRange.y; j += 32)
				{
					for (int k = -result.MinablesGenerationRange.z; k < result.MinablesGenerationRange.z; k += 32)
					{
						_generateClusterPositions.Add(new Vector3Int(vector3Int.x + i, vector3Int.y + j, vector3Int.z + k));
					}
				}
			}
		}
		foreach (Vector3Int generateClusterPosition in _generateClusterPositions)
		{
			VeinCluster.Generate(generateClusterPosition);
			VeinGenerationWorker.IncrementNextIndex();
		}
	}

	public static void GenerateMinables(IGenerateMinables iGenerateMinables)
	{
		if (RocketMath.SquareDistanceComparison(iGenerateMinables.GeneratePosition, iGenerateMinables.PreviousMinableRequestPosition, 32f) > 0f)
		{
			EnqueueMinableGeneration(iGenerateMinables);
			iGenerateMinables.PreviousMinableRequestPosition = iGenerateMinables.GeneratePosition;
		}
	}

	public static void EnqueueMinableGeneration(IGenerateMinables iGenerateMinables)
	{
		_generateMinablesQueue.Enqueue(iGenerateMinables);
	}

	public static void QueueMinableRenderRefresh(MinableRenderJob job)
	{
		_minableRenderJobs.Enqueue(job);
	}

	public static bool RefreshMinableRenderBuffers()
	{
		if (MinableRenderWorker.IsAnyWorking())
		{
			return false;
		}
		if (GameManager.IsBatchMode || InventoryManager.Parent == null)
		{
			_state = MinableGenerationState.Seeding;
			return false;
		}
		if (_minableRenderJobs.Count == 0 || InventoryManager.Parent == null)
		{
			_state = MinableGenerationState.Seeding;
			lock (_bufferFlipLock)
			{
				if (Write.IsWriting)
				{
					Write.IsWriting = false;
					_isWriteBufferA = !_isWriteBufferA;
				}
			}
			return false;
		}
		if (_minableRenderJobs.Count > 0)
		{
			lock (_bufferFlipLock)
			{
				Write.IsWriting = true;
			}
			while (_minableRenderJobs.Count > 0)
			{
				MinableRenderWorker.Assign(_minableRenderJobs.Dequeue());
			}
			MinableRenderWorker.ExecuteAll();
		}
		return false;
	}

	public static async UniTaskVoid DirtyAllMinables(Vector3 position)
	{
		if (Vector3.SqrMagnitude(InventoryManager.ParentPosition - position) < 9216f)
		{
			while (Write.IsWriting)
			{
				await UniTask.Yield();
			}
			Write.RefreshAll();
		}
	}

	private static List<Vein> GetVeinsInBounds(Vector3 position, float boundsSize)
	{
		float num = boundsSize * 0.5f;
		BoundsInt boundsInt = new BoundsInt((int)Math.Floor(position.x - num), (int)Math.Floor(position.y - num), (int)Math.Floor(position.z - num), (int)boundsSize + 1, (int)boundsSize + 1, (int)boundsSize + 1);
		List<Vein> veins = new List<Vein>();
		Vein.GetVeinsInBounds(boundsInt, ref veins);
		return veins;
	}

	private static List<Vein> GetVeinsInBounds(Vector3 position, float boundsSize, out BoundsInt bounds)
	{
		float num = boundsSize * 0.5f;
		bounds = new BoundsInt((int)Math.Floor(position.x - num), (int)Math.Floor(position.y - num), (int)Math.Floor(position.z - num), (int)boundsSize + 1, (int)boundsSize + 1, (int)boundsSize + 1);
		List<Vein> veins = new List<Vein>();
		Vein.GetVeinsInBounds(bounds, ref veins);
		return veins;
	}

	public static int GetNumberOfMinablesNearSurface(Vector3 position, float boundsSize, int maxDepthFromSurface)
	{
		int num = 0;
		foreach (Vein veinsInBound in GetVeinsInBounds(position, boundsSize))
		{
			num += veinsInBound.GetNumberOfReachableMinables(maxDepthFromSurface);
		}
		return num;
	}

	public static void GetAimeeMinableQueue(Vector3 position, float boundsSize, List<TargetMinableData> queue, int mineDepth)
	{
		BoundsInt bounds;
		foreach (Vein veinsInBound in GetVeinsInBounds(position, boundsSize, out bounds))
		{
			veinsInBound.GetAimeeMinables(queue, mineDepth, bounds);
		}
	}

	public static bool IsChecksumValid(int[] worldDataTerrainChunkChecksums)
	{
		if (worldDataTerrainChunkChecksums == null)
		{
			return false;
		}
		for (int i = 0; i < worldDataTerrainChunkChecksums.Length; i++)
		{
			if (worldDataTerrainChunkChecksums[i] != ReadOnlyOctree.Octrees[i].CheckSum)
			{
				return false;
			}
		}
		return true;
	}

	public static uint GetDebugColor(VoxelNodeType nodeType)
	{
		if ((nodeType & VoxelNodeType.Crust) != VoxelNodeType.None)
		{
			return ImGuiColor.Integer.Yellow;
		}
		if ((nodeType & VoxelNodeType.Dirt) != VoxelNodeType.None)
		{
			return ImGuiColor.Integer.Green;
		}
		return ImGuiColor.Integer.Grey;
	}

	public void SaveLavaLakes()
	{
		ConsoleWindow.Print("Saving Lava Lakes...", ConsoleColor.White, clearLine: false, aged: false);
		List<LavaObject> allLavaObjects = LavaObject.AllLavaObjects;
		TerrainProps terrainProps = new TerrainProps
		{
			LavaLakeList = new List<LavaLakeProp>()
		};
		foreach (LavaObject item2 in allLavaObjects)
		{
			LavaLakeProp item = new LavaLakeProp
			{
				Position = new Vector3Reference(item2.PivotTransform.position),
				Rotation = new Vector3Reference(item2.PivotTransform.eulerAngles),
				Scale = new Vector3Reference(item2.PivotTransform.localScale)
			};
			terrainProps.LavaLakeList.Add(item);
		}
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(TerrainProps));
		using StreamWriter textWriter = new StreamWriter("Assets/TerrainProps.xml");
		xmlSerializer.Serialize(textWriter, terrainProps);
		ConsoleWindow.Print("Lava Lakes saved to Assets/TerrainProps.xml", ConsoleColor.White, clearLine: false, aged: false);
	}

	public static async Task InitialiseMinablesOnLoad()
	{
		ConsoleWindow.Print("Init minables");
		Stopwatch sw = new Stopwatch();
		sw.Start();
		foreach (Human allHuman in Human.AllHumans)
		{
			EnqueueMinableGeneration(allHuman);
			allHuman.PreviousMinableRequestPosition = allHuman.transform.position;
		}
		SeedMinables();
		while (!GenerateMinables())
		{
			await UniTask.Yield();
		}
		DeduplicateMinables();
		while (VeinDeduplicationWorker.IsAnyWorking())
		{
			await UniTask.Yield();
		}
		RefreshMinableRenderBuffers();
		ConsoleWindow.Print($"Init minables completed in {(float)sw.ElapsedMilliseconds / 1000f}s");
	}
}
