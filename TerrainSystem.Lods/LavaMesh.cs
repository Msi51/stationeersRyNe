using UnityEngine;

namespace TerrainSystem.Lods;

public class LavaMesh : GameBase
{
	[SerializeField]
	private MeshFilter _meshFilter;

	[SerializeField]
	private MeshRenderer _meshRenderer;

	private static readonly int LAVA_MIN = Shader.PropertyToID("_MinLavaHeight");

	private static readonly int LAVA_MAX = Shader.PropertyToID("_MaxLavaHeight");

	private static readonly int HEIGHT_TEX = Shader.PropertyToID("_HeightMapTexture");

	public virtual void SetMesh(Mesh mesh)
	{
		_meshFilter.mesh = mesh;
		if (!(mesh == null))
		{
			Bounds bounds = mesh.bounds;
			bounds.Expand(new Vector3((float)VoxelConstants.Size * 2f, 0f, (float)VoxelConstants.Size * 2f));
			_meshRenderer.localBounds = bounds;
		}
	}

	public void Clear()
	{
		_meshFilter.mesh = null;
	}

	public void SetData(LavaData data)
	{
		_meshRenderer.sharedMaterial.SetFloat(LAVA_MIN, data.MinHeight);
		_meshRenderer.sharedMaterial.SetFloat(LAVA_MAX, data.MaxHeight);
		_meshRenderer.sharedMaterial.SetTexture(HEIGHT_TEX, data.LavaHeight.Texture);
	}
}
