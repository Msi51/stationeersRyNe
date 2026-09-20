using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using Networks;
using TerrainSystem;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts;

public class GridController : IDisposable
{
	public struct GridAirState
	{
		public bool CanAirPass;

		public void Reset()
		{
			CanAirPass = true;
		}

		public static bool operator ==(GridAirState a, GridAirState b)
		{
			return a.CanAirPass != b.CanAirPass;
		}

		public static bool operator !=(GridAirState a, GridAirState b)
		{
			return !(a == b);
		}
	}

	public static GridController World;

	public RoomController RoomController;

	public AtmosphericsController AtmosphericsController;

	public readonly Dictionary<WorldGrid, List<Structure>> GridWatchers = new Dictionary<WorldGrid, List<Structure>>();

	public ConcurrentDictionary<Grid3, Cell> GridCells = new ConcurrentDictionary<Grid3, Cell>();

	public Dictionary<Grid3, SmallCell> SmallGridCells = new Dictionary<Grid3, SmallCell>();

	public readonly Dictionary<Grid3, HashSet<Structure>> FaceLookup = new Dictionary<Grid3, HashSet<Structure>>();

	private const int MAX_STRUCTURES = 65536;

	public static readonly DensePool<Structure> AllStructuresPool = new DensePool<Structure>("AllStructuresPool", 65536);

	private const int MAX_SERVER_TICK_STRUCTURES = 8192;

	public static readonly DensePool<Structure> AllServerTickStructures = new DensePool<Structure>("AllServerTickStructures", 8192);

	private readonly Dictionary<Grid3, GridAirState> _gridAirStates = new Dictionary<Grid3, GridAirState>();

	private readonly HashSet<Grid3> _gridVoxelCanAirPass = new HashSet<Grid3>();

	public NativeHashMap<int3, byte> GridVoxelCanAirPassNative;

	private List<SmallCell> _occupiedcells = new List<SmallCell>(27);

	private const int SMALL_GRID_BUF_SIZE = 64;

	private static readonly HashSet<Structure> EmptyStructureHashset = new HashSet<Structure>(0);

	public const int GRID_NEIGHBOUR_BUFFER_SIZE = 32;

	public const int CARDINAL_NEIGHBOUR_COUNT = 6;

	public bool IsWorld { get; private set; }

	public Vector3 OffsetPosition => Vector3.zero;

	public void AlertGridWatchers(WorldGrid worldGrid, DynamicThing dynamicThing, GridEvent.GridEventType gridEventType)
	{
		if (GameManager.GameState == GameState.None)
		{
			return;
		}
		List<Structure> gridWatchers = GetGridWatchers(worldGrid);
		if (gridWatchers == null)
		{
			return;
		}
		GridEvent gridEvent = new GridEvent(worldGrid, dynamicThing, gridEventType);
		foreach (Structure item in gridWatchers)
		{
			item.ScheduleGridEventFromThread(gridEvent).Forget();
		}
	}

	public List<Structure> GetGridWatchers(WorldGrid worldGrid)
	{
		GridWatchers.TryGetValue(worldGrid, out var value);
		return value;
	}

	public void SetWatchGrid(WorldGrid worldGrid, Structure structure)
	{
		List<Structure> gridWatchers = GetGridWatchers(worldGrid);
		if (gridWatchers == null)
		{
			gridWatchers = new List<Structure> { structure };
			GridWatchers.Add(worldGrid, gridWatchers);
		}
		else
		{
			gridWatchers.Add(structure);
		}
	}

	public void RemoveWatchGrid(WorldGrid worldGrid, Structure structure)
	{
		GetGridWatchers(worldGrid)?.Remove(structure);
	}

	public static void ReleaseWorldController()
	{
		if (World != null)
		{
			World.ClearAll();
			if (World.AtmosphericsController != null)
			{
				World.AtmosphericsController.Clear();
			}
			World.RoomController = null;
			World.AtmosphericsController = null;
			AtmosphericsController.World = null;
			World.Dispose();
			World = null;
		}
	}

	public static void InitializeWorldController()
	{
		ReleaseWorldController();
		if (World == null)
		{
			World = new GridController
			{
				IsWorld = true
			};
			RoomController.World = World.RoomController;
			AtmosphericsController.World = World.AtmosphericsController;
			World.GridVoxelCanAirPassNative = new NativeHashMap<int3, byte>(100, Allocator.Persistent);
		}
	}

	public void ClearAll()
	{
		try
		{
			Thing.ClearAll();
			Item.ClearEvents();
			Cell.AllCells.Clear();
			SmallGridCells.Clear();
			GridCells.Clear();
			_gridVoxelCanAirPass.Clear();
			AllStructuresPool.Clear();
			AllServerTickStructures.Clear();
			Dispose();
		}
		catch (Exception ex)
		{
			Debug.LogError(ex.Message + " // " + ex.StackTrace);
		}
	}

	public GridController()
	{
		IsWorld = false;
		RoomController = new RoomController(this);
		AtmosphericsController = new AtmosphericsController(this);
	}

	public static GridController GetController(Vector3 worldPosition)
	{
		return World;
	}

	public Grid3 WorldToLocal(Vector3 worldPosition)
	{
		return worldPosition.ToGridPosition();
	}

