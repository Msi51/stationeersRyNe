using UnityEngine;

namespace TerrainSystem.Lods;

public class LodMeshRendererWithCollision : LodMeshRenderer
{
	[SerializeField]
	private MeshCollider _meshCollider;

	private Mesh _retiredColliderMesh;

	public override void SetMesh(Mesh mesh, bool shouldRenderMesh, Material sharedMaterial)
	{
		base.SetMesh(mesh, shouldRenderMesh, sharedMaterial);
		TerrainColliderBaker.RequestBake(this, mesh);
	}

	protected override void ReleasePreviousMesh(Mesh previousMesh)
	{
		if (_meshCollider.sharedMesh == previousMesh)
		{
			_retiredColliderMesh = previousMesh;
		}
		else
		{
			base.ReleasePreviousMesh(previousMesh);
		}
	}

	public void ApplyBakedCollider(Mesh mesh)
	{
		if (!(base.CurrentMesh != mesh))
		{
			_meshCollider.sharedMesh = mesh;
			if (_retiredColliderMesh != null)
			{
				TerrainColliderBaker.DestroyMesh(_retiredColliderMesh);
				_retiredColliderMesh = null;
			}
		}
	}

	public override void Clear()
	{
		_meshCollider.sharedMesh = null;
		if (_retiredColliderMesh != null)
		{
			TerrainColliderBaker.DestroyMesh(_retiredColliderMesh);
			_retiredColliderMesh = null;
		}
		base.Clear();
	}
}
