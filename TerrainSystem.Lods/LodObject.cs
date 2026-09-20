using System.Collections.Generic;
using Assets.Scripts.Util;
using UnityEngine;

namespace TerrainSystem.Lods;

public class LodObject : IThreadable, IBasicPoolable<Vector3Int>
{
	public int JobId;

	public const int DEFAULT_COLLECTION_SIZE = 800;

	public const int VEIN_COLLECTION_SIZE = 64;

	public static Queue<LodObject> FinishedQueue = new Queue<LodObject>(4096);

	private readonly List<Vector3> _verts = new List<Vector3>();

	private readonly List<int> _indices = new List<int>();

	private readonly List<byte> _voxelTypes = new List<byte>();

	private readonly List<Vector3> _normals = new List<Vector3>();

	private readonly List<Vector3> _deformationNormals = new List<Vector3>();

	private readonly List<Vector2> _minableColourHueValue = new List<Vector2>();

	private readonly List<Vein> _veins = new List<Vein>();

	private MarchingCubes _marchingCubes = new MarchingCubes();

	public readonly Dictionary<long, ILodRequester> RequesterLookup = new Dictionary<long, ILodRequester>(16);

	public int ThreadCost
	{
		get
		{
			if (!HasRequesters)
			{
				return 1;
			}
			return 10;
		}
	}

	public bool IsActive { get; set; }

	public Vector3Int Key => Index;

	public Vector3Int Index { get; private set; }

	public int Size { get; private set; }

	public int Level { get; private set; }

	public LodMeshRenderer LodMeshRenderer { get; set; }

	public bool HasRequesters => RequesterLookup.Count > 0;

	public bool ShouldRenderMesh { get; private set; }

	public string DebugName()
	{
		return "LodObject" + StringManager.Get(JobId);
	}

	public bool CanThread()
	{
		return true;
	}

	public void OnReturnedToPool()
	{
		DeactivateLodMeshRenderer();
		ClearCollections();
		Index = Vector3Int.zero;
		RequesterLookup.Clear();
		ShouldRenderMesh = false;
	}

	private int GetSize()
	{
		return (int)Mathf.Pow(2f, Level) * 8;
	}

	public void Set(Vector3Int index, int level)
	{
		Index = index;
		Level = level;
		Size = GetSize();
	}

	public void AddRequester(ILodRequester requester)
	{
		if (RequesterLookup.TryAdd(requester.ReferenceId, requester))
		{
			requester.RequestedLods[Level].Add(Index);
			CheckShouldRender();
		}
	}

	public void RemoveRequester(ILodRequester requester)
	{
		if (RequesterLookup.Remove(requester.ReferenceId))
		{
			CheckShouldRender();
		}
	}

	private void CheckShouldRender()
	{
		ShouldRenderMesh = false;
		foreach (KeyValuePair<long, ILodRequester> item in RequesterLookup)
		{
			if (item.Value.ShouldRender)
			{
				ShouldRenderMesh = true;
				break;
			}
		}
	}

	public void UpdateShouldRender()
	{
		LodMeshRenderer?.SetShouldRender(ShouldRenderMesh);
	}

	private void ClearCollections()
	{
		_verts.Clear();
		_verts.Capacity = _verts.Count;
		_indices.Clear();
		_indices.Capacity = _indices.Count;
		_voxelTypes.Clear();
		_voxelTypes.Capacity = _voxelTypes.Count;
		_normals.Clear();
		_normals.Capacity = _normals.Count;
		_deformationNormals.Clear();
		_deformationNormals.Capacity = _deformationNormals.Count;
		_minableColourHueValue.Clear();
		_minableColourHueValue.Capacity = _minableColourHueValue.Count;
		_veins.Clear();
		_veins.Capacity = _veins.Count;
	}

	public void GenerateMesh()
	{
		ClearCollections();
		_marchingCubes.Surface = 127;
		sbyte desiredDepth = (sbyte)(VoxelTerrain.MaxDepth - Level);
		Vector3Int vector3Int = VoxelTerrain.WorldToOctreeSpace(Index, VoxelConstants.OriginOffsetInt) / Size;
		_marchingCubes.Generate(_verts, _normals, _deformationNormals, _voxelTypes, _minableColourHueValue, _indices, _veins, vector3Int.x, vector3Int.y, vector3Int.z, Size, desiredDepth);
		lock (FinishedQueue)
		{
			FinishedQueue.Enqueue(this);
		}
	}

	public void ApplyMesh()
	{
		Mesh mesh = MeshCreatorHelper.CreateMesh(_verts, _normals, _deformationNormals, _voxelTypes, _minableColourHueValue, _indices);
		if ((bool)mesh)
		{
			if ((object)LodMeshRenderer == null)
			{
				LodMeshRenderer = LodManager.Instance.GetLodMeshFromPool(Index, Level);
				LodMeshRenderer.Transform.position = Index + VoxelConstants.TerrainMeshOffset + VoxelConstants.TerrainLodLevelOffset(Level);
			}
			try
			{
				if (Level > 2)
				{
					AdjustMeshBoundsForCurvature(mesh);
				}
				LodMeshRenderer.SetActive(active: true);
				LodMeshRenderer.SetMesh(mesh, ShouldRenderMesh, VoxelTerrain.Instance.TerrainMaterial);
				LodMeshRenderer.IsDirty = false;
				return;
			}
			catch
			{
				LodObjectCache.SetStateEmpty(Index, Level);
				ReturnLodMeshRenderer();
				return;
			}
		}
		LodObjectCache.SetStateEmpty(Index, Level);
		ReturnLodMeshRenderer();
	}

	private void AdjustMeshBoundsForCurvature(Mesh mesh)
	{
		int num = Level * Level * Level * 10;
		Bounds bounds = mesh.bounds;
		float curvatureOffset = VoxelConstants.GetCurvatureOffset(Vector3.zero, Vector3.one * num);
		Vector3 size = new Vector3(bounds.size.x, bounds.size.y - curvatureOffset, bounds.size.z);
		Vector3 center = bounds.center + new Vector3(0f, curvatureOffset / 2f, 0f);
		Bounds bounds2 = new Bounds(center, size);
		mesh.bounds = bounds2;
	}

	private void ReturnLodMeshRenderer()
	{
		if (!(LodMeshRenderer == null))
		{
			LodManager.Instance.ReturnLodMeshRenderer(LodMeshRenderer, Level);
			LodMeshRenderer = null;
		}
	}

	private void DeactivateLodMeshRenderer()
	{
		if (!(LodMeshRenderer == null))
		{
			LodManager.Instance.DeactivateLodMeshRenderer(LodMeshRenderer, Level);
			LodMeshRenderer = null;
		}
	}
}
