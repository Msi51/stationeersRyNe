using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public interface IThreadedWorker : IListable
{
	const int NAME_LENGTH = 24;

	static List<IThreadedWorker> ManagedThreads;

	string GetName { get; }

	float AverageTick { get; }

	float LastTick { get; }

	int JobCount { get; }

	static void Register(IThreadedWorker thread)
	{
		if (ManagedThreads.Contains(thread))
		{
			throw new Exception("thread " + thread.GetName + " already registered");
		}
		ManagedThreads.Add(thread);
	}

	static void ResetThreadStatistics()
	{
		foreach (IThreadedWorker managedThread in ManagedThreads)
		{
			managedThread.ResetStatistics();
		}
	}

	void ResetStatistics();

	static void PrintThreadInfo()
	{
		ThreadPool.GetMinThreads(out var workerThreads, out var _);
		ThreadPool.GetMaxThreads(out var workerThreads2, out var _);
		Debug.Log($"thread pool is {workerThreads} to {workerThreads2}");
	}

	static IThreadedWorker()
	{
		ManagedThreads = new List<IThreadedWorker>();
	}
}
