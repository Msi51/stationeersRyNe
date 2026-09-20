using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;

namespace Rooms;

public static class RoomHelper
{
	public static void CreateRoom(List<Grid3> grids)
	{
		Room room = new Room();
		foreach (Grid3 grid in grids)
		{
			AddGridToRoom(grid, room);
		}
		RegisterRoom(room, -1L);
	}

	public static void RegisterRoom(Room room, long id = -1L)
	{
		room.RoomId = ((id == -1) ? RoomController.World.GetNextRoomId() : id);
		RoomController.World.Rooms.Add(room);
		Room.AllRooms.Add(room);
		HelperHintsManager.Register(room);
	}

	public static void AddGridToRoom(Grid3 grid, Room room, bool createEmptyAtmosphere = false)
	{
		WorldGrid worldGrid = new WorldGrid(grid);
		room.Grids.Add(worldGrid);
		RoomController.World.RoomLookup[grid] = room;
		if (createEmptyAtmosphere)
		{
			(AtmosphericsController.World.GetAtmosphereLocal(worldGrid) ?? new Atmosphere(worldGrid, 0L)).Room = room;
			return;
		}
		Atmosphere atmosphere = AtmosphericsController.World.CloneGlobalAtmosphere(worldGrid, 0L);
		if (atmosphere != null)
		{
			atmosphere.Room = room;
		}
	}

	public static void DeleteRoom(Room room)
	{
		if (!RoomController.World.Rooms.Contains(room))
		{
			return;
		}
		foreach (WorldGrid grid in room.Grids)
		{
			RoomController.World.RoomLookup.Remove(grid, out var _);
			AtmosphericsController.World.SetRoom(grid, null);
		}
		RoomController.World.Rooms.Remove(room);
		Room.AllRooms.Remove(room);
		HelperHintsManager.Deregister(room);
	}
}
