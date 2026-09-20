using System.Collections.Generic;
using System.Diagnostics;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using UnityEngine;

namespace Rooms;

public class RoomEvaluator : MonoBehaviour
{
	public static RoomEvaluator Instance;

	private readonly RoomFloodFiller _filler = new RoomFloodFiller();

	public bool Pause;

	public int LastQueueDepth;

	public int LastGridsProcessed;

	public int LastGridsSkipped;

	public int LastGridsBlocked;

	public int LastFillsRun;

	public int LastFillsSuccess;

	public int LastFillsIterationLimit;

	public int LastTotalIterations;

	public int LastRoomsCreated;

	public int LastAtmosCloned;

	public double LastTotalMs;

	private Queue<Grid3> _pendingRoomChecks = new Queue<Grid3>(100);

	private HashSet<Grid3> _pendingRoomChecksLookup = new HashSet<Grid3>(100);

	private Queue<Grid3> _workingRoomChecks = new Queue<Grid3>(100);

	private HashSet<Grid3> _workingRoomChecksLookup = new HashSet<Grid3>(100);

	public int PendingCount => _pendingRoomChecksLookup.Count;

	private void Awake()
	{
		Instance = this;
		_filler.EvaluateNeighbour = delegate(Grid3 neighbourGrid)
		{
			_workingRoomChecksLookup.Remove(neighbourGrid);
			return false;
		};
	}

	public void Clear()
	{
		_filler.Clear();
		_pendingRoomChecks.Clear();
		_pendingRoomChecksLookup.Clear();
		_workingRoomChecks.Clear();
		_workingRoomChecksLookup.Clear();
	}

	public void ThreadedWork()
	{
		if (GameManager.GameState != GameState.Running || Pause)
		{
			return;
		}
		Queue<Grid3> pendingRoomChecks = _pendingRoomChecks;
		Queue<Grid3> workingRoomChecks = _workingRoomChecks;
		_workingRoomChecks = pendingRoomChecks;
		_pendingRoomChecks = workingRoomChecks;
		HashSet<Grid3> pendingRoomChecksLookup = _pendingRoomChecksLookup;
		HashSet<Grid3> workingRoomChecksLookup = _workingRoomChecksLookup;
		_workingRoomChecksLookup = pendingRoomChecksLookup;
		_pendingRoomChecksLookup = workingRoomChecksLookup;
		Stopwatch stopwatch = Stopwatch.StartNew();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		int num9 = 0;
		RoomController.RegenStormCurtain = false;
		LastQueueDepth = _workingRoomChecks.Count;
		while (_workingRoomChecks.Count > 0)
		{
			RoomController.RegenStormCurtain = true;
			Grid3 grid = _workingRoomChecks.Dequeue();
			if (!_workingRoomChecksLookup.Remove(grid))
			{
				num2++;
				continue;
			}
			if (GridController.World.IsGridBlockedByStructure(grid) || GridController.LargeGridIsFullOfVoxels(grid))
			{
				num3++;
				RoomController.World.GetRoom(grid)?.RemoveGrid(new WorldGrid(grid));
				continue;
			}
			num++;
			num4++;
			FillResult fillResult = _filler.Fill(grid);
			num7 += _filler.ClosedList.Count;
			switch (fillResult)
			{
			case FillResult.Success:
			{
				num5++;
				Room room = new Room();
				long num10 = -1L;
				num8++;
				foreach (Grid3 closed in _filler.ClosedList)
				{
					WorldGrid worldGrid = new WorldGrid(closed);
					Room room2 = RoomController.World.GetRoom(closed);
					if (room2 != null)
					{
						room2.Grids.Remove(worldGrid);
						if (room2.Grids.Count == 0)
						{
							RoomHelper.DeleteRoom(room2);
							if (num10 == -1)
							{
								num10 = room2.RoomId;
							}
						}
					}
					room.Grids.Add(worldGrid);
					RoomController.World.RoomLookup[closed] = room;
					if (GameManager.RunSimulation)
					{
						num9++;
						AtmosphericsController.World.CloneGlobalAtmosphere(worldGrid, 0L).Room = room;
					}
				}
				RoomHelper.RegisterRoom(room, num10);
				continue;
			}
			case FillResult.IterationLimit:
				num6++;
				break;
			}
			foreach (Grid3 closed2 in _filler.ClosedList)
			{
				Room room3 = RoomController.World.GetRoom(closed2);
				if (room3 != null)
				{
					RoomHelper.DeleteRoom(room3);
				}
			}
		}
		stopwatch.Stop();
		LastGridsProcessed = num;
		LastGridsSkipped = num2;
		LastGridsBlocked = num3;
		LastFillsRun = num4;
		LastFillsSuccess = num5;
		LastFillsIterationLimit = num6;
		LastTotalIterations = num7;
		LastRoomsCreated = num8;
		LastAtmosCloned = num9;
		LastTotalMs = stopwatch.Elapsed.TotalMilliseconds;
	}

	public void CheckGrid(Grid3 grid)
	{
		if (_pendingRoomChecksLookup.Add(grid))
		{
			_pendingRoomChecks.Enqueue(grid);
		}
	}

	public void CheckFrame(Grid3 origin)
	{
		CheckGrid(origin);
		CheckGrid(origin + Grid3.North);
		CheckGrid(origin + Grid3.South);
		CheckGrid(origin + Grid3.East);
		CheckGrid(origin + Grid3.West);
		CheckGrid(origin + Grid3.Up);
		CheckGrid(origin + Grid3.Down);
	}

	public void CheckWall(Grid3 origin, Grid3 opposite)
	{
		CheckGrid(origin);
		CheckGrid(opposite);
	}
}
