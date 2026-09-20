using System;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainSystem.Lods;

public class LodRequestWorker : ThreadWorker<LodRequestWorker, ILodRequester>
{
	private List<LodGroup> _lods;

	private List<LodManager.LodRequest> _requested = new List<LodManager.LodRequest>(512);

	private List<LodManager.LodRequest> _unrequested = new List<LodManager.LodRequest>(512);

	public static bool IsAnyWorking()
	{
		return WorkerCollections.LodRequestWorkers.IsAnyWorking();
	}

	public static void Assign(ILodRequester requester)
	{
		WorkerCollections.LodRequestWorkers.Assign(requester);
	}

	public static void ExecuteAll()
	{
		WorkerCollections.LodRequestWorkers.ExecuteAll();
	}

	public static void AbortAll()
	{
		WorkerCollections.LodRequestWorkers.AbortAll();
	}

	public LodRequestWorker()
	{
		DoTask = RequestTask;
		_lods = new List<LodGroup>();
		for (int i = 0; i < 6; i++)
		{
			_lods.Add(new LodGroup(i, new HashSet<Vector3Int>(1024)));
		}
	}

	protected override void FinishTask()
	{
		LodManager.AddRequests(_requested, _unrequested);
		base.FinishTask();
	}

	public override bool StartTask()
	{
		_requested.Clear();
		_unrequested.Clear();
		return base.StartTask();
	}

	private void RequestTask(object state)
	{
		if (!StartTask())
		{
			return;
		}
		try
		{
			while (_objects.Count > 0 && _state != WorkerState.Abort)
			{
				ILodRequester lodRequester = _objects.Dequeue();
				if (lodRequester == null || !lodRequester.CanThread())
				{
					continue;
				}
				LodManager.CalculateLods(lodRequester, _lods);
				foreach (LodGroup lod in _lods)
				{
					HashSet<Vector3Int> indices = lod.Indices;
					HashSet<Vector3Int> hashSet = lodRequester.RequestedLods[lod.Level];
					foreach (Vector3Int item in indices)
					{
						if (!hashSet.Contains(item))
						{
							_requested.Add(new LodManager.LodRequest(item, lod.Level, lodRequester));
						}
					}
					foreach (Vector3Int item2 in hashSet)
					{
						if (!indices.Contains(item2))
						{
							_unrequested.Add(new LodManager.LodRequest(item2, lod.Level, lodRequester));
						}
					}
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