	public static Grid3 WorldToLargeGridCenter(Vector3 worldPosition)
	{
		float num = 2f;
		float num2 = 1f / num;
		int num3 = 20;
		int num4 = num3 / 2;
		int num5 = Mathf.FloorToInt(worldPosition.x * num2) * num3;
		int num6 = Mathf.FloorToInt(worldPosition.y * num2) * num3;
		int num7 = Mathf.FloorToInt(worldPosition.z * num2) * num3;
		int nx = num5 + num4;
		int ny = num6 + num4;
		int nz = num7 + num4;
		return new Grid3(nx, ny, nz);
	}

	public Grid3 WorldToLocalGrid(Vector3 worldPosition, float gridSize = 2f, float gridOffset = 0f)
	{
		return worldPosition.ToGrid(gridSize, gridOffset);
	}

	public Vector3 LocalToWorld(Grid3 localGrid)
	{
		return localGrid.ToVector3();
	}

	public Vector3 ClampWorld(Vector3 worldPosition, float gridSize = 2f, float gridOffset = 0f)
	{
		return worldPosition.GridCenter(gridSize, gridOffset);
	}

	public Vector3 ClampWorld(Vector3 worldPosition, Vector3 gridSize3d, Vector3 gridOffset3d)
	{
		return worldPosition.GridCenter(gridSize3d, gridOffset3d);
	}

	private void AddFaceReference(Structure structure)
	{
		Grid3[] blockingGrids = structure.BlockingGrids;
		foreach (Grid3 key in blockingGrids)
		{
			if (!FaceLookup.ContainsKey(key))
			{
				FaceLookup.Add(key, new HashSet<Structure> { structure });
			}
			else
			{
				FaceLookup[key].Add(structure);
			}
		}
	}

	private void RemoveFaceReference(Structure structure)
	{
		Grid3[] blockingGrids = structure.BlockingGrids;
		foreach (Grid3 key in blockingGrids)
		{
			if (!FaceLookup.ContainsKey(key))
			{
				break;
			}
			FaceLookup[key].Remove(structure);
			if (FaceLookup[key].Count == 0)
			{
				FaceLookup.Remove(key);
			}
		}
	}

	public void Register(Structure structure)
	{
		structure.ThingTransformPosition = LocalToWorld(structure.RegisteredLocalGrid);
		structure.RegisteredPosition = structure.ThingTransformPosition;
		structure.RegisteredRotation = structure.ThingTransformRotation;
		SmallGrid smallGrid = structure as SmallGrid;
		if ((bool)smallGrid)
		{
			AddSmallGridStructure(smallGrid);
			if (!smallGrid.DualRegister)
			{
				return;
			}
		}
		AddGridStructure(structure);
		AddFaceReference(structure);
		UpdateAirState(structure);
	}

	public void Deregister(Structure structure)
	{
		if (GameManager.GameState == GameState.None)
		{
			return;
		}
		SmallGrid smallGrid = structure as SmallGrid;
		if ((bool)smallGrid)
		{
			RemoveSmallGridStructure(smallGrid);
			if (!smallGrid.DualRegister)
			{
				return;
			}
		}
		RemoveGridStructure(structure);
		RemoveFaceReference(structure);
		UpdateAirState(structure);
	}

	private void AddGridStructure(Structure structure)
	{
		Cell cell = null;
		if (structure.PlacementType == PlacementSnap.Grid)
		{
			Span<Grid3> span = stackalloc Grid3[structure.GridBounds._grids.Length];
			structure.GridBounds.GetLocalGrids(structure.ThingTransformPosition, structure.ThingTransformRotation, span);
			Span<Grid3> span2 = span;
			for (int i = 0; i < span2.Length; i++)
			{
				Grid3 grid = span2[i];
				cell = GetCell(grid);
				if (cell == null)
				{
					cell = new Cell(grid, this);
					cell.Add(structure, grid);
					GridCells.TryAdd(grid, cell);
					Cell.AllCells.TryAdd(cell, 0);
				}
				else if (!cell.Add(structure, grid))
				{
					return;
				}
			}
		}
		else
		{
			Grid3[] blockingGrids = structure.BlockingGrids;
			for (int i = 0; i < blockingGrids.Length; i++)
			{
				Grid3 relativePosition = blockingGrids[i];
				Grid3 value = new WorldGrid(relativePosition.GridCenter(structure.CenterPosition)).Value;
				cell = GetCell(value);
				if (cell == null)
				{
					cell = new Cell(value, this);
					cell.Add(structure, relativePosition);
					GridCells.TryAdd(value, cell);
					Cell.AllCells.TryAdd(cell, 0);
				}
				else if (!cell.Add(structure, relativePosition))
				{
					return;
				}
			}
		}
		AllStructuresPool.Add(structure);
		if (structure.IsUpdateOnServerTick)
		{
			AllServerTickStructures.Add(structure);
		}
		structure.OnRegistered(cell);
	}

