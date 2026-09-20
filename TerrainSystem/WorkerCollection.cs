using Assets.Scripts.Serialization;
using UnityEngine;

namespace TerrainSystem;

public class WorkerCollection<TWorker, TObject> where TWorker : ThreadWorker<TWorker, TObject>, IThreadedWorker, new() where TObject : IThreadable
{
	public TWorker[] Workers;

	protected TWorker NextWorker;

	private const int MIN_WORKERS = 1;

	private const int MAX_WORKERS_FLOOR = 4;

	public int Count => Workers.Length;

	public TWorker GetWorker(int index)
	{
		return Workers[index];
	}

	public void Initialize(int quantity)
	{
		int num = Mathf.Max(Settings.CurrentData.MaxConcurrentWorkers, 4);
		if (quantity > num)
		{
			quantity = num;
		}
		quantity = Mathf.Max(1, quantity);
		Workers = new TWorker[quantity];
		for (int i = 0; i < quantity; i++)
		{
			TWorker val = new TWorker();
			val.Setup(i);
			Workers[i] = val;
		}
	}

	public void ExecuteAll()
	{
		TWorker[] workers = Workers;
		for (int i = 0; i < workers.Length; i++)
		{
			workers[i].Execute();
		}
	}

	public void AbortAll()
	{
		if (Workers != null)
		{
			TWorker[] workers = Workers;
			for (int i = 0; i < workers.Length; i++)
			{
				workers[i].Abort();
			}
		}
	}

	public int CountWorking()
	{
		int num = 0;
		TWorker[] workers = Workers;
		for (int i = 0; i < workers.Length; i++)
		{
			if (workers[i].IsWorking())
			{
				num++;
			}
		}
		return num;
	}

	public bool IsAnyWorking()
	{
		if (Workers == null)
		{
			return false;
		}
		TWorker[] workers = Workers;
		for (int i = 0; i < workers.Length; i++)
		{
			if (workers[i].IsWorking())
			{
				return true;
			}
		}
		return false;
	}

	public void Assign(TObject tObject)
	{
		if (tObject != null && tObject.CanThread())
		{
			GetNextWorker().Enqueue(tObject);
		}
	}

	private TWorker GetNextWorker()
	{
		if (NextWorker == null)
		{
			NextWorker = Workers[0];
		}
		else
		{
			TWorker[] workers = Workers;
			foreach (TWorker val in workers)
			{
				if (val.Score < NextWorker.Score)
				{
					NextWorker = val;
				}
			}
		}
		return NextWorker;
	}
}
