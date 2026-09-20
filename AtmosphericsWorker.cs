using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Networks;
using UnityEngine;
using UnityEngine.Profiling;

public class AtmosphericsWorker
{
	public enum Job
	{
		None,
		CalculateOpenNeighbours,
		InternalReactions,
		ThingFireTick,
		CacheData,
		Mix,
		CacheRoomData,
		NetworkTick
	}

	public static readonly int WorkerThreads = Math.Max(Environment.ProcessorCount - 2, 4);

	private WorkerState _state;

	private static AtmosphericsWorker[] _workers;

	private static AtmosphericsWorker _nextWorker;

	private static CountdownEvent _completionEvent = new CountdownEvent(0);

	private readonly WaitCallback _calculateOpenNeighbours;

	private readonly WaitCallback _internalReactions;

	private readonly WaitCallback _thingFireTick;

	private readonly WaitCallback _cacheData;

	private readonly WaitCallback _mixInWorld;

	private readonly WaitCallback _cacheRoomData;

	private readonly WaitCallback _networkTick;

	private readonly Queue<Atmosphere> _atmospheres = new Queue<Atmosphere>(512);

	public readonly MovingAverage MovingAverage = new MovingAverage(50);

	private int _score;

	private readonly Queue<Thing> _things = new Queue<Thing>(512);

	private readonly Queue<Room> _rooms = new Queue<Room>(128);

	private readonly Queue<AtmosphericsNetwork> _networks = new Queue<AtmosphericsNetwork>(128);

	public string PaddedName = "Unknown?";

	private int _index;

	private Stopwatch _workerStopwatch = new Stopwatch();

	public Stopwatch TaskStopwatch = new Stopwatch();

	private double _lastTickTime;

	private double _avgTickTime;

	private int _exceptions;

	private int lastTimeIndex;

	private readonly double[] _times = new double[40];

	public string GetName { get; }

	public float AverageTick => (float)_avgTickTime;

	public static int GetWorkerScore(int threadIndex)
	{
		return _workers[threadIndex]._score;
	}

	public AtmosphericsWorker(int i)
	{
		GetName = "Atmospherics" + StringManager.Get(i);
		PaddedName = GetName.PadRight(24);
		_internalReactions = InternalReactionsTask;
		_calculateOpenNeighbours = CalculateOpenNeighboursTask;
		_thingFireTick = ThingFireTickTask;
		_cacheData = CacheDataTask;
		_mixInWorld = MixTask;
		_cacheRoomData = CacheRoomDataTask;
		_networkTick = NetworkTickTask;
	}

	public static void Initialize()
	{
		_workers = new AtmosphericsWorker[WorkerThreads];
		for (int i = 0; i < WorkerThreads; i++)
		{
			_workers[i] = new AtmosphericsWorker(i);
		}
	}

	public static void Execute(Job job)
	{
		if (job == Job.None)
		{
			return;
		}
		int num = 0;
		AtmosphericsWorker[] workers = _workers;
		foreach (AtmosphericsWorker atmosphericsWorker in workers)
		{
			if (atmosphericsWorker._state != WorkerState.Idle)
			{
				throw new Exception("thread '" + atmosphericsWorker.GetName + "' not ready");
			}
			if (HasWork(atmosphericsWorker, job))
			{
				num++;
			}
		}
		if (num == 0)
		{
			return;
		}
		_completionEvent.Reset(num);
		workers = _workers;
		foreach (AtmosphericsWorker atmosphericsWorker2 in workers)
		{
			if (HasWork(atmosphericsWorker2, job))
			{
				atmosphericsWorker2._state = WorkerState.Scheduled;
				switch (job)
				{
				case Job.CalculateOpenNeighbours:
					ThreadPool.QueueUserWorkItem(atmosphericsWorker2._calculateOpenNeighbours);
					break;
				case Job.InternalReactions:
					ThreadPool.QueueUserWorkItem(atmosphericsWorker2._internalReactions);
					break;
				case Job.ThingFireTick:
					ThreadPool.QueueUserWorkItem(atmosphericsWorker2._thingFireTick);
					break;
				case Job.CacheData:
					ThreadPool.QueueUserWorkItem(atmosphericsWorker2._cacheData);
					break;
				case Job.Mix:
					ThreadPool.QueueUserWorkItem(atmosphericsWorker2._mixInWorld);
					break;
				case Job.CacheRoomData:
					ThreadPool.QueueUserWorkItem(atmosphericsWorker2._cacheRoomData);
					break;
				case Job.NetworkTick:
					ThreadPool.QueueUserWorkItem(atmosphericsWorker2._networkTick);
					break;
				}
			}
		}
	}