	private void RemoveGridStructure(Structure structure)
	{
		if (!structure)
		{
			return;
		}
		AllStructuresPool.Remove(structure);
		Cell value;
		byte value2;
		if (structure.PlacementType == PlacementSnap.Grid)
		{
			Span<Grid3> span = stackalloc Grid3[structure.GridBounds._grids.Length];
			structure.GridBounds.GetLocalGrids(structure.RegisteredPosition, structure.RegisteredRotation, span);
			Span<Grid3> span2 = span;
			for (int i = 0; i < span2.Length; i++)
			{
				Grid3 grid = span2[i];
				Cell cell = GetCell(grid);
				if (cell == null)
				{
					continue;
				}
				if (cell.AllStructures.Contains(structure))
				{
					cell.Lookup.Clear(StructureElement.Center);
					cell.AllStructures.Remove(structure);
				}
				if (cell.IsValid())
				{
					continue;
				}
				Atmosphere atmosphereLocal = AtmosphericsController.GetAtmosphereLocal(cell.WorldGrid);
				if (atmosphereLocal != null)
				{
					atmosphereLocal.Cell = null;
				}
				GridCells.TryRemove(grid, out value);
				Cell.AllCells.TryRemove(cell, out value2);
				foreach (Cell neighborCell in cell.NeighborCells)
				{
					neighborCell.NeighborCells.Remove(cell);
				}
			}
		}
		else
		{
			Grid3[] blockingGrids = structure.BlockingGrids;
			for (int i = 0; i < blockingGrids.Length; i++)
			{
				Grid3 grid2 = blockingGrids[i];
				Grid3 localGrid = grid2.GridCenter(structure.CenterPosition);
				Cell cell2 = GetCell(localGrid);
				if (cell2 == null)
				{
					continue;
				}
				Grid3 grid3 = grid2 - cell2.Grid;
				if (cell2.AllStructures.Contains(structure))
				{
					cell2.Lookup.Clear(grid3);
					cell2.AllStructures.Remove(structure);
				}
				if (cell2.IsValid())
				{
					continue;
				}
				Atmosphere atmosphereLocal2 = AtmosphericsController.GetAtmosphereLocal(cell2.WorldGrid);
				if (atmosphereLocal2 != null)
				{
					atmosphereLocal2.Cell = null;
				}
				GridCells.TryRemove(cell2.Grid, out value);
				Cell.AllCells.TryRemove(cell2, out value2);
				foreach (Cell neighborCell2 in cell2.NeighborCells)
				{
					neighborCell2.NeighborCells.Remove(cell2);
				}
			}
		}
		structure.Cell = null;
		structure.OnDeregistered();
	}

	public void Attach(Structure structure)
	{
		SmallGrid smallGrid = structure as SmallGrid;
		if ((bool)smallGrid)
		{
			AttachSmallGridStructure(smallGrid);
			if (!smallGrid.DualRegister)
			{
				return;
			}
		}
		AttachStructureToGrid(structure);
		AddFaceReference(structure);
		UpdateAirState(structure);
	}

	public void Detatch(Structure structure)
	{
		if (GameManager.GameState == GameState.None)
		{
			return;
		}
		SmallGrid smallGrid = structure as SmallGrid;
		if ((bool)smallGrid)
		{
			DetatchSmallGridStructure(smallGrid);
			if (!smallGrid.DualRegister)
			{
				return;
			}
		}
		DetatchStructureFromGrid(structure);
		RemoveFaceReference(structure);
		UpdateAirState(structure);
	}

	public void AttachStructureToGrid(Structure structure)
	{
		structure.RegisteredLocalGrid = new Grid3(structure.ThingTransformPosition);
		structure.ThingTransformPosition = LocalToWorld(structure.RegisteredLocalGrid);
		structure.Position = structure.ThingTransformPosition;
		structure.RegisteredPosition = structure.ThingTransformPosition;
		structure.RegisteredRotation = structure.ThingTransformRotation;
		if (structure.PlacementType == PlacementSnap.Grid)
		{
			Grid3 relativeGrid = structure.GridController.WorldToLocal(structure.ThingTransformPosition);
			Span<Grid3> span = stackalloc Grid3[structure.GridBounds._grids.Length];
			structure.GridBounds.GetLocalGrids(structure.ThingTransformPosition, structure.ThingTransformRotation, span);
			Span<Grid3> span2 = span;
			for (int i = 0; i < span2.Length; i++)
			{
				Grid3 absoluteGrid = span2[i];
				if (!TryAddStructureToGridCell(structure, relativeGrid, absoluteGrid))
				{
					break;
				}
			}
			return;
		}
		Grid3[] blockingGrids = structure.BlockingGrids;
		for (int i = 0; i < blockingGrids.Length; i++)
		{
			Grid3 relativeGrid2 = blockingGrids[i];
			if (!TryAddStructureToGridCell(structure, relativeGrid2, new WorldGrid(relativeGrid2.GridCenter(structure.CenterPosition)).Value))
			{
				break;
			}
		}
	}

	private bool TryAddStructureToGridCell(Structure structure, Grid3 relativeGrid, Grid3 absoluteGrid)
	{
		Cell cell = GetCell(absoluteGrid);
		if (cell != null)
		{
			return cell.Add(structure, relativeGrid);
		}
		cell = new Cell(absoluteGrid, this);
		cell.Add(structure, relativeGrid);
		GridCells.TryAdd(absoluteGrid, cell);
		Cell.AllCells.TryAdd(cell, 0);
		return true;
	}

	public void DetatchStructureFromGrid(Structure structure)
	{
		if (!structure)
		{
			return;
		}
		if (structure.PlacementType == PlacementSnap.Grid)
		{
			Span<Grid3> span = stackalloc Grid3[structure.GridBounds._grids.Length];
			structure.GridBounds.GetLocalGrids(structure.RegisteredPosition, structure.RegisteredRotation, span);
			Span<Grid3> span2 = span;
			for (int i = 0; i < span2.Length; i++)
			{
				Grid3 localGrid = span2[i];
				RemoveStructureFromGridCell(structure, GetCell(localGrid));
			}
		}
		else
		{
			Grid3[] blockingGrids = structure.BlockingGrids;
			foreach (Grid3 grid in blockingGrids)
			{
				RemoveStructureFromGridCell(structure, GetCell(grid.GridCenter(structure.RegisteredPosition)));
			}
		}
		structure.Cell = null;
		structure.RemoveFromNeighbouringGridCells(structure.RegisteredPosition);
	}

