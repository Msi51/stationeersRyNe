using System.Collections.Generic;
using UnityEngine;

namespace TerrainSystem.Lods;

public class LodMeshRenderer : GameBase
{
	[SerializeField]
	private MeshFilter _meshFilter;

	[SerializeField]
	private MeshRenderer _meshRenderer;

	public bool IsDirty;

	private static readonly List<LodMeshRenderer> AllRenderers = new List<LodMeshRenderer>(1024);

	private static bool _spaceCulling;

	public LodMeshCache.CacheObject CacheObject;

	public Mesh CurrentMesh => _meshFilter.sharedMesh;

	public static void Register(LodMeshRenderer lodMeshRenderer)
	{
		AllRenderers.Add(lodMeshRenderer);
	}

	public static void SetSpaceCulling(bool enabled)
	{
		if (_spaceCulling == enabled)
		{
			return;
		}
		_spaceCulling = enabled;
		for (int num = AllRenderers.Count - 1; num >= 0; num--)
		{
			LodMeshRenderer lodMeshRenderer = AllRenderers[num];
			if (lodMeshRenderer == null)
			{
				AllRenderers.RemoveAt(num);
			}
			else
			{
				lodMeshRenderer.ApplyCullingBounds();
			}
		}
	}

	private void ApplyCullingBounds()
	{
		if (_meshRenderer == null)
		{
			return;
		}
		if (_spaceCulling)
		{
			Mesh sharedMesh = _meshFilter.sharedMesh;
			if (!(sharedMesh == null))
			{
				Bounds bounds = sharedMesh.bounds;
				bounds.Expand(new Vector3((float)VoxelConstants.Size * 2f, 0f, (float)VoxelConstants.Size * 2f));
				_meshRenderer.localBounds = bounds;
			}
		}
		else
		{
			_meshRenderer.ResetLocalBounds();
		}
	}

	public virtual void SetMesh(Mesh mesh, bool shouldRenderMesh, Material sharedMaterial)
	{
		if (_meshFilter.sharedMesh != null)
		{
			ReleasePreviousMesh(_meshFilter.sharedMesh);
		}
		_meshFilter.sharedMesh = mesh;
		_meshRenderer.sharedMaterial = sharedMaterial;
		_meshRenderer.enabled = shouldRenderMesh;
		if (_spaceCulling)
		{
			ApplyCullingBounds();
		}
	}

	protected virtual void ReleasePreviousMesh(Mesh previousMesh)
	{
		TerrainColliderBaker.DestroyMesh(previousMesh);
	}

	public void SetShouldRender(bool shouldRenderMesh)
	{
		_meshRenderer.enabled = shouldRenderMesh;
	}

	public virtual void Clear()
	{
		if (_meshFilter.sharedMesh != null)
		{
			ReleasePreviousMesh(_meshFilter.sharedMesh);
		}
		_meshFilter.sharedMesh = null;
		IsDirty = false;
	}
}
