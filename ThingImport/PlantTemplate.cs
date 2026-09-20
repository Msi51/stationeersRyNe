using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace ThingImport;

public class PlantTemplate : MonoBehaviour
{
	[SerializeField]
	private MeshFilter _meshFilter;

	[SerializeField]
	private MeshRenderer _meshRenderer;

	[SerializeField]
	private BoxCollider _boxCollider;

	[SerializeField]
	private Plant _plant;

	public Plant Plant => _plant;

	public MeshRenderer MeshRenderer => _meshRenderer;

	public void Initialize(CustomPlantData data, Material material)
	{
		_meshRenderer.sharedMaterial = material;
		_meshFilter.sharedMesh = data.MeshRef.Mesh;
		if (data.TextureRef != null)
		{
			_meshRenderer.sharedMaterial.SetTexture("_MainTex", data.TextureRef.Texture);
		}
		_boxCollider.center = data.MeshRef.Mesh.bounds.center;
		_boxCollider.size = data.MeshRef.Mesh.bounds.size;
	}
}
