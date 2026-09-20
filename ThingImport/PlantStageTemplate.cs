using UnityEngine;

namespace ThingImport;

public class PlantStageTemplate : MonoBehaviour
{
	[SerializeField]
	private MeshFilter _meshFilter;

	[SerializeField]
	private MeshRenderer _meshRenderer;

	public MeshRenderer Renderer => _meshRenderer;

	public void Initialize(PlantStageData stageData, CustomPlantData plantData, Material material)
	{
		_meshRenderer.sharedMaterial = material;
		_meshFilter.sharedMesh = stageData.MeshRef.Mesh;
		_meshRenderer.sharedMaterial.SetTexture("_MainTex", plantData.TextureRef.Texture);
	}
}
