using System;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.GridSystem;

public class GridPathfinder
{
	public class PathSegment
	{
		public float G;

		public float H;

		public float F;

		public PathSegment PreviousSegment;

		public NpcPathGrid CurrentPathGrid;

		public Grid3 EntryNodePos;

		public Grid3 ExitNodePos;

		public GridController Gridcontroller;

		public PathSegment(float g, float h, NpcPathGrid currentPathGrid, PathSegment prevSegment, GridController gridController)
		{
			G = g;
			H = h;
			F = g + h;
			CurrentPathGrid = currentPathGrid;
			PreviousSegment = prevSegment;
			Gridcontroller = gridController;
		}

		public void UpdateValues(float g, PathSegment prevSegment)
		{
			G = g;
			F = g + H;
			PreviousSegment = prevSegment;
		}
	}

	public class PathInfo
	{
		public Grid3 SquarePos;

		public Grid3 EntryNodePos;

		public Grid3 ExitNodePos;

		public PathInfo()
		{
		}

		public PathInfo(Grid3 squarePos, Grid3 entryNodePos, Grid3 exitNodePos)
		{
			SquarePos = squarePos;
			EntryNodePos = entryNodePos;
			ExitNodePos = exitNodePos;
		}
	}

	public class NpcPathGrid
	{
		public Grid3 Grid;

		public NpcGridType GridType;

		public NpcPathGrid(Grid3 grid, NpcGridType gridType)
		{
			Grid = grid;
			GridType = gridType;
		}
	}

	public enum NpcGridType
	{
		OutDoorStructure,
		OutDoorStairs,
		InRoom,
		InRoomStairs,
		Terrain
	}

	public enum NavGridType
	{
		Blocked,
		EdgeBlocked,
		Floor,
		Ceiling,
		Stairs
	}

	public class NavDebugInfo
	{
		public Grid3 LocalPosition;

		public NavGridType NavType;

		public GridController GridController;
	}

	public bool CanGoDiagonal;

	public bool DebugDisplayPath = true;

	public List<PathSegment> OpenList = new List<PathSegment>();

	public List<PathSegment> ClosedList = new List<PathSegment>();

	private PathSegment _pathEnd;

	public Grid3 EndSquare;

	public List<Grid3> FinalPath;

	public List<Grid3> CheckedSquares;

	public static List<NavDebugInfo> NavigationDebugInfo = new List<NavDebugInfo>();

	public static Vector3 StartCell = Vector3.zero;

	public static Vector3 RandomPosition = Vector3.zero;

	public float MaxDistance;

	public static HashSet<Grid3> DeviceBlockedList = new HashSet<Grid3>();

	private int _checks;

	private static readonly float _distanceThreshold = 2f;

	private static readonly float _half = 0.5f;

	public List<NpcPathGrid> Pathfind(Grid3 start, Grid3 end, float maxDistance, GridController gridController, int maxChecks = 200)
	{
		OpenList.Clear();
		ClosedList.Clear();
		MaxDistance = maxDistance;
		_pathEnd = null;
		EndSquare = end;
		CheckedSquares = new List<Grid3>();
		if (start == end)
		{
			return new List<NpcPathGrid>();
		}
		float h = Vector3.Distance(start.ToVector3(), end.ToVector3());
		NpcPathGrid currentPathGrid = new NpcPathGrid(start, NpcGridType.Terrain);
		PathSegment item = new PathSegment(0f, h, currentPathGrid, null, gridController);
		OpenList.Add(item);
		_checks = 0;
		while (OpenList.Count > 0 && _pathEnd == null && _checks < maxChecks)
		{
			PathSegment bestOptionInOpenList = GetBestOptionInOpenList();
			AddOptionsToOpenList(bestOptionInOpenList);
			OpenList.Remove(bestOptionInOpenList);
			ClosedList.Add(bestOptionInOpenList);
			_checks++;
		}
		if (_pathEnd == null)
		{
			return null;
		}
		return CreatePath();
	}

	private List<NpcPathGrid> CreatePath()
	{
		List<NpcPathGrid> list = new List<NpcPathGrid>();
		while (_pathEnd != null)
		{
			list.Insert(0, _pathEnd.CurrentPathGrid);
			_pathEnd = _pathEnd.PreviousSegment;
		}
		return list;
	}