	private void RemoveStructureFromGridCell(Structure structure, Cell cell)
	{
		if (cell == null)
		{
			return;
		}
		cell.Lookup.Remove(structure);
		cell.AllStructures.Remove(structure);
		if (cell.IsValid())
		{
			return;
		}
		Atmosphere atmosphereLocal = AtmosphericsController.GetAtmosphereLocal(cell.WorldGrid);
		if (atmosphereLocal != null)
		{
			atmosphereLocal.Cell = null;
		}
		GridCells.TryRemove(cell.Grid, out var _);
		Cell.AllCells.TryRemove(cell, out var _);
		foreach (Cell neighborCell in cell.NeighborCells)
		{
			neighborCell.NeighborCells.Remove(cell);
		}
	}

	private void AttachSmallGridStructure(SmallGrid smallGrid)
	{
		_occupiedcells.Clear();
		Grid3[] array = (Grid3[])smallGrid.GridBounds.GetLocalSmallGrid(smallGrid.ThingTransformPosition, smallGrid.ThingTransformRotation);
		foreach (Grid3 grid in array)
		{
			SmallCell smallCell = GetSmallCell(grid);
			if (smallCell == null)
			{
				smallCell = new SmallCell(grid, this);
				smallCell.Add(smallGrid);
				SmallGridCells.Add(grid, smallCell);
			}
			else
			{
				smallCell.Add(smallGrid);
				_occupiedcells.Add(smallCell);
			}
			smallGrid.SmallCell = smallCell;
		}
		if (_occupiedcells.Count < 6)
		{
			return;
		}
		foreach (SmallCell occupiedcell in _occupiedcells)
		{
			if (!(occupiedcell.Device == null) && !occupiedcell.Device.IsDoor)
			{
				Cell cell = GetCell(occupiedcell.Device.LocalGrid);
				if (cell == null || !cell.Stairs)
				{
					GridPathfinder.DeviceBlockedList.Add(occupiedcell.Device.LocalGrid);
				}
			}
		}
	}

	private void DetatchSmallGridStructure(SmallGrid smallGrid)
	{
		Grid3[] array = (Grid3[])smallGrid.GridBounds.GetLocalSmallGrid(smallGrid.RegisteredPosition, smallGrid.RegisteredRotation);
		foreach (Grid3 grid in array)
		{
			SmallCell smallCell = GetSmallCell(grid);
			if (smallCell != null)
			{
				GridPathfinder.DeviceBlockedList.Remove(grid);
				if (smallCell.Device != null)
				{
					GridPathfinder.DeviceBlockedList.Remove(smallCell.Device.LocalGrid);
				}
				smallCell.RemoveCellObjectReferences(smallGrid);
				if (!smallCell.IsValid())
				{
					SmallGridCells.Remove(grid);
				}
			}
		}
	}

	public void AddSmallCellOwner(Grid3 grid, ISmallGridOwner owner)
	{
		SmallCell smallCell = GetSmallCell(grid);
		if (smallCell != null)
		{
			smallCell.Owner = owner;
			return;
		}
		smallCell = new SmallCell(grid, this)
		{
			Owner = owner
		};
		SmallGridCells.Add(grid, smallCell);
	}

	public void RemoveSmallCellOwner(Grid3 grid, ISmallGridOwner owner)
	{
		SmallCell smallCell = GetSmallCell(grid);
		if (smallCell != null && smallCell.Owner == owner)
		{
			smallCell.Owner = null;
			if (!smallCell.IsValid())
			{
				SmallGridCells.Remove(grid);
			}
		}
	}

	private void AddSmallGridStructure(SmallGrid smallGridObject)
	{
		List<SmallCell> list = new List<SmallCell>();
		Grid3[] array = (Grid3[])smallGridObject.GridBounds.GetLocalSmallGrid(smallGridObject.RegisteredPosition, smallGridObject.RegisteredRotation);
		foreach (Grid3 grid in array)
		{
			SmallCell smallCell = GetSmallCell(grid);
			if (smallCell == null)
			{
				smallCell = new SmallCell(grid, this);
				smallCell.Add(smallGridObject);
				SmallGridCells.Add(grid, smallCell);
			}
			else
			{
				smallCell.Add(smallGridObject);
			}
			smallGridObject.SmallCell = smallCell;
			list.Add(smallCell);
		}
		AllStructuresPool.Add(smallGridObject);
		if (smallGridObject.IsUpdateOnServerTick)
		{
			AllServerTickStructures.Add(smallGridObject);
		}
		foreach (Connection openEnd in smallGridObject.OpenEnds)
		{
			openEnd.CacheTransformUp();
		}
		smallGridObject.OnRegistered(null);
		if (list.Count >= 6)
		{
			foreach (SmallCell item in list)
			{
				if (!(item.Device == null) && !item.Device.IsDoor)
				{
					Cell cell = GetCell(item.Device.LocalGrid);
					if (cell == null || !cell.Stairs)
					{
						GridPathfinder.DeviceBlockedList.Add(item.Device.LocalGrid);
					}
				}
			}
		}
		foreach (SmallCell item2 in list)
		{
			if (item2.Pipe != null)
			{
				item2.Pipe.OnGridPlaced(smallGridObject);
			}
			if (item2.Cable != null)
			{
				item2.Cable.OnGridPlaced(smallGridObject);
			}
			if (item2.Device != null)
			{
				item2.Device.OnGridPlaced(smallGridObject);
			}
			if (item2.Chute != null)
			{
				item2.Chute.OnGridPlaced(smallGridObject);
			}
			if (item2.Other != null)
			{
				item2.Other.OnGridPlaced(smallGridObject);
			}
			if (item2.Rail != null)
			{
				((SmallGrid)item2.Rail).OnGridPlaced(smallGridObject);
			}
			if (item2.Owner != null)
			{
				item2.Owner.OnGridPlaced(smallGridObject);
			}
		}
	}

