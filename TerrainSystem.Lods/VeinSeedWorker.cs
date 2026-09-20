using System;
using System.Threading;
using UnityEngine;

namespace TerrainSystem.Lods;

public class VeinSeedWorker : ThreadWorker<VeinSeedWorker, VeinSeedJob>
{
	protected override System.Threading.ThreadPriority Priority => System.Threading.ThreadPriority.Normal;

	public static void ExecuteAll()
	{
		WorkerCollections.VeinSeedWorker.ExecuteAll();
	}

	public VeinSeedWorker()
	{
		DoTask = VeinSeedTask;
	}

	private void VeinSeedTask(object state)
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