	private void AddOptionsToOpenList(PathSegment currentSegment)
	{
		foreach (NpcPathGrid walkableGrid in GetWalkableGrids(currentSegment.CurrentPathGrid.Grid, currentSegment.Gridcontroller))
		{
			AddToOpenList(currentSegment, walkableGrid, currentSegment.Gridcontroller);
		}
		if (!currentSegment.Gridcontroller.IsWorld || currentSegment.Gridcontroller.RoomController.GetRoom(currentSegment.CurrentPathGrid.Grid) != null)
		{
			return;
		}
		foreach (NpcPathGrid walkableTerrainGrid in GetWalkableTerrainGrids(currentSegment.CurrentPathGrid.Grid))
		{
			AddToOpenList(currentSegment, walkableTerrainGrid, currentSegment.Gridcontroller);
		}
	}

	public void AddToOpenList(PathSegment currentSegment, NpcPathGrid newGridPath, GridController gridController)
	{
		float num = currentSegment.G + GetDistanceBetweenNeighbors(newGridPath.Grid, currentSegment.CurrentPathGrid.Grid);
		PathSegment pathSegment = ClosedList.ReverseFind((PathSegment seg) => seg.CurrentPathGrid.Grid == newGridPath.Grid);
		if (pathSegment != null)
		{
			return;
		}
		pathSegment = OpenList.Find((PathSegment seg) => seg.CurrentPathGrid.Grid == newGridPath.Grid);
		if (!(num < MaxDistance))
		{
			return;
		}
		if (pathSegment == null)
		{
			float num2 = Vector3.Distance(newGridPath.Grid.ToVector3(), EndSquare.ToVector3());
			PathSegment pathSegment2 = new PathSegment(num, num2, newGridPath, currentSegment, gridController);
			OpenList.Add(pathSegment2);
			if (num2 <= GetScale() / 2f)
			{
				_pathEnd = pathSegment2;
			}
			CheckedSquares.Add(newGridPath.Grid);
		}
		else if (num < pathSegment.G)
		{
			pathSegment.UpdateValues(num, currentSegment);
		}
	}

	private PathSegment GetBestOptionInOpenList()
	{
		float num = float.MaxValue;
		PathSegment result = null;
		foreach (PathSegment open in OpenList)
		{
			if (open.F < num)
			{
				result = open;
				num = open.F;
			}
		}
		return result;
	}

	private float GetDistanceBetweenNeighbors(Grid3 pos1, Grid3 pos2)
	{
		if ((float)Math.Abs(pos1.x - pos2.x) < _distanceThreshold || (float)Math.Abs(pos1.z - pos2.z) < _distanceThreshold)
		{
			return GetScale();
		}
		return GetScale() * 1.4f;
	}

	public float GetScale(Monster monster = null)
	{
		return 2f;
	}

	public static List<NpcPathGrid> GetWalkableTerrainGrids(Grid3 startGrid)
	{
		Span<Grid3> obj = stackalloc Grid3[32];
		int count = 0;
		GridController.PopulateGridNeighbours(obj, ref count, startGrid);
		Span<Grid3> span = obj;
		Span<Grid3> span2 = span.Slice(0, count);
		List<NpcPathGrid> list = new List<NpcPathGrid>();
		Cell cell = GridController.World.GetCell(startGrid);
		Span<Vector3> voxelBuf = stackalloc Vector3[4];
		span = span2;
		for (int i = 0; i < span.Length; i++)
		{
			Grid3 grid = span[i];
			if (GridController.World.RoomController.GetRoom(grid) != null)
			{
				continue;
			}
			Cell cell2 = GridController.World.GetCell(grid);
			if (cell2 != null)
			{
				if ((bool)cell2.Stairs)
				{
					Grid3 exit = cell2.Stairs.Exit;
					if (cell2.Stairs.Entry == startGrid || (cell2.Stairs != null && cell != null && cell.Stairs != null))
					{
						NpcPathGrid item = new NpcPathGrid(exit, NpcGridType.OutDoorStairs);
						list.Add(item);
					}
				}
				continue;
			}
			Cell cell3 = GridController.World.GetCell(grid + Grid3.Down);
			if (cell3 != null && (bool)cell3.Stairs)
			{
				Grid3 exit2 = cell3.Stairs.Exit;
				if (cell3.Stairs.Entry == startGrid || (cell3.Stairs != null && cell != null && cell.Stairs != null))
				{
					NpcPathGrid item2 = new NpcPathGrid(exit2, NpcGridType.OutDoorStairs);
					list.Add(item2);
				}
				continue;
			}
			Grid3 grid2 = grid + Grid3.Down;
			Vector3 face = startGrid.ToVector3().Middle((grid + Grid3.Down).ToVector3());
			if (!GridController.World.IsVoxelFaceOpen(grid2.ToVector3(), face, voxelBuf))
			{
				Vector3 face2 = startGrid.ToVector3().Middle(grid.ToVector3());
				if (GridController.World.IsVoxelFaceOpen(startGrid.ToVector3(), face2, voxelBuf))
				{
					NpcPathGrid item3 = new NpcPathGrid(grid, NpcGridType.Terrain);
					list.Add(item3);
				}
			}
		}
		return list;
	}

