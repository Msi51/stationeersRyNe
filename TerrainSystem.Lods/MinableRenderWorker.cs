using System;
using UnityEngine;

namespace TerrainSystem.Lods;

public class MinableRenderWorker : ThreadWorker<MinableRenderWorker, MinableRenderJob>
{
	public static bool IsAnyWorking()
	{
		return WorkerCollections.MinableRenderWorkers.IsAnyWorking();
	}

	public static void Assign(MinableRenderJob job)
	{
		WorkerCollections.MinableRenderWorkers.Assign(job);
	}

	public static void ExecuteAll()
	{
		WorkerCollections.MinableRenderWorkers.ExecuteAll();
	}

	public MinableRenderWorker()
	{
		DoTask = RefreshTask;
	}

	private void RefreshTask(object state)
	{
		if (!StartTask())
		{
			return;
		}
		try
		{
			while (_objects.Count > 0 && _state != WorkerState.Abort)
			{
				_objects.Dequeue().DoWork();
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
