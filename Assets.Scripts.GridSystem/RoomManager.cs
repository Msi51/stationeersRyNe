using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.GridSystem;

public class RoomManager : ThreadedManager
{
	public enum JobState
	{
		Find,
		Random,
		FindFood
	}

	public class PathfindingTask
	{
		public long TaskId;

		public JobState State;

		public DynamicThing Requester;

		public Grid3 StartGrid;

		public Grid3 FinishGrid;

		public List<GridPathfinder.NpcPathGrid> Result;

		public IAnimalFood FoundFood;

		public void OnComplete()
		{
			Requester.FollowPath(this);
		}
	}

	public static Dictionary<WorldGrid, List<Thing>> RoomContributors = new Dictionary<WorldGrid, List<Thing>>();

	public static List<RoomTypeRule> RoomTypeRules = new List<RoomTypeRule>();

	public static HashSet<int> ContributingPrefabs = new HashSet<int>();

	private static List<Grid3> PotentialPositions = new List<Grid3>();

	public static long PathfindingTaskId = 0L;

	public static List<PathfindingTask> OpenPathfindingTasks = new List<PathfindingTask>();

	public static RoomManager Instance;

	private GridPathfinder _pathfinder;

	protected override System.Threading.ThreadPriority ThreadPriority => Settings.NonFrameCriticalThreadPriority;

	public static void EvaluateRoomTypeRules(Room room)
	{
		if (room == null)
		{
			return;
		}
		List<Thing> list = new List<Thing>();
		foreach (WorldGrid grid in room.Grids)
		{
			if (RoomContributors.TryGetValue(grid, out var value))
			{
				list.AddRange(value);
			}
		}
		List<RoomTypeRule> list2 = new List<RoomTypeRule>();
		foreach (RoomTypeRule roomTypeRule2 in RoomTypeRules)
		{
			if (roomTypeRule2.Evaluate(list, room.Grids.Count))
			{
				list2.Add(roomTypeRule2);
			}
		}
		if (list2.Count == 1)
		{
			RoomTypeRule roomTypeRule = list2[0];
			room.RoomType = roomTypeRule.RoomType;
		}
		else
		{
			room.RoomType = RoomType.Default;
		}
	}

	public static void AddContributingPrefab(int prefabHash)
	{
		if (!ContributingPrefabs.Contains(prefabHash))
		{
			ContributingPrefabs.Add(prefabHash);
		}
	}

	public static void AddRoomTypeRule(RoomTypeRuleData data)
	{
		RoomTypeRules.Add(new RoomTypeRule(data));
	}

	public static void AddRoomContributor(WorldGrid grid, Thing thing)
	{
		if (RoomContributors.ContainsKey(grid))
		{
			RoomContributors[grid].Add(thing);
		}
		else
		{
			RoomContributors.Add(grid, new List<Thing> { thing });
		}
		Room room = RoomController.World.GetRoom(grid);
		if (room != null)
		{
			EvaluateRoomTypeRules(room);
		}
	}

	public static void RemoveRoomContributor(WorldGrid grid, Thing thing)
	{
		if (RoomContributors.ContainsKey(grid))
		{
			RoomContributors[grid].Remove(thing);
			if (RoomContributors[grid].Count == 0)
			{
				RoomContributors.Remove(grid);
			}
		}
		Room room = RoomController.World.GetRoom(grid);
		if (room != null)
		{
			EvaluateRoomTypeRules(room);
		}
	}

	public static long RegisterNewTask(DynamicThing requester, Grid3 startGrid, Grid3 finishGrid, JobState state = JobState.Find)
	{
		PathfindingTaskId++;
		PathfindingTask item = new PathfindingTask
		{
			TaskId = PathfindingTaskId,
			Requester = requester,
			StartGrid = startGrid,
			FinishGrid = finishGrid,
			State = state
		};
		OpenPathfindingTasks.Add(item);
		return PathfindingTaskId;
	}

	public static long RegisterNewSearchTask(DynamicThing requester, Grid3 startGrid)
	{
		PathfindingTaskId++;
		PathfindingTask item = new PathfindingTask
		{
			TaskId = PathfindingTaskId,
			Requester = requester,
			StartGrid = startGrid,
			State = JobState.FindFood
		};
		OpenPathfindingTasks.Add(item);
		return PathfindingTaskId;
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
	}

	public override void StartManager()
	{
		_pathfinder = GridManager.PathFinder;
		base.StartManager();
	}

	public override void ThreadedWork()
	{
		base.ThreadedWork();
		if (!GameManager.RunSimulation || OpenPathfindingTasks.Count <= 0 || GameManager.GameState != GameState.Running)
		{
			return;
		}
		try
		{
			PathfindingTask pathfindingTask = OpenPathfindingTasks[0];
			if (pathfindingTask != null)
			{
				switch (pathfindingTask.State)
				{
				case JobState.Find:
					pathfindingTask.Result = _pathfinder.Pathfind(pathfindingTask.StartGrid, pathfindingTask.FinishGrid, 10000f, pathfindingTask.Requester.GridController, 5000);
					OpenPathfindingTasks.RemoveAt(0);
					pathfindingTask.OnComplete();
					break;
				case JobState.Random:
					pathfindingTask.Result = _pathfinder.Pathfind(pathfindingTask.StartGrid, pathfindingTask.FinishGrid, 20f, pathfindingTask.Requester.GridController, 10);
					OpenPathfindingTasks.RemoveAt(0);
					pathfindingTask.OnComplete();
					break;
				case JobState.FindFood:
				{
					if (Plant.AllEdibles.Count < 1)
					{
						OpenPathfindingTasks.RemoveAt(0);
						break;
					}
					IAnimalFood animalFood = Plant.AllEdibles[0];
					float num = RocketMath.DistanceSquared(pathfindingTask.Requester.Position, animalFood.Position);
					foreach (IAnimalFood allEdible in Plant.AllEdibles)
					{
						float num2 = RocketMath.DistanceSquared(pathfindingTask.Requester.Position, allEdible.Position);
						if (num2 < num)
						{
							animalFood = allEdible;
							num = num2;
						}
					}
					pathfindingTask.FoundFood = animalFood;
					pathfindingTask.Result = _pathfinder.Pathfind(pathfindingTask.StartGrid, animalFood.GridPosition, 20f, pathfindingTask.Requester.GridController, 1000);
					OpenPathfindingTasks.RemoveAt(0);
					pathfindingTask.OnComplete();
					break;
				}
				}
			}
			else
			{
				OpenPathfindingTasks.RemoveAt(0);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(ex.Message + " // " + ex.StackTrace);
		}
	}

	public static void ClearAll()
	{
		PathfindingTaskId = 0L;
		OpenPathfindingTasks.Clear();
		PotentialPositions.Clear();
		Room.AllRooms.Clear();
		RoomContributors.Clear();
	}
}
