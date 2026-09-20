using System;
using UnityEngine;

namespace TerrainSystem.Lods;

public class ColliderBakeWorker : ThreadWorker<ColliderBakeWorker, ColliderBakeJob>
{
	public static bool IsAnyWorking()
	{
		return WorkerCollections.ColliderBakeWorkers.IsAnyWorking();
	}

	public static void Assign(ColliderBakeJob job)
	{
		WorkerCollections.ColliderBakeWorkers.Assign(job);
	}

	public static void ExecuteAll()
	{
		WorkerCollections.ColliderBakeWorkers.ExecuteAll();
	}

	public static void AbortAll()
	{
		WorkerCollections.ColliderBakeWorkers.AbortAll();
	}

	public ColliderBakeWorker()
	{
		DoTask = BakeTask;
	}

	private void BakeTask(object state)
	{
		if (!StartTask())
		{
			return;
		}
		try
		{
			while (_objects.Count > 0 && _state != WorkerState.Abort)
			{
				ColliderBakeJob job = _objects.Dequeue();
				try
				{
					Physics.BakeMesh(job.MeshId, convex: false);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
				TerrainColliderBaker.OnBakeCompleted(in job);
			}
		}
		catch (Exception exception2)
		{
			Debug.LogException(exception2);
		}
		finally
		{
			FinishTask();
		}
	}
}
