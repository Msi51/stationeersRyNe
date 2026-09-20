using System.Collections.Concurrent;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using UnityEngine;

namespace Assets.Scripts;

public class RoomController
{
	public static RoomController World;

	public List<Room> Rooms = new List<Room>();

	public long NextRoomId = 1L;

	public ConcurrentDictionary<Grid3, Room> RoomLookup = new ConcurrentDictionary<Grid3, Room>();

	private static readonly int MAXIterations = 1200;

	public static bool RegenStormCurtain;

	public GridController GridController { get; }

	public RoomController(GridController gridController)
	{
		GridController = gridController;
	}

	public static void SerializeOnJoin(RocketBinaryWriter writer)
	{
		Network.WriteIndex<ushort>(writer, out var count, out var bufferIndex);
		foreach (Room room in World.Rooms)
		{
			writer.WriteInt64(room.RoomId);
			Network.WriteIndex<ushort>(writer, out var count2, out var bufferIndex2);
			foreach (WorldGrid grid in room.Grids)
			{
				writer.WriteWorldGrid(grid);
				count2++;
			}
			Network.WriteIndex(writer, count2, bufferIndex2);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
		writer.WriteInt64(World.NextRoomId);
	}

	public static void DeserializeOnJoin(RocketBinaryReader reader)
	{
		Network.ReadIndex<ushort>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			long id = reader.ReadInt64();
			Room room = new Room();
			Network.ReadIndex<ushort>(reader, out var value2);
			World.Register(room, id);
			for (int j = 0; j < value2; j++)
			{
				room.AddGrid(reader.ReadWorldGrid());
			}
		}
		World.NextRoomId = reader.ReadInt64();
	}

	public void RegisterRoomGridFromWorldSetting(long roomId, WorldGrid worldGrid)
	{
		Room room = Get(roomId);
		if (room == null)
		{
			room = new Room();
			World.NextRoomId = roomId;
			World.Register(room, roomId);
		}
		room.AddGrid(worldGrid);
	}

	public Room GetRoom(Grid3 grid)
	{
		Room value = null;
		RoomLookup?.TryGetValue(grid, out value);
		return value;
	}

	public Room GetRoom(Vector3 worldPosition)
	{
		return GetRoom(new WorldGrid(worldPosition));
	}

	public long GetNextRoomId()
	{
		long nextRoomId = NextRoomId;
		NextRoomId++;
		return nextRoomId;
	}

	public void Register(Room room, long id = 0L)
	{
		Rooms.Add(room);
		Room.AllRooms.Add(room);
		if (id == 0L)
		{
			room.RoomId = NextRoomId;
			NextRoomId++;
		}
		else
		{
			room.RoomId = id;
			NextRoomId = id + 1;
		}
		foreach (WorldGrid grid in room.Grids)
		{
			if (GetRoom(grid) == null)
			{
				AddOrUpdateRoomLookup(grid, room);
				AtmosphericsController.World.SetRoom(grid, room);
			}
		}
		RoomManager.EvaluateRoomTypeRules(room);
		HelperHintsManager.Register(room);
	}

	public void AddOrUpdateRoomLookup(WorldGrid grid, Room newValue)
	{
		RoomLookup[grid.Value] = newValue;
	}

	public void RemoveRoomLookup(WorldGrid grid)
	{
		RoomLookup.TryRemove(grid.Value, out var _);
	}

	public static bool IsRoomValid(Room room)
	{
		if (room != null)
		{
			return !room.IsDeletionCandidate;
		}
		return false;
	}

	public static Room Get(long roomId)
	{
		for (int num = World.Rooms.Count - 1; num >= 0; num--)
		{
			Room room = World.Rooms[num];
			if (room != null && room.RoomId == roomId)
			{
				return room;
			}
		}
		return null;
	}

	public void Deregister(Room room)
	{
		for (int num = room.Grids.Count - 1; num >= 0; num--)
		{
			WorldGrid grid = room.Grids[num];
			RemoveRoomLookup(grid);
			AtmosphericsController.World.SetRoom(grid, null);
		}
		HelperHintsManager.Deregister(room);
		Rooms.Remove(room);
		Room.AllRooms.Remove(room);
	}

	public Room GetRoom(WorldGrid worldGrid)
	{
		return GetRoom(worldGrid.Value);
	}
}
