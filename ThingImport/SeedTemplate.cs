using UnityEngine;

namespace ThingImport;

public class SeedTemplate : MonoBehaviour
{
	[SerializeField]
	private MeshFilter _meshFilter;

	[SerializeField]
	private MeshRenderer _meshRenderer;

	[SerializeField]
	private BoxCollider _boxCollider;

	[SerializeField]
	private Seed _seed;

	public Seed Seed => _seed;

	public void Initialize(CustomSeedData data, Material material)
	{
		_meshFilter.sharedMesh = data.MeshRef.Mesh;
		_meshRenderer.sharedMaterial = material;
		_meshRenderer.sharedMaterial.SetTexture("_MainTex", data.TextureRef.Texture);
		_boxCollider.center = data.MeshRef.Mesh.bounds.center;
		_boxCollider.size = data.MeshRef.Mesh.bounds.size;
	}
}