	public static List<NpcPathGrid> GetWalkableGrids(Grid3 startGrid, GridController gridController)
	{
		Span<Grid3> obj = stackalloc Grid3[32];
		int count = 0;
		GridController.PopulateGridNeighbours(obj, ref count, startGrid);
		Span<Grid3> span = obj;
		Span<Grid3> span2 = span.Slice(0, count);
		List<NpcPathGrid> list = new List<NpcPathGrid>();
		Cell cell = gridController.GetCell(startGrid);
		bool flag = gridController.IsCellFloor(startGrid) || gridController.IsCellCeiling(startGrid + Grid3.Down);
		bool flag2 = cell != null && (bool)cell.Stairs;
		span = span2;
		for (int i = 0; i < span.Length; i++)
		{
			Grid3 grid = span[i];
			if (DeviceBlockedList.Contains(grid) || DeviceBlockedList.Contains(grid + Grid3.Down) || DeviceBlockedList.Contains(startGrid) || DeviceBlockedList.Contains(startGrid + Grid3.Down))
			{
				continue;
			}
			Cell cell2 = gridController.GetCell(grid);
			Cell cell3 = gridController.GetCell(grid + Grid3.Down);
			NavDebugInfo navDebugInfo = new NavDebugInfo();
			navDebugInfo.GridController = gridController;
			if (gridController.IsCellBlocked(grid))
			{
				navDebugInfo.LocalPosition = grid;
				navDebugInfo.NavType = NavGridType.Blocked;
				NavigationDebugInfo.Add(navDebugInfo);
				continue;
			}
			bool num = cell != null && !cell.IsWalkable((startGrid + grid) * 0.5f);
			bool flag3 = cell2 != null && !cell2.IsWalkable((startGrid + grid) * 0.5f);
			if (num || flag3)
			{
				navDebugInfo.LocalPosition = grid;
				navDebugInfo.NavType = NavGridType.Blocked;
				NavigationDebugInfo.Add(navDebugInfo);
				continue;
			}
			bool flag4 = gridController.IsCellFloor(grid) || gridController.IsCellCeiling(grid + Grid3.Down) || gridController.IsCellBlocked(grid + Grid3.Down);
			bool flag5 = cell2 != null && (bool)cell2.Stairs;
			bool flag6 = cell3 != null && (bool)cell3.Stairs;
			if (flag && flag4 && !flag5 && !flag2)
			{
				navDebugInfo.LocalPosition = grid;
				navDebugInfo.NavType = NavGridType.Floor;
				NavigationDebugInfo.Add(navDebugInfo);
				NpcPathGrid item = new NpcPathGrid(grid, NpcGridType.InRoom);
				list.Add(item);
				continue;
			}
			if (flag && flag5)
			{
				Grid3 exit = cell2.Stairs.Exit;
				if (cell2.Stairs.Entry == startGrid || (cell2.Stairs != null && cell != null && cell.Stairs != null))
				{
					navDebugInfo.LocalPosition = exit;
					navDebugInfo.NavType = NavGridType.Stairs;
					NavigationDebugInfo.Add(navDebugInfo);
					NpcPathGrid item2 = new NpcPathGrid(exit, NpcGridType.InRoomStairs);
					list.Add(item2);
					continue;
				}
			}
			if (flag && flag6)
			{
				Grid3 exit2 = cell3.Stairs.Exit;
				Grid3 entry = cell3.Stairs.Entry;
				if (exit2 == startGrid && cell3.Stairs.Exit == startGrid)
				{
					navDebugInfo.LocalPosition = entry;
					navDebugInfo.NavType = NavGridType.Stairs;
					NavigationDebugInfo.Add(navDebugInfo);
					NpcPathGrid item3 = new NpcPathGrid(entry, NpcGridType.InRoomStairs);
					list.Add(item3);
				}
			}
		}
		return list;
	}

	public static Grid3 GetStairExitPoint(Grid3 exitPosition, List<Grid3> pathList, GridController gridController)
	{
		Cell cell = gridController.GetCell(exitPosition);
		bool flag = cell != null && (bool)cell.Stairs;
		if (RoomManager.OpenPathfindingTasks != null && RoomManager.OpenPathfindingTasks.Count > 0 && RoomManager.OpenPathfindingTasks[0].FinishGrid == exitPosition)
		{
			return exitPosition;
		}
		if (flag)
		{
			if (CheckBlockedStair(cell.Stairs, gridController))
			{
				return exitPosition;
			}
			pathList.Add(cell.Stairs.Entry);
			pathList.Add(cell.Stairs.Exit);
		}
		pathList.Add(exitPosition);
		if (flag)
		{
			return GetStairEntryPoint(cell.Stairs.Exit, pathList, gridController);
		}
		return exitPosition;
	}

