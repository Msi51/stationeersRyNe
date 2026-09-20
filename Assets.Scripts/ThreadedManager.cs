using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts;

public class ThreadedManager : ManagerBase
{
	[Header("Threaded Manager")]
	[ReadOnly]
	public bool IsRunning;

	public int TickSpeed = 1;

	public Thread WorkingThread;

	protected ManualResetEvent _manualReset = new ManualResetEvent(initialState: false);

	private bool _isPaused;

	private double _elapseTime;

	private DateTime _startTime = DateTime.Now;

	private DateTime _endTime = DateTime.Now;

	public int TotalTickCount { get; private set; }

	protected virtual System.Threading.ThreadPriority ThreadPriority => Settings.FrameCriticalThreadPriority;

	public float TickSpeedSeconds => (float)TickSpeed / 1000f;

	public static bool IsThread => Thread.CurrentThread.ManagedThreadId != GameManager.MainThreadId;

	public bool IsPaused
	{
		get
		{
			return _isPaused;
		}
		set
		{
			_isPaused = value;
			if (_isPaused)
			{
				_manualReset.Reset();
			}
			else
			{
				_manualReset.Set();
			}
		}
	}

	public static List<ThreadedManager> Active { get; set; }

	public string PaddedName { get; set; }

	public void OnApplicationPause(bool pause)
	{
		IsPaused = pause;
	}

	public virtual void StartManager()
	{
		IsRunning = true;
		WorkingThread = new Thread(ThreadedLoop);
		WorkingThread.Priority = ThreadPriority;
		WorkingThread.Start();
		if (!GameManager.IsBatchMode)
		{
			IsPaused = false;
		}
	}

	public virtual void ResetThread()
	{
		if (WorkingThread != null)
		{
			if (WorkingThread.ThreadState != ThreadState.Unstarted)
			{
				WorkingThread.Join(5000);
			}
			WorkingThread.Abort();
			WorkingThread = null;
		}
		WorkingThread = new Thread(ThreadedLoop);
		WorkingThread.Start();
	}

	public string GetStatus()
	{
		if (WorkingThread != null)
		{
			return $"{GetType().Name} Alive : {WorkingThread.IsAlive} {(DateTime.Now - _startTime).TotalMilliseconds:F3} {WorkingThread.ThreadState}";
		}
		return $"{GetType().Name} thread now working";
	}

	public virtual void StopManager()
	{
		if ((bool)this && IsRunning)
		{
			IsRunning = false;
		}
	}

	public virtual void ThreadedWork()
	{
	}

	public void ThreadedLoop()
	{
		while (IsRunning && (bool)this)
		{
			_manualReset.WaitOne();
			_startTime = DateTime.Now;
			ThreadedWork();
			_endTime = DateTime.Now;
			_elapseTime = (_endTime - _startTime).TotalMilliseconds;
			Thread.Sleep(TickSpeed);
			if (TotalTickCount < int.MaxValue)
			{
				TotalTickCount++;
			}
		}
		IsRunning = false;
		WorkingThread = null;
	}

	private void OnApplicationQuit()
	{
		IsRunning = false;
	}
}
