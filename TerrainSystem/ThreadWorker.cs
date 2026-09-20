using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UI.ImGuiUi;

namespace TerrainSystem;

public class ThreadWorker<TWorker, TObject> : IThreadedWorker, IListable where TObject : IThreadable
{
	protected WorkerState _state;

	public string PaddedName = "Unknown?";

	private int _score;

	private int _index;

	protected Queue<TObject> _objects = new Queue<TObject>(2048);

	private double _lastTickTime;

	private double _maxTickTime;

	private double _avgTickTime;

	private readonly MovingAverage _movingAverage = new MovingAverage(50);

	protected int _exceptions;

	protected WaitCallback DoTask;

	protected Stopwatch _workerStopwatch = new Stopwatch();

	public Stopwatch TaskStopwatch = new Stopwatch();

	protected virtual ThreadPriority Priority => ThreadPriority.AboveNormal;

	public int JobCount => _objects.Count;

	public string GetName { get; private set; }

	public float AverageTick => (float)_avgTickTime;

	public float LastTick => (float)_lastTickTime;

	public int Score
	{
		get
		{
			return _score;
		}
		set
		{
			_score = value;
		}
	}

	public virtual bool StartTask()
	{
		if (_objects.Count == 0)
		{
			_state = WorkerState.Idle;
			return false;
		}
		if (_state != WorkerState.Abort)
		{
			_state = WorkerState.Running;
		}
		Thread.CurrentThread.Priority = Priority;
		_workerStopwatch.Reset();
		_workerStopwatch.Start();
		return true;
	}

	public void Setup(int index)
	{
		_index = index;
		GetName = $"{typeof(TObject).Name}{index}";
		PaddedName = GetName.PadRight(24);
		IThreadedWorker.Register(this);
	}

	protected virtual void FinishTask()
	{
		_workerStopwatch.Stop();
		SetTime(_workerStopwatch.Elapsed.TotalMilliseconds);
		Score = 0;
		_objects.Clear();
		_state = WorkerState.Idle;
	}

	private void SetTime(double milliSeconds = 0.0)
	{
		_lastTickTime = milliSeconds;
		_lastTickTime = Math.Max(_lastTickTime, _maxTickTime);
		_avgTickTime = _movingAverage.Update(_lastTickTime);
	}

	public void ResetStatistics()
	{
		_exceptions = 0;
		_avgTickTime = 0.0;
		_maxTickTime = 0.0;
	}

	public void Execute()
	{
		if (_state != WorkerState.Idle)
		{
			throw new Exception("thread worker '" + GetName + "' not ready");
		}
		_state = WorkerState.Scheduled;
		ThreadPool.QueueUserWorkItem(DoTask);
	}

	public void Abort()
	{
		if (_state == WorkerState.Running)
		{
			_state = WorkerState.Abort;
		}
	}

	public bool IsWorking()
	{
		WorkerState state = _state;
		return state == WorkerState.Running || state == WorkerState.Scheduled;
	}

	public void Enqueue(TObject workObject)
	{
		_objects.Enqueue(workObject);
		_score += workObject.ThreadCost;
	}

	public void DrawInList(ref int index)
	{
		ImguiHelper.Text(GetName);
		ImguiHelper.SameLine();
		ImguiHelper.Text("  ");
		ImguiHelper.SameLine();
		ImguiHelper.Text("AverageTime   ");
		ImguiHelper.SameLine();
		ImguiHelper.Text($"{(int)_avgTickTime} ms");
		ImguiHelper.SameLine();
		ImguiHelper.Text("LastTime ");
		ImguiHelper.SameLine();
		ImguiHelper.Text($"{(int)_lastTickTime} ms");
	}
}
