using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Util;

public class UnityMainThreadDispatcher : ManagerBase
{
	private static readonly Queue<Action> ExecutionQueue = new Queue<Action>();

	private static readonly Queue<ChunkThread> TaskQueue = new Queue<ChunkThread>();

	private static UnityMainThreadDispatcher _instance = null;

	public void ClearAll()
	{
		lock (ExecutionQueue)
		{
			ExecutionQueue.Clear();
		}
		lock (TaskQueue)
		{
			TaskQueue.Clear();
		}
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		lock (ExecutionQueue)
		{
			while (ExecutionQueue.Count > 0)
			{
				Action action = ExecutionQueue.Dequeue();
				if (action.Target != null)
				{
					action();
				}
			}
		}
		lock (TaskQueue)
		{
			while (TaskQueue.Count > 0)
			{
				ChunkThread chunkThread = TaskQueue.Dequeue();
				chunkThread.TargetThread.Start(chunkThread.Parameter);
			}
		}
	}

	public void Enqueue(IEnumerator action)
	{
		lock (ExecutionQueue)
		{
			ExecutionQueue.Enqueue(delegate
			{
				StartCoroutine(action);
			});
		}
	}

	public void Enqueue(ChunkThread thread)
	{
		lock (TaskQueue)
		{
			TaskQueue.Enqueue(thread);
		}
	}

	public void Enqueue(Action action)
	{
		Enqueue(ActionWrapper(action));
	}

	private IEnumerator ActionWrapper(Action a)
	{
		a();
		yield return null;
	}

	public static bool Exists()
	{
		return _instance != null;
	}

	public static UnityMainThreadDispatcher Instance()
	{
		if (!Exists())
		{
			throw new Exception("UnityMainThreadDispatcher could not find the UnityMainThreadDispatcher object. Please ensure you have added the MainThreadExecutor Prefab to your scene.");
		}
		return _instance;
	}

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		if (_instance == null)
		{
			_instance = this;
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		}
	}

	private void OnDestroy()
	{
		_instance = null;
	}
}