	private void RemoveSmallGridStructure(SmallGrid smallGridObject)
	{
		if (!smallGridObject)
		{
			return;
		}
		AllStructuresPool.Remove(smallGridObject);
		Span<Grid3> span = stackalloc Grid3[64];
		int num = 0;
		Grid3 item = Grid3.zero;
		Grid3[] array = (Grid3[])smallGridObject.GridBounds.GetLocalSmallGrid(smallGridObject.RegisteredPosition, smallGridObject.RegisteredRotation);
		foreach (Grid3 grid in array)
		{
			SmallCell smallCell = GetSmallCell(grid);
			if (smallCell != null)
			{
				if (smallGridObject == smallCell.Device)
				{
					item = smallCell.Device.LocalGrid;
					smallCell.Device.SmallCell = null;
					smallCell.Device = null;
				}
				if (smallGridObject == smallCell.Chute)
				{
					smallCell.Chute.SmallCell = null;
					smallCell.Chute = null;
				}
				if (smallGridObject == smallCell.Pipe)
				{
					smallCell.Pipe.SmallCell = null;
					smallCell.Pipe = null;
				}
				if (smallGridObject == smallCell.Cable)
				{
					smallCell.Cable.SmallCell = null;
					smallCell.Cable = null;
				}
				if (smallGridObject == (SmallGrid)smallCell.Rail)
				{
					smallCell.Rail.SmallCell = null;
					smallCell.Rail = null;
				}
				if (smallGridObject == smallCell.Other)
				{
					smallCell.Other.SmallCell = null;
					smallCell.Other = null;
				}
				if (smallCell.Owner != null)
				{
					smallCell.Owner.OnGridRemoved(smallGridObject);
				}
				if (!smallCell.IsValid())
				{
					SmallGridCells.Remove(grid);
				}
			}
		}
		smallGridObject.OnDeregistered();
		array = (Grid3[])smallGridObject.GridBounds.GetLocalSmallGrid(smallGridObject.RegisteredPosition, smallGridObject.RegisteredRotation);
		foreach (Grid3 grid2 in array)
		{
			SmallCell smallCell2 = GetSmallCell(grid2);
			if (smallCell2 != null && smallCell2.Device != null)
			{
				span[num++] = grid2;
			}
		}
		if (num < 6 && (num != 0 || !GridPathfinder.DeviceBlockedList.Remove(item)))
		{
			Span<Grid3> span2 = span;
			Span<Grid3> span3 = span2.Slice(0, num);
			for (int i = 0; i < span3.Length; i++)
			{
				Grid3 item2 = span3[i];
				GridPathfinder.DeviceBlockedList.Remove(item2);
			}
		}
	}

	private bool CanAirPass(Grid3 blockingGrid)
	{
		Structure structure = Get<Structure>(new WorldGrid(blockingGrid));
		if (structure != null && !structure.CanAirPass)
		{
			return false;
		}
		foreach (Structure faceStructure in GetFaceStructures(blockingGrid))
		{
			if (!faceStructure.CanAirPass)
			{
				return false;
			}
		}
		return true;
	}

	public void UpdateAirState(Structure structure)
	{
		if (!IsWorld)
		{
			return;
		}
		GridAirState value = new GridAirState
		{
			CanAirPass = true
		};
		if (structure.BlockingGrids == null)
		{
			if (!CanAirPass(new Grid3(structure.ThingTransformPosition)))
			{
				value.CanAirPass = false;
			}
		}
		else
		{
			Grid3[] blockingGrids = structure.BlockingGrids;
			foreach (Grid3 blockingGrid in blockingGrids)
			{
				if (!CanAirPass(blockingGrid))
				{
					value.CanAirPass = false;
					break;
				}
			}
		}
		if (value.CanAirPass)
		{
			_gridAirStates.Remove(structure.OriginInWorldGridSpace);
		}
		else
		{
			_gridAirStates[structure.OriginInWorldGridSpace] = value;
		}
	}

	public void UpdateVoxelAirState(Grid3 localGrid, bool canAirPass)
	{
		if (IsWorld)
		{
			if (canAirPass)
			{
				RemoveVoxelAirState(localGrid);
			}
			else if (_gridVoxelCanAirPass.Add(localGrid))
			{
				GridVoxelCanAirPassNative.TryAdd(localGrid, 0);
			}
		}
	}

	private void RemoveVoxelAirState(Grid3 grid)
	{
		if (_gridVoxelCanAirPass.Remove(grid))
		{
			GridVoxelCanAirPassNative.Remove(grid);
		}
	}

	public Vector3 GridCenterFromWorld(Vector3 worldPosition, float gridSize = 2f, float gridOffset = 0f)
	{
		return (worldPosition - OffsetPosition).GridCenter(gridSize, gridOffset);
	}

