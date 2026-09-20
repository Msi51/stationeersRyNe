using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace TerrainSystem.Lods;

public class VeinDeduplicationWorker : ThreadWorker<VeinDeduplicationWorker, VeinCluster>
{
	private static int _nextIndex;

	protected override System.Threading.ThreadPriority Priority => System.Threading.ThreadPriority.Normal;

	public static bool IsAnyWorking()
	{
		return WorkerCollections.VeinDeduplicationWorkers.IsAnyWorking();
	}

	public static void Assign(VeinCluster vein)
	{
		if (_nextIndex >= WorkerCollections.VeinDeduplicationWorkers.Count)
		{
			_nextIndex = 0;
		}
		WorkerCollections.VeinDeduplicationWorkers.GetWorker(_nextIndex).Enqueue(vein);
		_nextIndex++;
	}

	public static void ExecuteAll()
	{
		WorkerCollections.VeinDeduplicationWorkers.ExecuteAll();
	}

	public static void AbortAll()
	{
		WorkerCollections.VeinDeduplicationWorkers.AbortAll();
	}

	public VeinDeduplicationWorker()
	{
		DoTask = VeinDeduplicationTask;
	}

	private void VeinDeduplicationTask(object state)
	{
		if (!StartTask())
		{
			return;
		}
		try
		{
			List<Vein> neighbourVeins = new List<Vein>(32);
			while (_objects.Count > 0 && _state != WorkerState.Abort)
			{
				VeinCluster.DeduplicateMinables(_objects.Dequeue(), ref neighbourVeins);
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
