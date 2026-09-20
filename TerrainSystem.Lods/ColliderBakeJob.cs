using UnityEngine;

namespace TerrainSystem.Lods;

public readonly struct ColliderBakeJob(Mesh mesh, int meshId, LodMeshRendererWithCollision renderer) : IThreadable
{
	public readonly Mesh Mesh = mesh;

	public readonly int MeshId = meshId;

	public readonly LodMeshRendererWithCollision Renderer = renderer;

	public int ThreadCost => 1;

	public bool CanThread()
	{
		return true;
	}

	public string DebugName()
	{
		return string.Empty;
	}
}
