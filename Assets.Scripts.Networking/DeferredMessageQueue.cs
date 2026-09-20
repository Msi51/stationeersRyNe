using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Networking;

public static class DeferredMessageQueue
{
	private struct Pending
	{
		public IMessageProcessable Message;

		public long HostId;

		public long Id1;

		public long Id2;

		public long[] ExtraIds;

		public float Deadline;

		public string ActionName;
	}

	private sealed class Clearable : IClearable
	{
		public void Clear()
		{
			DeferredMessageQueue.Clear();
		}
	}

	private static List<Pending> _pending = new List<Pending>();

	private static List<Pending> _scratch = new List<Pending>();

	private static bool _draining;

	private static volatile bool _drainRequested;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void RegisterWithClearables()
	{
		Clearables.Register(new Clearable());
	}

	public static void DeferUntilExists(IMessageProcessable msg, long hostId, long id1, float timeout, string actionName)
	{
		_pending.Add(new Pending
		{
			Message = msg,
			HostId = hostId,
			Id1 = id1,
			Id2 = 0L,
			ExtraIds = null,
			Deadline = Time.realtimeSinceStartup + timeout,
			ActionName = actionName
		});
	}

	public static void DeferUntilExists(IMessageProcessable msg, long hostId, long id1, long id2, float timeout, string actionName)
	{
		_pending.Add(new Pending
		{
			Message = msg,
			HostId = hostId,
			Id1 = id1,
			Id2 = id2,
			ExtraIds = null,
			Deadline = Time.realtimeSinceStartup + timeout,
			ActionName = actionName
		});
	}

	public static void DeferUntilExists(IMessageProcessable msg, long hostId, ReadOnlySpan<long> ids, float timeout, string actionName)
	{
		switch (ids.Length)
		{
		case 0:
			return;
		case 1:
			DeferUntilExists(msg, hostId, ids[0], timeout, actionName);
			return;
		case 2:
			DeferUntilExists(msg, hostId, ids[0], ids[1], timeout, actionName);
			return;
		}
		long[] array = new long[ids.Length];
		for (int i = 0; i < ids.Length; i++)
		{
			array[i] = ids[i];
		}
		_pending.Add(new Pending
		{
			Message = msg,
			HostId = hostId,
			Id1 = 0L,
			Id2 = 0L,
			ExtraIds = array,
			Deadline = Time.realtimeSinceStartup + timeout,
			ActionName = actionName
		});
	}

	public static void NotifyRegistered(long referenceId)
	{
		_drainRequested = true;
	}

	public static void TickExpire()
	{
		if (_drainRequested || _pending.Count != 0)
		{
			_drainRequested = false;
			Drain();
		}
	}

	public static void Clear()
	{
		_pending.Clear();
		_scratch.Clear();
	}

	private static void Drain()
	{
		if (_draining || _pending.Count == 0)
		{
			return;
		}
		_draining = true;
		try
		{
			List<Pending> scratch = _scratch;
			List<Pending> pending = _pending;
			_pending = scratch;
			_scratch = pending;
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			for (int i = 0; i < _scratch.Count; i++)
			{
				Pending p = _scratch[i];
				if (IsResolved(in p))
				{
					try
					{
						p.Message.Process(p.HostId);
					}
					catch (Exception ex)
					{
						ConsoleWindow.PrintError("DeferredMessageQueue: " + p.ActionName + " dispatch threw " + ex.GetType().Name + ": " + ex.Message);
					}
				}
				else if (realtimeSinceStartup > p.Deadline)
				{
					ConsoleWindow.PrintError($"DeferredMessageQueue: {p.ActionName} timed out (id1={p.Id1} id2={p.Id2})");
				}
				else
				{
					_pending.Add(p);
				}
			}
			_scratch.Clear();
		}
		finally
		{
			_draining = false;
		}
	}

	private static bool IsResolved(in Pending p)
	{
		if (p.ExtraIds != null)
		{
			long[] extraIds = p.ExtraIds;
			foreach (long num in extraIds)
			{
				if (num != 0L && !Referencable.Referencables.ContainsKey(num))
				{
					return false;
				}
			}
			return true;
		}
		if (p.Id1 != 0L && !Referencable.Referencables.ContainsKey(p.Id1))
		{
			return false;
		}
		if (p.Id2 != 0L && !Referencable.Referencables.ContainsKey(p.Id2))
		{
			return false;
		}
		return true;
	}
}
