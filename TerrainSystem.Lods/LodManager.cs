using System.Collections.Generic;
using System.Diagnostics;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TerrainSystem.Lods;

public class LodManager : GameBase
{
	private enum JobState
	{
		Unassigned,
		RemoveRequesters,
		PrepareRequest,
		WaitForRequestJobs,
		Request,
		Prepare,
		WaitForJobs,
		Apply,
		Delete
	}

	public readonly struct LodRequest(Vector3Int index, int level, ILodRequester requester)
	{
		public readonly Vector3Int Index = index;

		public readonly int Level = level;

		public readonly ILodRequester Requester = requester;
	}

	public static LodManager Instance;

	[SerializeField]
	private LodMeshRenderer lodMeshRenderer;

	[SerializeField]
	private LodMeshRendererWithCollision lodMeshRendererWithCollision;

	[Header("Debug")]
	[SerializeField]
	private bool _debug;

	[SerializeField]
	private int _debugLevel;

	[SerializeField]
	private Vector3Int _debugLodObject;

	public static LodInfo LodHighQuality = new LodInfo(8, new int[6] { 10, 10, 5, 5, 4, 8 });

	public static LodInfo LodMediumQuality = new LodInfo(8, new int[6] { 8, 8, 4, 4, 4, 8 });

	public static LodInfo LodLowQuality = new LodInfo(8, new int[6] { 5, 5, 3, 3, 3, 8 });

	public static LodInfo PlayerLodInfo;

	public static LodInfo ThingLodInfo = new LodInfo(8, 1);

	private LodMeshCache[] _lodMeshCaches;

	private static Queue<LodObject> _updateLodQueue = new Queue<LodObject>(20000);

	private static Queue<LodObject> _deleteLodQueue = new Queue<LodObject>(20000);

	private static Queue<ILodRequester> _requestersToRemove = new Queue<ILodRequester>(100);

	private static object _requestersToUpdateLock = new object();

	private static Queue<ILodRequester> _requestersToUpdate = new Queue<ILodRequester>(500);

	private static HashSet<Vector3Int>[] _updatedLodIndices;

	public const int LOD_START_SIZE = 8;

	public const int LOD_LEVEL_COUNT = 6;

	public static int[] MeshesPerLevel = new int[6] { 4000, 2000, 220, 500, 500, 500 };

	private static Vector3Int[][] LodNeighbourOffsets;

	private static Vector3Int[] InnerLodOffsets;

	private static JobState _jobState = JobState.RemoveRequesters;

	private static float TERRAIN_MAIN_THREAD_BUDGET_MS = 3f;

	private static bool IsRunningOnTakeControl = false;

	private Stopwatch _JobStopWatch = new Stopwatch();

	private static object AddRequestsLock = new object();

	private static List<LodRequest> _toAdd = new List<LodRequest>(1024);

	private static List<LodRequest> _toRemove = new List<LodRequest>(1024);

	private static readonly HashSet<long> _alreadyProcessedThings = new HashSet<long>(64);

	public bool AwaitingTerrainGeneration
	{
		get
		{
			lock (_requestersToUpdateLock)
			{
				return _requestersToUpdate.Count > 0 || LodMeshWorker.IsAnyWorking() || LodObject.FinishedQueue.Count > 0 || TerrainColliderBaker.HasOutstandingBakes;
			}
		}
	}

	public Vector2 TerrainHeight => new Vector2(0f, 1023f);

	private void Start()
	{
		Instance = this;
		InitializeArrays();
		InitializeCaches();
		CreateLodNeighbourOffsets();
		CreateInnerLodOffsets();
	}

	public static void UpdateRenderDistance()
	{
		PlayerLodInfo = Settings.CurrentData.TerrainDistance switch
		{
			"Low" => LodLowQuality, 
			"Medium" => LodMediumQuality, 
			"High" => LodHighQuality, 
			_ => LodHighQuality, 
		};
		if (GameManager.GameState != GameState.None && (bool)InventoryManager.ParentBrain.RootParent)
		{
			DynamicThing asDynamicThing = InventoryManager.ParentBrain.RootParent.AsDynamicThing;
			EnqueueRequesterToUpdate(asDynamicThing);
			asDynamicThing.PreviousLodRequestPosition = asDynamicThing.CenterPosition;
		}
	}

