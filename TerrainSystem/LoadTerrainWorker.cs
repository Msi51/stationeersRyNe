using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace TerrainSystem;

public class LoadTerrainWorker : ThreadWorker<LoadTerrainWorker, LoadTerrainJob>
{
	private const int WORKING_ARRAY_SIZE = 134217728;

	[NativeDisableContainerSafetyRestriction]
	private NativeArray<byte> _workingArray;

	protected override System.Threading.ThreadPriority Priority => System.Threading.ThreadPriority.Normal;

	public static bool IsAnyWorking()
	{
		return WorkerCollections.LoadTerrainWorkers.IsAnyWorking();
	}

	public static void Assign(LoadTerrainJob job)
	{
		WorkerCollections.LoadTerrainWorkers.Assign(job);
	}

	public static void ExecuteAll()
	{
		WorkerCollections.LoadTerrainWorkers.ExecuteAll();
	}

	public static void AbortAll()
	{
		WorkerCollections.LoadTerrainWorkers.AbortAll();
	}

	public LoadTerrainWorker()
	{
		DoTask = WorkerTask;
	}

	public override bool StartTask()
	{
		if (!base.StartTask())
		{
			return false;
		}
		_workingArray = new NativeArray<byte>(134217728, Allocator.Persistent);
		return true;
	}

	protected override void FinishTask()
	{
		if (_workingArray.IsCreated)
		{
			_workingArray.Dispose();
		}
		base.FinishTask();
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
				_objects.Dequeue().DoWork(_workingArray);
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

	public static int GetTotalJobsEnqueued()
	{
		int num = 0;
		LoadTerrainWorker[] workers = WorkerCollections.LoadTerrainWorkers.Workers;
		foreach (LoadTerrainWorker loadTerrainWorker in workers)
		{
			num += loadTerrainWorker.JobCount;
		}
		return num;
	}
}