	public Cell GetCell(Grid3 localGrid)
	{
		localGrid = new WorldGrid(localGrid).Value;
		GridCells.TryGetValue(localGrid, out var value);
		return value;
	}

	public Cell GetCell(WorldGrid localGrid)
	{
		return GetCell(localGrid.Value);
	}

	public Cell GetCell(Vector3 worldPosition)
	{
		Grid3 localGrid = WorldToLocalGrid(worldPosition, SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
		return GetCell(localGrid);
	}

	public SmallCell GetSmallCell(Vector3 worldPosition)
	{
		Grid3 localGrid = WorldToLocalGrid(worldPosition, SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
		return GetSmallCell(localGrid);
	}

	public SmallCell GetSmallCell(Grid3 localGrid)
	{
		SmallGridCells.TryGetValue(localGrid, out var value);
		return value;
	}

	public Structure Get<T>(Vector3 worldPosition, StructureElement element) where T : Structure
	{
		return Get<T>(WorldToLocalGrid(worldPosition), element);
	}

	public T Get<T>(Grid3 worldGrid, StructureElement element) where T : Structure
	{
		return GetCell(worldGrid)?.Lookup[element] as T;
	}

	public T Get<T>(WorldGrid worldGrid) where T : Structure
	{
		return GetCell(worldGrid)?.Lookup[StructureElement.Center] as T;
	}

	public Structure GetFaceStructure(Vector3 worldPosition, Vector3 direction)
	{
		return GetFaceStructure(WorldToLocalGrid(worldPosition), direction);
	}

	public Structure GetFaceStructure(Grid3 cellGrid, Vector3 direction)
	{
		return GetCell(cellGrid)?.Lookup[direction];
	}

	public bool CanContainAtmos(WorldGrid worldGrid, bool allowCrewModules = true)
	{
		return CanContainAtmos(worldGrid.Value, allowCrewModules);
	}

	public bool CanContainAtmos(Grid3 localGrid, bool allowCrewModules = true)
	{
		return GetCell(localGrid)?.IsOpenAir(allowCrewModules) ?? true;
	}

	public bool IsCellFloor(Grid3 localGrid)
	{
		Cell cell = GetCell(localGrid);
		if (cell == null)
		{
			return false;
		}
		return !cell.Lookup.IsEmpty(StructureElement.Down);
	}

	public bool IsCellCeiling(Grid3 localGrid)
	{
		Cell cell = GetCell(localGrid);
		if (cell == null)
		{
			return false;
		}
		return !cell.Lookup.IsEmpty(StructureElement.Up);
	}

	public bool IsCellBlocked(Grid3 localGrid)
	{
		Cell cell = GetCell(localGrid);
		if (cell == null)
		{
			return false;
		}
		StructuralArray.Enumerator enumerator = cell.Lookup.GetEnumerator();
		while (enumerator.MoveNext())
		{
			Structure current = enumerator.Current;
			Wall wall = current as Wall;
			Structure structure = current;
			Door door = current as Door;
			if (door != null && door.CanAirPass)
			{
				return false;
			}
			if (structure != null && !wall)
			{
				return true;
			}
		}
		return false;
	}

	public HashSet<Structure> GetFaceStructures(Grid3 localFacePosition)
	{
		FaceLookup.TryGetValue(localFacePosition, out var value);
		return value ?? EmptyStructureHashset;
	}

	public bool IsGridBlockedByStructure(Grid3 localGrid)
	{
		Cell cell = GetCell(localGrid);
		if (cell == null)
		{
			return false;
		}
		Structure structure = cell.Lookup[StructureElement.Center];
		if (structure == null)
		{
			return false;
		}
		return !structure.CanGravityPass;
	}

	public Pipe GetPipe(Grid3 localPosition)
	{
		SmallCell smallCell = GetSmallCell(localPosition);
		if (smallCell != null && (bool)smallCell.Pipe && smallCell.Pipe.PipeNetwork != null && smallCell.Pipe.PipeNetwork.IsNetworkValid())
		{
			return smallCell.Pipe;
		}
		return null;
	}

	public Cable GetCable(Grid3 localPosition)
	{
		SmallCell smallCell = GetSmallCell(localPosition);
		if (smallCell != null && (bool)smallCell.Cable && smallCell.Cable.CableNetwork != null && smallCell.Cable.CableNetwork.IsNetworkValid())
		{
			return smallCell.Cable;
		}
		return null;
	}

	public SmallGrid GetOther(Vector3 registeredPosition)
	{
		return GetSmallCell(registeredPosition)?.Other;
	}

	public void PopulateWorldGridFaces(Span<Vector3> buf, ref int count, Vector3 worldPosition)
	{
		Grid3 grid = WorldToLocalGrid(worldPosition);
		buf[0] = LocalToWorld(grid + Grid3.Face.North);
		buf[1] = LocalToWorld(grid + Grid3.Face.East);
		buf[2] = LocalToWorld(grid + Grid3.Face.South);
		buf[3] = LocalToWorld(grid + Grid3.Face.West);
		buf[4] = LocalToWorld(grid + Grid3.Face.Up);
		buf[5] = LocalToWorld(grid + Grid3.Face.Down);
		count = 6;
	}

	public void PopulateSmallGridNeighbours(Vector3 worldPosition, Span<Grid3> buf, ref int count, bool horizontalOnly = false)
	{
		Grid3 localGrid = WorldToLocalGrid(worldPosition, 0.5f, 0.25f);
		PopulateSmallGridNeighbours(localGrid, buf, ref count, horizontalOnly);
	}

	public void PopulateSmallGridNeighbours(Grid3 localGrid, Span<Grid3> buf, ref int count, bool horizontalOnly = false)
	{
		buf[0] = localGrid + RocketGrid.SmallNorth;
		buf[1] = localGrid + RocketGrid.SmallEast;
		buf[2] = localGrid + RocketGrid.SmallSouth;
		buf[3] = localGrid + RocketGrid.SmallWest;
		if (horizontalOnly)
		{
			count = 4;
			return;
		}
		buf[4] = localGrid + RocketGrid.SmallUp;
		buf[5] = localGrid + RocketGrid.SmallDown;
		count = 6;
	}

	private static bool GetFaceVoxels(Vector3 grid, Vector3 face, Span<Vector3> voxelBuf)
	{
		switch (RocketGrid.GetFaceDir(face, grid))
		{
		case Dir.North:
			voxelBuf[0] = RocketGrid.GridVoxel.NorthEastUp + grid;
			voxelBuf[1] = RocketGrid.GridVoxel.NorthEastDown + grid;
			voxelBuf[2] = RocketGrid.GridVoxel.NorthWestDown + grid;
			voxelBuf[3] = RocketGrid.GridVoxel.NorthWestUp + grid;
			return true;
		case Dir.South:
			voxelBuf[0] = RocketGrid.GridVoxel.SouthEastUp + grid;
			voxelBuf[1] = RocketGrid.GridVoxel.SouthEastDown + grid;
			voxelBuf[2] = RocketGrid.GridVoxel.SouthWestDown + grid;
			voxelBuf[3] = RocketGrid.GridVoxel.SouthWestUp + grid;
			return true;
		case Dir.West:
			voxelBuf[0] = RocketGrid.GridVoxel.NorthWestDown + grid;
			voxelBuf[1] = RocketGrid.GridVoxel.NorthWestUp + grid;
			voxelBuf[2] = RocketGrid.GridVoxel.SouthWestDown + grid;
			voxelBuf[3] = RocketGrid.GridVoxel.SouthWestUp + grid;
			return true;
		case Dir.East:
			voxelBuf[0] = RocketGrid.GridVoxel.SouthEastUp + grid;
			voxelBuf[1] = RocketGrid.GridVoxel.SouthEastDown + grid;
			voxelBuf[2] = RocketGrid.GridVoxel.NorthEastUp + grid;
			voxelBuf[3] = RocketGrid.GridVoxel.NorthEastDown + grid;
			return true;
		case Dir.Up:
			voxelBuf[0] = RocketGrid.GridVoxel.SouthEastUp + grid;
			voxelBuf[1] = RocketGrid.GridVoxel.SouthWestUp + grid;
			voxelBuf[2] = RocketGrid.GridVoxel.NorthEastUp + grid;
			voxelBuf[3] = RocketGrid.GridVoxel.NorthWestUp + grid;
			return true;
		case Dir.Down:
			voxelBuf[0] = RocketGrid.GridVoxel.SouthEastDown + grid;
			voxelBuf[1] = RocketGrid.GridVoxel.SouthWestDown + grid;
			voxelBuf[2] = RocketGrid.GridVoxel.NorthEastDown + grid;
			voxelBuf[3] = RocketGrid.GridVoxel.NorthWestDown + grid;
			return true;
		default:
			return false;
		}
	}

	public static Vector3[] GetGridVoxels(Vector3 grid)
	{
		return new Vector3[8]
		{
			RocketGrid.GridVoxel.NorthEastUp + grid,
			RocketGrid.GridVoxel.NorthEastDown + grid,
			RocketGrid.GridVoxel.NorthWestDown + grid,
			RocketGrid.GridVoxel.NorthWestUp + grid,
			RocketGrid.GridVoxel.SouthEastUp + grid,
			RocketGrid.GridVoxel.SouthEastDown + grid,
			RocketGrid.GridVoxel.SouthWestDown + grid,
			RocketGrid.GridVoxel.SouthWestUp + grid
		};
	}

	public static bool LargeGridIsFullOfVoxels(Grid3 grid)
	{
		Vector3[] gridVoxels = GetGridVoxels(grid.ToVector3());
		for (int i = 0; i < gridVoxels.Length; i++)
		{
			if (VoxelTerrain.GetDensityAtSize(gridVoxels[i].FloorToInt(), 1) < 0.49803922f)
			{
				return false;
			}
		}
		return true;
	}

	public bool IsVoxelFaceOpen(Grid3 gridA, Grid3 gridB, Grid3 face, Span<Vector3> voxelBuf)
	{
		Vector3 grid = gridA.ToVector3();
		Vector3 grid2 = gridB.ToVector3();
		Vector3 face2 = face.ToVector3();
		if (IsVoxelFaceOpen(grid, face2, voxelBuf))
		{
			return IsVoxelFaceOpen(grid2, face2, voxelBuf);
		}
		return false;
	}

	public bool IsVoxelFaceOpen(Vector3 grid, Vector3 face, Span<Vector3> voxelBuf)
	{
		if (!GetFaceVoxels(grid, face, voxelBuf))
		{
			return true;
		}
		for (int i = 0; i < 4; i++)
		{
			if (VoxelTerrain.GetDensityAtSize(voxelBuf[i].FloorToInt(), 1) < 0.49803922f)
			{
				return true;
			}
		}
		return false;
	}

	public void PopulateGridNeighbours(Span<Grid3> buf, ref int count, Vector3 worldPosition, bool horizontalOnly = false)
	{
		PopulateGridNeighbours(buf, ref count, WorldToLocalGrid(worldPosition), horizontalOnly);
	}

	public static void PopulateGridNeighbours(Span<Grid3> buf, ref int count, Grid3 localGrid, bool horizontalOnly = false, bool includeCorners = false)
	{
		buf[count++] = localGrid + Grid3.North;
		buf[count++] = localGrid + Grid3.East;
		buf[count++] = localGrid + Grid3.South;
		buf[count++] = localGrid + Grid3.West;
		if (!horizontalOnly)
		{
			buf[count++] = localGrid + Grid3.Up;
			buf[count++] = localGrid + Grid3.Down;
		}
		if (includeCorners)
		{
			buf[count++] = localGrid + Grid3.North + Grid3.Up;
			buf[count++] = localGrid + Grid3.North + Grid3.Down;
			buf[count++] = localGrid + Grid3.South + Grid3.Up;
			buf[count++] = localGrid + Grid3.South + Grid3.Down;
			buf[count++] = localGrid + Grid3.East + Grid3.Up;
			buf[count++] = localGrid + Grid3.East + Grid3.Down;
			buf[count++] = localGrid + Grid3.West + Grid3.Up;
			buf[count++] = localGrid + Grid3.West + Grid3.Down;
			buf[count++] = localGrid + Grid3.North + Grid3.East + Grid3.Up;
			buf[count++] = localGrid + Grid3.North + Grid3.East + Grid3.Down;
			buf[count++] = localGrid + Grid3.North + Grid3.West + Grid3.Up;
			buf[count++] = localGrid + Grid3.North + Grid3.West + Grid3.Down;
			buf[count++] = localGrid + Grid3.South + Grid3.East + Grid3.Up;
			buf[count++] = localGrid + Grid3.South + Grid3.East + Grid3.Down;
			buf[count++] = localGrid + Grid3.South + Grid3.West + Grid3.Up;
			buf[count++] = localGrid + Grid3.South + Grid3.West + Grid3.Down;
			buf[count++] = localGrid + Grid3.North + Grid3.East;
			buf[count++] = localGrid + Grid3.North + Grid3.West;
			buf[count++] = localGrid + Grid3.South + Grid3.East;
			buf[count++] = localGrid + Grid3.South + Grid3.West;
		}
	}

	public void GetNeighborCells(Vector3 worldPosition, Span<Grid3> buf, ref int count, bool includeCorners = false)
	{
		GetNeighborCells(WorldToLocalGrid(worldPosition), buf, ref count, includeCorners);
	}

	public void GetNeighborCells(Grid3 localGrid, Span<Grid3> buf, ref int count, bool includeCorners = false)
	{
		PopulateGridNeighbours(buf, ref count, localGrid, horizontalOnly: false, includeCorners);
	}

	public bool GetOpenNeighbors(Grid3 localGrid, Span<Grid3> result, ref int count)
	{
		count = 0;
		Cell cell = GetCell(localGrid);
		Vector3 vector = localGrid.ToVector3();
		Span<Vector3> voxelBuf = stackalloc Vector3[4];
		Span<Grid3> obj = stackalloc Grid3[32];
		int count2 = 0;
		PopulateGridNeighbours(obj, ref count2, localGrid);
		Span<Grid3> span = obj;
		Span<Grid3> span2 = span.Slice(0, count2);
		for (int i = 0; i < span2.Length; i++)
		{
			Grid3 grid = span2[i];
			Structure structure = Get<Structure>(grid, StructureElement.Center);
			if (!structure || structure.CanGravityPass)
			{
				result[count++] = grid;
			}
		}
		int index = count;
		while (index-- > 0)
		{
			Grid3 localGrid2 = result[index];
			Vector3 vector2 = vector.Middle(localGrid2.ToVector3()).ToGridPosition().ToVector3();
			Cell cell2 = GetCell(localGrid2);
			if (!IsVoxelFaceOpen(vector, vector2, voxelBuf) || !IsVoxelFaceOpen(localGrid2.ToVector3(), vector2, voxelBuf))
			{
				result[index] = result[--count];
			}
			else if (cell != null && !cell.IsOpen(vector2))
			{
				result[index] = result[--count];
			}
			else if (cell2 != null && !cell2.IsOpen(vector2))
			{
				result[index] = result[--count];
			}
		}
		return true;
	}

	public bool IsBlockedGrid(Vector3 registeredPosition)
	{
		Structure structure = Get<Structure>(registeredPosition, StructureElement.Center);
		if ((bool)structure && structure.StructureCollisionType == CollisionType.BlockGrid)
		{
			return !structure.CanGravityPass;
		}
		return false;
	}

	public bool IsBlockedGrid(WorldGrid worldGrid)
	{
		Structure structure = Get<Structure>(worldGrid);
		if ((bool)structure && structure.StructureCollisionType == CollisionType.BlockGrid)
		{
			return !structure.CanGravityPass;
		}
		return false;
	}

	public bool IsBlockedAirGrid(Grid3 localGrid)
	{
		Structure structure = Get<Structure>(localGrid, StructureElement.Center);
		if ((bool)structure && structure.StructureCollisionType == CollisionType.BlockGrid)
		{
			return !structure.CanAirPass;
		}
		return false;
	}

	private void Dispose(bool disposing)
	{
		if (disposing && GridVoxelCanAirPassNative.IsCreated)
		{
			GridVoxelCanAirPassNative.Dispose();
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
