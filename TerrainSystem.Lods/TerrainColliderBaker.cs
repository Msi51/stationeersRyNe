using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainSystem.Lods;

public static class TerrainColliderBaker
{
	private static readonly Queue<ColliderBakeJob> _pendingBakes = new Queue<ColliderBakeJob>(256);

	private static readonly ConcurrentQueue<ColliderBakeJob> _completedBakes = new ConcurrentQueue<ColliderBakeJob>();

	private static readonly HashSet<int> _bakingMeshIds = new HashSet<int>(256);

	private static readonly HashSet<int> _destroyAfterBakeMeshIds = new HashSet<int>(64);

	public static bool HasOutstandingBakes
	{
		get
		{
			if (_pendingBakes.Count <= 0)
			{
				return _bakingMeshIds.Count > 0;
			}
			return true;
		}
	}

	public static void RequestBake(LodMeshRendererWithCollision renderer, Mesh mesh)
	{
		int instanceID = mesh.GetInstanceID();
		_bakingMeshIds.Add(instanceID);
		_pendingBakes.Enqueue(new ColliderBakeJob(mesh, instanceID, renderer));
	}

	public static void DestroyMesh(Mesh mesh)
	{
		if (_bakingMeshIds.Contains(mesh.GetInstanceID()))
		{
			_destroyAfterBakeMeshIds.Add(mesh.GetInstanceID());
		}
		else
		{
			Object.Destroy(mesh);
		}
	}

	public static void OnBakeCompleted(in ColliderBakeJob job)
	{
		_completedBakes.Enqueue(job);
	}

	public static void MainThreadUpdate()
	{
		ColliderBakeJob result;
		while (_completedBakes.TryDequeue(out result))
		{
			_bakingMeshIds.Remove(result.MeshId);
			if (_destroyAfterBakeMeshIds.Remove(result.MeshId))
			{
				if (result.Mesh != null)
				{
					Object.Destroy(result.Mesh);
				}
			}
			else if (result.Renderer != null)
			{
				result.Renderer.ApplyBakedCollider(result.Mesh);
			}
			else if (result.Mesh != null)
			{
				Object.Destroy(result.Mesh);
			}
		}
		if (_pendingBakes.Count > 0 && !ColliderBakeWorker.IsAnyWorking())
		{
			while (_pendingBakes.Count > 0)
			{
				ColliderBakeWorker.Assign(_pendingBakes.Dequeue());
			}
			ColliderBakeWorker.ExecuteAll();
		}
	}

	public static void Clear()
	{
		while (_pendingBakes.Count > 0)
		{
			ColliderBakeJob colliderBakeJob = _pendingBakes.Dequeue();
			_bakingMeshIds.Remove(colliderBakeJob.MeshId);
			if (_destroyAfterBakeMeshIds.Remove(colliderBakeJob.MeshId) && colliderBakeJob.Mesh != null)
			{
				Object.Destroy(colliderBakeJob.Mesh);
			}
		}
	}
}
