using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;

namespace Rooms;

public static class RoomRegenerator
{
	private static Queue<Grid3> _pendingRegenerate = new Queue<Grid3>();

	private static HashSet<Grid3> _pendingRegenerateHashset = new HashSet<Grid3>();

	private static readonly Action<Structure> StructureRegenerateRoomAction = delegate(Structure structure)
	{
		WorldGrid worldGrid = structure.WorldGrid;
		AddGridForRegenerate(worldGrid);
		AddGridForRegenerate(worldGrid + Grid3.North);
		AddGridForRegenerate(worldGrid + Grid3.South);
		AddGridForRegenerate(worldGrid + Grid3.East);
		AddGridForRegenerate(worldGrid + Grid3.West);
		AddGridForRegenerate(worldGrid + Grid3.Up);
		AddGridForRegenerate(worldGrid + Grid3.Down);
	};

	private static void AddGridForRegenerate(Grid3 grid)
	{
		if (!GridController.LargeGridIsFullOfVoxels(grid) && !GridController.World.IsGridBlockedByStructure(grid) && _pendingRegenerateHashset.Add(grid))
		{
			_pendingRegenerate.Enqueue(grid);
		}
	}

	public static void RegenerateRooms()
	{
		RoomController.World.NextRoomId = 1L;
		_pendingRegenerate.Clear();
		_pendingRegenerateHashset.Clear();
		foreach (Room room in RoomController.World.Rooms)
		{
			if (room.Grids.Count > 0)
			{
				AddGridForRegenerate(room.Grids[0]);
			}
		}
		GridController.AllStructuresPool.ForEach(StructureRegenerateRoomAction);
		for (int num = RoomController.World.Rooms.Count - 1; num >= 0; num--)
		{
			RoomHelper.DeleteRoom(RoomController.World.Rooms[num]);
		}
		RoomEvaluator.Instance.Clear();
		RoomFloodFiller roomFloodFiller = new RoomFloodFiller();
		roomFloodFiller.EvaluateNeighbour = delegate(Grid3 neighbourGrid)
		{
			_pendingRegenerateHashset.Remove(neighbourGrid);
			return false;
		};
		while (_pendingRegenerate.Count > 0)
		{
			Grid3 grid = _pendingRegenerate.Dequeue();
			if (_pendingRegenerateHashset.Remove(grid) && roomFloodFiller.Fill(grid) == FillResult.Success)
			{
				RoomHelper.CreateRoom(roomFloodFiller.ClosedList);
			}
		}
		_pendingRegenerate.Clear();
		_pendingRegenerateHashset.Clear();
	}
}