	private void InitializeArrays()
	{
		_updatedLodIndices = new HashSet<Vector3Int>[6];
		for (int i = 0; i < 6; i++)
		{
			_updatedLodIndices[i] = new HashSet<Vector3Int>();
		}
	}

	private void InitializeCaches()
	{
		_lodMeshCaches = new LodMeshCache[6];
		for (int i = 0; i < 6; i++)
		{
			LodMeshRenderer lodMeshRendererPrefab = ((i == 0) ? lodMeshRendererWithCollision : lodMeshRenderer);
			_lodMeshCaches[i] = new LodMeshCache(lodMeshRendererPrefab, i);
			_lodMeshCaches[i].PrePopulate(MeshesPerLevel[i]);
		}
	}

	public (int total, int active, int deactivated, int uninitialised) LodMeshCacheCounts(int level)
	{
		return _lodMeshCaches[level].GetCounts();
	}

	private void CreateLodNeighbourOffsets()
	{
		LodNeighbourOffsets = new Vector3Int[6][];
		for (int i = 0; i < 6; i++)
		{
			LodNeighbourOffsets[i] = new Vector3Int[27];
			int num = 8 * (1 << i);
			int num2 = 0;
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					for (int l = -1; l <= 1; l++)
					{
						LodNeighbourOffsets[i][num2] = new Vector3Int(j, k, l) * num;
						num2++;
					}
				}
			}
		}
	}

	private void CreateInnerLodOffsets()
	{
		InnerLodOffsets = new Vector3Int[7];
		int num = 0;
		for (int i = 0; i <= 1; i++)
		{
			for (int j = 0; j <= 1; j++)
			{
				for (int k = 0; k <= 1; k++)
				{
					if (i != 0 || j != 0 || k != 0)
					{
						InnerLodOffsets[num] = new Vector3Int(i, j, k);
						num++;
					}
				}
			}
		}
	}

	private void LateUpdate()
	{
		TerrainColliderBaker.MainThreadUpdate();
		if (GameManager.GameState == GameState.Running)
		{
			_JobStopWatch.Restart();
			while (RunJobState())
			{
			}
		}
	}

	private bool RunJobState()
	{
		if (IsRunningOnTakeControl)
		{
			return false;
		}
		return _jobState switch
		{
			JobState.RemoveRequesters => RunJobStateRemoveRequesters(), 
			JobState.PrepareRequest => RunJobStatePrepareRequest(), 
			JobState.WaitForRequestJobs => RunJobStateWaitForRequestJobs(), 
			JobState.Request => RunJobStateRequest(), 
			JobState.Prepare => RunJobStatePrepare(), 
			JobState.WaitForJobs => RunJobStateWaitForJobs(), 
			JobState.Apply => RunJobStateApply(), 
			JobState.Delete => RunJobStateDelete(), 
			_ => false, 
		};
	}

	public static async UniTask InitialiseLodsOnLoad()
	{
		GameState gameState = GameManager.GameState;
		if (gameState != GameState.Joining && gameState != GameState.Loading)
		{
			await UniTask.WaitUntil(() => _jobState == JobState.RemoveRequesters);
		}
		else
		{
			await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenPreparingTerrain.DisplayString);
		}
		IsRunningOnTakeControl = true;
		try
		{
			await UniTask.NextFrame();
			Instance.RunJobStateRemoveRequesters();
			lock (_requestersToUpdateLock)
			{
				foreach (ILodRequester item in _requestersToUpdate)
				{
					if (item is Thing thing && (bool)thing)
					{
						thing.CachePositionOnSpawn();
					}
				}
			}
			Instance.RunJobStatePrepareRequest();
			while (!Instance.RunJobStateWaitForRequestJobs())
			{
				gameState = GameManager.GameState;
				if (gameState == GameState.Joining || gameState == GameState.Loading)
				{
					await ImGuiLoadingScreen.SetProgress(0f);
				}
			}
			Instance.RunJobStateRequest();
			Instance.RunJobStatePrepare();
			int startTotalJobs = Mathf.Max(1, LodMeshWorker.GetJobsEnqueued());
			while (!Instance.RunJobStateWaitForJobs())
			{
				await ImGuiLoadingScreen.SetProgress((float)(startTotalJobs - LodMeshWorker.GetJobsEnqueued()) / (float)startTotalJobs);
			}
			Instance.RunJobStateApply(applyAll: true);
			Instance.RunJobStateDelete();
		}
		finally
		{
			IsRunningOnTakeControl = false;
		}
	}

	private bool RunJobStateRemoveRequesters()
	{
		_jobState = JobState.PrepareRequest;
		if (_requestersToRemove.Count > 0)
		{
			ProcessRequestersToRemove();
		}
		return true;
	}

	private bool RunJobStatePrepareRequest()
	{
		_jobState = JobState.WaitForRequestJobs;
		_alreadyProcessedThings.Clear();
		bool flag = false;
		lock (_requestersToUpdateLock)
		{
			while (_requestersToUpdate.Count > 0)
			{
				ILodRequester lodRequester = _requestersToUpdate.Dequeue();
				if (!lodRequester.BeingDestroyed && _alreadyProcessedThings.Add(lodRequester.ReferenceId))
				{
					LodRequestWorker.Assign(lodRequester);
					flag = true;
					if (lodRequester is Human { ShouldRender: not false } human)
					{
						VoxelTerrain.DirtyAllMinables(human.Position).Forget();
					}
				}
			}
		}
		if (flag)
		{
			LodRequestWorker.ExecuteAll();
			return false;
		}
		return true;
	}

	private bool RunJobStateWaitForRequestJobs()
	{
		if (LodRequestWorker.IsAnyWorking())
		{
			return false;
		}
		_jobState = JobState.Request;
		return true;
	}

	private bool RunJobStateRequest()
	{
		_jobState = JobState.Prepare;
		foreach (LodRequest item in _toAdd)
		{
			RequestLod(item);
		}
		foreach (LodRequest item2 in _toRemove)
		{
			UnRequestLod(item2);
		}
		_toAdd.Clear();
		_toRemove.Clear();
		return (float)_JobStopWatch.ElapsedMilliseconds < TERRAIN_MAIN_THREAD_BUDGET_MS;
	}

	private bool RunJobStatePrepare()
	{
		_jobState = JobState.WaitForJobs;
		if (_updateLodQueue.Count > 0)
		{
			PrepareAndExecuteJobs();
			return false;
		}
		return true;
	}

	private bool RunJobStateWaitForJobs()
	{
		if (LodMeshWorker.IsAnyWorking())
		{
			return false;
		}
		_jobState = JobState.Apply;
		return true;
	}

	private bool RunJobStateApply(bool applyAll = false)
	{
		if (LodObject.FinishedQueue.Count > 0)
		{
			return ApplyJobs(applyAll);
		}
		_jobState = JobState.Delete;
		return true;
	}

	private bool RunJobStateDelete()
	{
		if (_deleteLodQueue.Count > 0)
		{
			return DeleteLods();
		}
		_jobState = JobState.RemoveRequesters;
		return false;
	}

	public static void AddRequests(List<LodRequest> requested, List<LodRequest> unrequested)
	{
		lock (AddRequestsLock)
		{
			_toAdd.AddRange(requested);
			_toRemove.AddRange(unrequested);
		}
	}

	private static void ProcessRequestersToRemove()
	{
		while (_requestersToRemove.Count > 0)
		{
			ILodRequester lodRequester = _requestersToRemove.Dequeue();
			for (int i = 0; i < lodRequester.RequestedLods.Length; i++)
			{
				foreach (Vector3Int item in lodRequester.RequestedLods[i])
				{
					if (LodObjectCache.TryGetActive(item, i, out var lodObject))
					{
						lodObject.RemoveRequester(lodRequester);
						if (!lodObject.HasRequesters)
						{
							EnqueueRemovedLod(lodObject);
						}
						else
						{
							lodObject.UpdateShouldRender();
						}
					}
				}
			}
			HashSet<Vector3Int>[] requestedLods = lodRequester.RequestedLods;
			for (int j = 0; j < requestedLods.Length; j++)
			{
				requestedLods[j].Clear();
			}
		}
	}

	private void PrepareAndExecuteJobs()
	{
		_updatedLodIndices.Reset();
		bool flag = false;
		while (_updateLodQueue.Count > 0)
		{
			LodObject lodObject = _updateLodQueue.Dequeue();
			if (!LodObjectCache.IsStateEmpty(lodObject.Index, lodObject.Level) && _updatedLodIndices[lodObject.Level].Add(lodObject.Index))
			{
				flag = true;
				LodMeshWorker.Assign(lodObject);
			}
		}
		if (flag)
		{
			LodMeshWorker.ExecuteAll();
		}
	}

	private bool ApplyJobs(bool applyAll)
	{
		while (LodObject.FinishedQueue.Count > 0)
		{
			if (!applyAll && (float)_JobStopWatch.ElapsedMilliseconds > TERRAIN_MAIN_THREAD_BUDGET_MS)
			{
				return false;
			}
			LodObject lodObject = LodObject.FinishedQueue.Dequeue();
			if (lodObject == null)
			{
				ConsoleWindow.PrintError("lod job is null");
			}
			else if (!lodObject.HasRequesters)
			{
				EnqueueRemovedLod(lodObject);
			}
			else
			{
				lodObject.ApplyMesh();
			}
		}
		return true;
	}

	private bool DeleteLods()
	{
		while (_deleteLodQueue.Count > 0)
		{
			if ((float)_JobStopWatch.ElapsedMilliseconds > TERRAIN_MAIN_THREAD_BUDGET_MS)
			{
				return false;
			}
			LodObject lodObject = _deleteLodQueue.Dequeue();
			if (lodObject.IsActive)
			{
				if (lodObject.HasRequesters)
				{
					EnqueueDirtyLod(lodObject);
				}
				else
				{
					LodObjectCache.Return(lodObject);
				}
			}
		}
		return false;
	}

	public static bool IsAtOrUnderBedrock(Vector3 position)
	{
		return position.y < 2f;
	}

	public LodMeshRenderer InstantiateLodMeshRenderer(Vector3 position, LodMeshRenderer prefab)
	{
		LodMeshRenderer obj = Object.Instantiate(prefab, position, Quaternion.identity, Transform);
		obj.SetActive(active: false);
		LodMeshRenderer.Register(obj);
		return obj;
	}

	public LodMeshRenderer GetLodMeshFromPool(Vector3Int position, int level)
	{
		LodMeshCache.CacheObject uninitialized = _lodMeshCaches[level].GetUninitialized(position);
		uninitialized.LodMeshRenderer.SetActive(active: true);
		return uninitialized.LodMeshRenderer;
	}

	public void DeactivateLodMeshRenderer(LodMeshRenderer lodMeshRenderer, int level)
	{
		_lodMeshCaches[level].Deactivate(lodMeshRenderer.CacheObject);
		lodMeshRenderer.SetActive(active: false);
	}

	public void ReturnLodMeshRenderer(LodMeshRenderer lodMeshRenderer, int level)
	{
		_lodMeshCaches[level].SetUninitialized(lodMeshRenderer.CacheObject);
		lodMeshRenderer.SetActive(active: false);
	}

	private void RequestLod(LodRequest request)
	{
		if (LodObjectCache.TryGetActive(request.Index, request.Level, out var lodObject))
		{
			lodObject.AddRequester(request.Requester);
			SetMeshFromExistingOrQueueForRegeneration(lodObject, request.Index, request.Level);
		}
		else
		{
			lodObject = LodObjectCache.GetFromPool(request.Index, request.Level);
			lodObject.AddRequester(request.Requester);
			SetMeshFromExistingOrQueueForRegeneration(lodObject, request.Index, request.Level);
		}
	}

	private void UnRequestLod(LodRequest lodRequest)
	{
		if (LodObjectCache.TryGetActive(lodRequest.Index, lodRequest.Level, out var lodObject))
		{
			lodObject.RemoveRequester(lodRequest.Requester);
			lodRequest.Requester.RemoveRequested(lodObject);
			if (!lodObject.HasRequesters)
			{
				EnqueueRemovedLod(lodObject);
			}
			else
			{
				lodObject.UpdateShouldRender();
			}
		}
	}

	private void SetMeshFromExistingOrQueueForRegeneration(LodObject lodObject, Vector3Int index, int level)
	{
		LodMeshCache lodMeshCache = _lodMeshCaches[level];
		if (lodMeshCache.TryGetActive(index, out var cacheObject))
		{
			lodObject.UpdateShouldRender();
		}
		else if (lodMeshCache.TryReactivate(index, out cacheObject))
		{
			lodObject.LodMeshRenderer = cacheObject.LodMeshRenderer;
			lodObject.LodMeshRenderer.Transform.position = index + VoxelConstants.TerrainMeshOffset + VoxelConstants.TerrainLodLevelOffset(lodObject.Level);
			if (cacheObject.LodMeshRenderer.IsDirty)
			{
				cacheObject.LodMeshRenderer.IsDirty = false;
				EnqueueDirtyLod(lodObject);
			}
			else
			{
				lodObject.LodMeshRenderer.SetActive(active: true);
				lodObject.UpdateShouldRender();
			}
		}
		else
		{
			EnqueueDirtyLod(lodObject);
		}
	}

	public void DirtyLodsBounds(Vector3 min, Vector3 max)
	{
		Vector3Int vector3Int = min.FloorToInt();
		Vector3Int vector3Int2 = max.FloorToInt();
		vector3Int /= 8;
		vector3Int2 /= 8;
		vector3Int -= Vector3Int.one;
		vector3Int2 += Vector3Int.one;
		vector3Int *= 8;
		vector3Int2 *= 8;
		for (int i = vector3Int.x; i <= vector3Int2.x; i += 8)
		{
			for (int j = vector3Int.y; j <= vector3Int2.y; j += 8)
			{
				for (int k = vector3Int.z; k <= vector3Int2.z; k += 8)
				{
					DirtyLods(new Vector3(i, j, k), dirtyNeighbours: false);
				}
			}
		}
	}

	public void DirtyLods(Vector3 position, bool dirtyNeighbours)
	{
		for (int i = 0; i < 6; i++)
		{
			int size = 8 * (1 << i);
			Vector3Int vector3Int = FloorToLodSize(position, size);
			if (dirtyNeighbours)
			{
				Vector3Int[] array = LodNeighbourOffsets[i];
				foreach (Vector3Int vector3Int2 in array)
				{
					DirtyLod(vector3Int + vector3Int2, i);
				}
			}
			else
			{
				DirtyLod(vector3Int, i);
			}
		}
	}

	private void DirtyLod(Vector3Int index, int level)
	{
		LodObjectCache.SetStateUnknown(index, level);
		LodMeshCache.CacheObject cacheObject;
		if (LodObjectCache.TryGetActive(index, level, out var lodObject))
		{
			EnqueueDirtyLod(lodObject);
			if ((object)lodObject.LodMeshRenderer != null)
			{
				lodObject.LodMeshRenderer.IsDirty = true;
			}
		}
		else if (_lodMeshCaches[level].TryGetDeactivated(index, out cacheObject) && (object)cacheObject.LodMeshRenderer != null)
		{
			cacheObject.LodMeshRenderer.IsDirty = true;
		}
	}

	private static void EnqueueDirtyLod(LodObject lodObject)
	{
		_updateLodQueue.Enqueue(lodObject);
	}

	private static void EnqueueRemovedLod(LodObject lodObject)
	{
		_deleteLodQueue.Enqueue(lodObject);
	}

	public static void EnqueueRequesterToRemove(ILodRequester lodRequester)
	{
		_requestersToRemove.Enqueue(lodRequester);
	}

	public static void EnqueueRequesterToUpdate(ILodRequester requester)
	{
		if (requester == null)
		{
			return;
		}
		lock (_requestersToUpdateLock)
		{
			_requestersToUpdate.Enqueue(requester);
		}
	}

	public void Clear()
	{
		LodMeshCache[] lodMeshCaches = _lodMeshCaches;
		for (int i = 0; i < lodMeshCaches.Length; i++)
		{
			lodMeshCaches[i].Reset();
		}
		TerrainColliderBaker.Clear();
		_updateLodQueue.Clear();
		_deleteLodQueue.Clear();
		_requestersToRemove.Clear();
		_requestersToUpdate.Clear();
		_updatedLodIndices.Reset();
		_alreadyProcessedThings.Clear();
		_jobState = JobState.RemoveRequesters;
		IsRunningOnTakeControl = false;
	}

	public static void CalculateLods(ILodRequester requester, List<LodGroup> lods)
	{
		LodInfo lodInfo = requester.LodInfo;
		Vector3 centerPosition = requester.CenterPosition;
		if (requester.ShouldRender)
		{
			centerPosition.x -= OrbitalViewController.RecenterX;
			centerPosition.z -= OrbitalViewController.RecenterZ;
		}
		foreach (LodGroup lod in lods)
		{
			lod.Clear();
		}
		if (!lodInfo.Expand)
		{
			LodLevel lodLevel = lodInfo.Levels[0];
			GetLodIndices(centerPosition, lodLevel.Size, lodLevel.Radius, ref lods);
			return;
		}
		LodBounds previousBounds = LodBounds.Zero();
		for (int i = 0; i < lodInfo.Levels.Count; i++)
		{
			LodLevel lodLevel2 = lodInfo.Levels[i];
			LodGroup lodGroup = lods[i];
			GetLodIndicesWithExpandingBounds(centerPosition, lodLevel2.Size, lodLevel2.Radius, previousBounds, out var bounds, ref lodGroup);
			previousBounds = bounds;
		}
	}

	private static void GetLodIndices(Vector3 position, int size, int radius, ref List<LodGroup> lods)
	{
		Vector3Int vector3Int = FloorToLodSize(position, size);
		int num = size * radius;
		for (int i = -num; i <= num; i += size)
		{
			for (int j = -num; j <= num; j += size)
			{
				for (int k = -num; k <= num; k += size)
				{
					Vector3Int vector3Int2 = vector3Int + new Vector3Int(i, j, k);
					if (!OutOfBounds(vector3Int2, size))
					{
						lods[0].Indices.Add(vector3Int2);
					}
				}
			}
		}
	}

	private static void GetLodIndicesWithExpandingBounds(Vector3 position, int size, int radius, LodBounds previousBounds, out LodBounds bounds, ref LodGroup lodGroup)
	{
		Vector3Int vector3Int = FloorToLodSize(position, size);
		GetBounds(vector3Int.x, size, radius, out var min, out var max);
		GetBounds(vector3Int.y, size, radius, out var min2, out var max2);
		GetBounds(vector3Int.z, size, radius, out var min3, out var max3);
		bounds = new LodBounds(min, min2, min3, max, max2, max3);
		for (int i = min - size; i <= max + size; i += size)
		{
			for (int j = min2 - size; j <= max2 + size; j += size)
			{
				for (int k = min3 - size; k <= max3 + size; k += size)
				{
					Vector3Int vector3Int2 = new Vector3Int(i, j, k);
					if (!OutOfBounds(vector3Int2, size) && previousBounds.Outside(vector3Int2))
					{
						lodGroup.Indices.Add(vector3Int2);
					}
				}
			}
		}
	}

	private static bool OutOfBounds(Vector3Int position, int size)
	{
		if (position.y > 1023)
		{
			return true;
		}
		if (position.y + size < 0)
		{
			return true;
		}
		if (position.x < -VoxelConstants.Offset || position.x >= VoxelConstants.Offset || position.z < -VoxelConstants.Offset || position.z >= VoxelConstants.Offset)
		{
			return true;
		}
		return false;
	}

	private static void GetBounds(int value, int size, int radius, out int min, out int max)
	{
		min = value - radius * size;
		max = value + radius * size;
		int num = size * 2;
		if (min % num != 0)
		{
			min -= size;
		}
		if ((max + size) % num != 0)
		{
			max += size;
		}
	}

	private static Vector3Int FloorToLodSize(Vector3 position, int size)
	{
		int x = Mathf.FloorToInt(position.x / (float)size) * size;
		int y = Mathf.FloorToInt(position.y / (float)size) * size;
		int z = Mathf.FloorToInt(position.z / (float)size) * size;
		return new Vector3Int(x, y, z);
	}
}
