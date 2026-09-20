using System;
using System.Threading;
using UnityEngine;

namespace TerrainSystem;

public class FillOctreeWorker : ThreadWorker<FillOctreeWorker, FillOctreeJob>
{
	protected override System.Threading.ThreadPriority Priority => System.Threading.ThreadPriority.Normal;

	public static bool IsAnyWorking()
	{
		return WorkerCollections.FillOctreeWorkers.IsAnyWorking();
	}

	public static void Assign(FillOctreeJob job)
	{
		WorkerCollections.FillOctreeWorkers.Assign(job);
	}

	public static void ExecuteAll()
	{
		WorkerCollections.FillOctreeWorkers.ExecuteAll();
	}

	public static void AbortAll()
	{
		WorkerCollections.FillOctreeWorkers.AbortAll();
	}

	public FillOctreeWorker()
	{
		DoTask = WorkerTask;
	}

	private void WorkerTask(object state)
	{
		if (!StartTask())
		{
			return;
		}
		try
		{
			while (_objects.Count > 0 && _state != WorkerState.Abort)
			{
				FillOctreeJob fillOctreeJob = _objects.Dequeue();
				if (fillOctreeJob != null && fillOctreeJob.CanThread())
				{
					fillOctreeJob.DoWork();
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
}
