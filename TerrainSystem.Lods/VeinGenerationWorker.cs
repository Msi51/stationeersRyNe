using System;
using System.Threading;
using UnityEngine;

namespace TerrainSystem.Lods;

public class VeinGenerationWorker : ThreadWorker<VeinGenerationWorker, VeinTemplate>
{
	private static int _nextIndex;

	protected override System.Threading.ThreadPriority Priority => System.Threading.ThreadPriority.Normal;

	public static void ClearAll()
	{
		if (IsAnyWorking())
		{
			throw new Exception("Initialising VeinWorkers while working");
		}
		_nextIndex = 0;
	}

	public static bool IsAnyWorking()
	{
		return WorkerCollections.VeinGenerationWorkers.IsAnyWorking();
	}

	public static void Assign(VeinTemplate template)
	{
		WorkerCollections.VeinGenerationWorkers.GetWorker(_nextIndex).Enqueue(template);
	}

	public static void IncrementNextIndex()
	{
		_nextIndex++;
		if (_nextIndex >= WorkerCollections.VeinGenerationWorkers.Count)
		{
			_nextIndex = 0;
		}
	}

	public static void ExecuteAll()
	{
		WorkerCollections.VeinGenerationWorkers.ExecuteAll();
	}

	protected override void FinishTask()
	{
		base.FinishTask();
		_nextIndex = 0;
	}

	public static void AbortAll()
	{
		WorkerCollections.VeinGenerationWorkers.AbortAll();
	}

	public VeinGenerationWorker()
	{
		DoTask = VeinGenerationTask;
	}

	private void VeinGenerationTask(object state)
	{
		if (!StartTask())
		{
			return;
		}
		try
		{
			float[] array = new float[27];
			while (_objects.Count > 0 && _state != WorkerState.Abort)
			{
				VeinTemplate template = _objects.Dequeue();
				System.Random random = new System.Random(template.GetSeed(WorldManager.Seed));
				Vein.Generate(template, random, array);
			}
			Array.Clear(array, 0, array.Length);
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