	private static bool HasWork(AtmosphericsWorker worker, Job job)
	{
		return job switch
		{
			Job.ThingFireTick => worker._things.Count > 0, 
			Job.CacheRoomData => worker._rooms.Count > 0, 
			Job.NetworkTick => worker._networks.Count > 0, 
			_ => worker._atmospheres.Count > 0, 
		};
	}

	public static void WaitForCompletion()
	{
		_completionEvent.Wait();
	}

	public static void Clear()
	{
		if (_workers == null)
		{
			return;
		}
		AtmosphericsWorker[] workers = _workers;
		foreach (AtmosphericsWorker atmosphericsWorker in workers)
		{
			WorkerState state = atmosphericsWorker._state;
			if (state == WorkerState.Running || state == WorkerState.Scheduled)
			{
				atmosphericsWorker._state = WorkerState.Abort;
			}
		}
	}

	public static bool IsWorking()
	{
		if (_workers == null)
		{
			return false;
		}
		AtmosphericsWorker[] workers = _workers;
		for (int i = 0; i < workers.Length; i++)
		{
			WorkerState state = workers[i]._state;
			if (state == WorkerState.Running || state == WorkerState.Scheduled)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsIdle()
	{
		if (_workers == null)
		{
			return false;
		}
		AtmosphericsWorker[] workers = _workers;
		for (int i = 0; i < workers.Length; i++)
		{
			WorkerState state = workers[i]._state;
			if (state == WorkerState.Running || state == WorkerState.Scheduled || state == WorkerState.Abort)
			{
				return false;
			}
		}
		return true;
	}

	public static void ClearScores()
	{
		if (_workers != null)
		{
			AtmosphericsWorker[] workers = _workers;
			for (int i = 0; i < workers.Length; i++)
			{
				workers[i]._score = 0;
			}
		}
	}

	public static void Assign(Atmosphere atmosphere, bool weighted = true)
	{
		if (atmosphere != null)
		{
			AtmosphericsWorker nextWorker = GetNextWorker();
			nextWorker._score += ((!weighted) ? 1 : atmosphere.WorkScore());
			nextWorker._atmospheres.Enqueue(atmosphere);
		}
	}

	public static void Assign(Atmosphere atmosphere, int threadIndex)
	{
		AtmosphericsWorker obj = _workers[threadIndex];
		obj._score++;
		obj._atmospheres.Enqueue(atmosphere);
	}

	public static void Assign(Thing thing)
	{
		if ((object)thing != null)
		{
			AtmosphericsWorker nextWorker = GetNextWorker();
			nextWorker._score++;
			nextWorker._things.Enqueue(thing);
		}
	}

	public static void Assign(Room room)
	{
		if (room != null && !room.IsDeletionCandidate)
		{
			AtmosphericsWorker nextWorker = GetNextWorker();
			nextWorker._score += room.Grids.Count;
			nextWorker._rooms.Enqueue(room);
		}
	}

	public static void Assign(AtmosphericsNetwork network)
	{
		if (network != null)
		{
			AtmosphericsWorker nextWorker = GetNextWorker();
			nextWorker._score++;
			nextWorker._networks.Enqueue(network);
		}
	}

	private static AtmosphericsWorker GetNextWorker()
	{
		if (_nextWorker == null)
		{
			_nextWorker = _workers[0];
		}
		else
		{
			for (int i = 0; i < _workers.Length; i++)
			{
				AtmosphericsWorker atmosphericsWorker = _workers[i];
				if (atmosphericsWorker._score < _nextWorker._score)
				{
					_nextWorker = atmosphericsWorker;
				}
			}
		}
		return _nextWorker;
	}

	public static int GetLowestScoreThreadIndex()
	{
		int num = 0;
		AtmosphericsWorker atmosphericsWorker = _workers[num];
		for (int i = num; i < _workers.Length; i++)
		{
			AtmosphericsWorker atmosphericsWorker2 = _workers[i];
			if (atmosphericsWorker2._score < atmosphericsWorker._score)
			{
				atmosphericsWorker = atmosphericsWorker2;
				num = i;
			}
		}
		return num;
	}

	private void ThingFireTickTask(object state)
	{
		if (_things.Count == 0)
		{
			_state = WorkerState.Idle;
			_completionEvent.Signal();
			return;
		}
		InitTask();
		try
		{
			while (_things.Count > 0 && _state != WorkerState.Abort)
			{
				_things.Dequeue()?.OnFireTick();
			}
		}
		catch (Exception e)
		{
			UnityMainThreadDispatcher.Instance()?.Enqueue(LogException(e));
			_exceptions++;
		}
		CleanUpTask();
	}

	private void MixTask(object state)
	{
		if (_atmospheres.Count == 0)
		{
			_state = WorkerState.Idle;
			_completionEvent.Signal();
			return;
		}
		InitTask();
		try
		{
			while (_atmospheres.Count > 0 && _state != WorkerState.Abort)
			{
				_atmospheres.Dequeue()?.Mix();
			}
			Profiler.EndThreadProfiling();
		}
		catch (Exception e)
		{
			Profiler.EndThreadProfiling();
			UnityMainThreadDispatcher.Instance()?.Enqueue(LogException(e));
			_exceptions++;
		}
		CleanUpTask();
	}

	private void CacheRoomDataTask(object state)
	{
		if (_rooms.Count == 0)
		{
			_state = WorkerState.Idle;
			_completionEvent.Signal();
			return;
		}
		InitTask();
		try
		{
			while (_rooms.Count > 0 && _state != WorkerState.Abort)
			{
				_rooms.Dequeue()?.CacheRoomData();
			}
		}
		catch (Exception e)
		{
			UnityMainThreadDispatcher.Instance()?.Enqueue(LogException(e));
			_exceptions++;
		}
		CleanUpTask();
	}

	[SkipLocalsInit]
	private void NetworkTickTask(object state)
	{
		if (_networks.Count == 0)
		{
			_state = WorkerState.Idle;
			_completionEvent.Signal();
			return;
		}
		InitTask();
		try
		{
			Span<ThingRef<IReferencable>> buffer = stackalloc ThingRef<IReferencable>[8192];
			while (_networks.Count > 0 && _state != WorkerState.Abort)
			{
				_networks.Dequeue()?.OnAtmosphericTick(buffer);
			}
			Profiler.EndThreadProfiling();
		}
		catch (Exception e)
		{
			Profiler.EndThreadProfiling();
			UnityMainThreadDispatcher.Instance()?.Enqueue(LogException(e));
			_exceptions++;
		}
		CleanUpTask();
	}

	private void CacheDataTask(object state)
	{
		if (_atmospheres.Count == 0)
		{
			_state = WorkerState.Idle;
			_completionEvent.Signal();
			return;
		}
		InitTask();
		try
		{
			while (_atmospheres.Count > 0 && _state != WorkerState.Abort)
			{
				Atmosphere atmosphere = _atmospheres.Dequeue();
				if (atmosphere != null)
				{
					atmosphere.UpdateCache();
					if (!atmosphere.IsGlobalAtmosphere && !atmosphere.IsCachable && !atmosphere.ForceNonCachable)
					{
						atmosphere.IsCachable = true;
					}
				}
			}
		}
		catch (Exception e)
		{
			UnityMainThreadDispatcher.Instance()?.Enqueue(LogException(e));
			_exceptions++;
		}
		CleanUpTask();
	}

	[SkipLocalsInit]
	private void CalculateOpenNeighboursTask(object state)
	{
		if (_atmospheres.Count == 0)
		{
			_state = WorkerState.Idle;
			_completionEvent.Signal();
			return;
		}
		InitTask();
		try
		{
			Span<Grid3> bufClosed = stackalloc Grid3[6];
			Span<Grid3> bufOpen = stackalloc Grid3[6];
			while (_atmospheres.Count > 0 && _state != WorkerState.Abort)
			{
				Atmosphere atmosphere = _atmospheres.Dequeue();
				if (atmosphere != null && atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World)
				{
					atmosphere.GetOpenAirNeighbors(bufClosed, bufOpen);
				}
			}
		}
		catch (Exception e)
		{
			UnityMainThreadDispatcher.Instance()?.Enqueue(LogException(e));
			_exceptions++;
		}
		CleanUpTask();
	}

	private void InternalReactionsTask(object state)
	{
		if (_atmospheres.Count == 0)
		{
			_state = WorkerState.Idle;
			_completionEvent.Signal();
			return;
		}
		InitTask();
		try
		{
			while (_atmospheres.Count > 0 && _state != WorkerState.Abort)
			{
				Atmosphere atmosphere = _atmospheres.Dequeue();
				if (atmosphere != null)
				{
					long elapsedMilliseconds = _workerStopwatch.ElapsedMilliseconds;
					AtmosphericsController.AtmosphereJob(atmosphere);
					long num = _workerStopwatch.ElapsedMilliseconds - elapsedMilliseconds;
					if (num > 1)
					{
						atmosphere.LastTickScore = (int)((float)num * 15f);
					}
					else
					{
						atmosphere.LastTickScore = 1;
					}
				}
			}
		}
		catch (Exception e)
		{
			UnityMainThreadDispatcher.Instance()?.Enqueue(LogException(e));
			_exceptions++;
		}
		CleanUpTask();
	}

	private void InitTask()
	{
		if (_state != WorkerState.Abort)
		{
			_state = WorkerState.Running;
		}
		Thread.CurrentThread.Priority = Settings.NonFrameCriticalThreadPriority;
		_workerStopwatch.Reset();
		_workerStopwatch.Start();
	}

	private void CleanUpTask()
	{
		_workerStopwatch.Stop();
		_lastTickTime = _workerStopwatch.Elapsed.TotalMilliseconds;
		_times[lastTimeIndex] = _lastTickTime;
		lastTimeIndex++;
		if (lastTimeIndex < 0 || lastTimeIndex >= _times.Length)
		{
			lastTimeIndex = 0;
		}
		_avgTickTime = MovingAverage.Update(_lastTickTime);
		_atmospheres.Clear();
		_rooms.Clear();
		_things.Clear();
		_networks.Clear();
		Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
		_state = WorkerState.Idle;
		_completionEvent.Signal();
	}

	private static IEnumerator LogException(Exception e)
	{
		UnityEngine.Debug.LogException(e);
		yield break;
	}

	private double RecentMax()
	{
		double num = 0.0;
		double[] times = _times;
		foreach (double val in times)
		{
			num = Math.Max(num, val);
		}
		return num;
	}

	public void DrawInList(ref int index)
	{
		ConsoleWindow.Print($"{StringManager.Get(index)}\t{PaddedName}\t{_state}\t{StringManager.Get(_exceptions)} err\t{StringManager.Get(_avgTickTime):00.000}ms avg\t{StringManager.Get(RecentMax()):00.000}ms max");
	}
}