	private static Grid3 GetStairEntryPoint(Grid3 entryPosition, List<Grid3> pathList, GridController gridController)
	{
		Cell cell = gridController.GetCell(entryPosition + Grid3.Down * 2f);
		bool flag = cell != null && (bool)cell.Stairs;
		if (RoomManager.OpenPathfindingTasks != null && RoomManager.OpenPathfindingTasks.Count > 0 && RoomManager.OpenPathfindingTasks[0].FinishGrid == entryPosition)
		{
			return entryPosition;
		}
		if (flag)
		{
			if (CheckBlockedStair(cell.Stairs, gridController))
			{
				return entryPosition;
			}
			pathList.Add(cell.Stairs.Exit);
			pathList.Add(cell.Stairs.Entry);
		}
		pathList.Add(entryPosition);
		if (flag)
		{
			return GetStairEntryPoint(cell.Stairs.Entry, pathList, gridController);
		}
		return entryPosition;
	}

	public static Grid3 GetStairEntryPoint(Grid3 entryPosition, GridController gridController)
	{
		Cell cell = gridController.GetCell(entryPosition + Grid3.Down * 2f);
		bool flag = cell != null && (bool)cell.Stairs;
		if (RoomManager.OpenPathfindingTasks != null && RoomManager.OpenPathfindingTasks.Count > 0 && RoomManager.OpenPathfindingTasks[0].FinishGrid == entryPosition)
		{
			return entryPosition;
		}
		if (flag)
		{
			return GetStairEntryPoint(cell.Stairs.Entry, gridController);
		}
		return entryPosition;
	}

	private static bool CheckBlockedStair(Stairs stairs, GridController gridController)
	{
		Grid3 entry = stairs.Entry;
		Grid3 exit = stairs.Exit;
		Grid3 grid = entry - stairs.Forward.ToGrid() * 2f;
		Grid3 grid2 = entry - stairs.Forward.ToGrid() * 2f + Grid3.Up;
		Grid3 grid3 = exit + stairs.Forward.ToGrid() * 2f;
		Grid3 grid4 = exit + stairs.Forward.ToGrid() * 2f + Grid3.Down;
		Cell cell = gridController.GetCell(entry);
		Cell cell2 = gridController.GetCell(exit);
		Cell cell3 = gridController.GetCell(grid);
		Cell cell4 = gridController.GetCell(grid2);
		Cell cell5 = gridController.GetCell(grid3);
		Cell cell6 = gridController.GetCell(grid4);
		Grid3 worldGrid = (entry + grid) * _half;
		Grid3 worldGrid2 = (exit + grid3) * _half;
		Grid3 worldGrid3 = (entry + grid2) * _half;
		Grid3 worldGrid4 = (exit + grid4) * _half;
		Grid3 worldGrid5 = grid - stairs.Forward.ToGrid();
		Grid3 worldGrid6 = grid2 - stairs.Forward.ToGrid();
		Grid3 worldGrid7 = grid4 + stairs.Forward.ToGrid();
		Grid3 worldGrid8 = grid3 + stairs.Forward.ToGrid();
		bool num = (cell != null && !cell.IsWalkable(worldGrid)) || (cell3 != null && !cell3.IsWalkable(worldGrid));
		bool flag = (cell2 != null && !cell2.IsWalkable(worldGrid2)) || (cell5 != null && !cell5.IsWalkable(worldGrid2));
		bool flag2 = (cell != null && !cell.IsWalkable(worldGrid5)) || (cell3 != null && !cell3.IsWalkable(worldGrid5));
		bool flag3 = (cell2 != null && !cell2.IsWalkable(worldGrid7)) || (cell6 != null && !cell6.IsWalkable(worldGrid7));
		bool flag4 = (cell != null && !cell.IsWalkable(worldGrid6)) || (cell4 != null && !cell4.IsWalkable(worldGrid6));
		bool flag5 = (cell2 != null && !cell2.IsWalkable(worldGrid8)) || (cell5 != null && !cell5.IsWalkable(worldGrid8));
		bool flag6 = (cell != null && !cell.IsWalkable(worldGrid3)) || (cell4 != null && !cell4.IsWalkable(worldGrid3));
		bool flag7 = (cell2 != null && !cell2.IsWalkable(worldGrid4)) || (cell6 != null && !cell6.IsWalkable(worldGrid4));
		return num || flag || flag2 || flag3 || flag4 || flag5 || flag6 || flag7;
	}
}
