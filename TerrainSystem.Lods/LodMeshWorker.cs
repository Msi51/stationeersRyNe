using System;
using UnityEngine;

namespace TerrainSystem.Lods;

public class LodMeshWorker : ThreadWorker<LodMeshWorker, LodObject>
{
	public static bool IsAnyWorking()
	{
		return WorkerCollections.LodMeshWorkers.IsAnyWorking();
	}

	public static void Assign(LodObject job)
	{
		WorkerCollections.LodMeshWorkers.Assign(job);
	}

	public static void ExecuteAll()
	{
		WorkerCollections.LodMeshWorkers.ExecuteAll();
	}

	public static void AbortAll()
	{
		WorkerCollections.LodMeshWorkers.AbortAll();
	}

	public LodMeshWorker()
	{
		DoTask = MeshTask;
	}

	private void MeshTask(object state)
	{
		if (!StartTask())
		{
			return;
		}
		try
		{
			while (_objects.Count > 0 && _state != WorkerState.Abort)
			{
				LodObject lodObject = _objects.Dequeue();
				if (lodObject != null && lodObject.CanThread())
				{
					lodObject.GenerateMesh();
				}
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		finally
		{
			FinishTask();
		}
	}

	public static int GetJobsEnqueued()
	{
		int num = 0;
		LodMeshWorker[] workers = WorkerCollections.LodMeshWorkers.Workers;
		foreach (LodMeshWorker lodMeshWorker in workers)
		{
			num += lodMeshWorker.JobCount;
		}
		return num;
	}
}
