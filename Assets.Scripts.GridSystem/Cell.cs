using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.GridSystem;

[SerializeField]
public class Cell
{
	public static readonly ConcurrentDictionary<Cell, byte> AllCells = new ConcurrentDictionary<Cell, byte>();

	public GridController GridController;

	public readonly WorldGrid WorldGrid;

	public Vector3 Position;

	public List<Structure> AllStructures = new List<Structure>();

	public List<Cell> NeighborCells = new List<Cell>();

	public Stairs Stairs;

	public bool HasLight;

	public StructuralArray Lookup;

	public Grid3 Grid => WorldGrid.Value;

	public Vector3 WorldPosition => GridController.LocalToWorld(Grid);

	public Quaternion Rotation => Quaternion.identity;

	public bool IsBlocked
	{
		get
		{
			Structure structure = Lookup[StructureElement.Center];
			if (!structure)
			{
				return false;
			}
			return structure.StructureCollisionType == CollisionType.BlockGrid;
		}
	}

	public bool IsBlockedLight
	{
		get
		{
			Structure structure = Lookup[StructureElement.Center];
			if (!structure)
			{
				return false;
			}
			if (structure.StructureCollisionType == CollisionType.BlockGrid)
			{
				return !structure.CanLightPass;
			}
			return false;
		}
	}

	public Cell(Grid3 key, GridController controller)
	{
		GridController = controller;
		Position = key.ToVector3();
		WorldGrid = new WorldGrid(Position);
		Span<Grid3> span = stackalloc Grid3[32];
		int count = 0;
		GridController.GetNeighborCells(Grid, span, ref count, includeCorners: true);
		Span<Grid3> span2 = span;
		Span<Grid3> span3 = span2.Slice(0, count);
		for (int i = 0; i < span3.Length; i++)
		{
			Grid3 localGrid = span3[i];
			Cell cell = GridController.GetCell(localGrid);
			if (cell != null)
			{
				cell.NeighborCells.Add(this);
				NeighborCells.Add(cell);
			}
		}
	}

	public void HandleCollisions(Structure collidingStructure, ref HashSet<Structure> handledStructures)
	{
	}

	private bool AddStructural(Structure structure, Grid3 relativePosition)
	{
		Grid3 grid = relativePosition - Grid;
		if (structure is Fuselage)
		{
			grid = Grid3.zero;
		}
		if (!Lookup.IsValid(grid))
		{
			if (!AllStructures.Contains(structure))
			{
				AllStructures.Add(structure);
			}
			return true;
		}
		Structure structure2 = Lookup[grid];
		if (!structure2)
		{
			StructureElement structureElement = StructuralArray.Get(grid);
			if (structureElement == StructureElement.Invalid)
			{
				ConsoleWindow.PrintError($"Non-Fatal Error {structure.DisplayName} failed to register at {relativePosition}.", suppressStacktrace: true);
				if (GameManager.RunSimulation)
				{
					OnServer.Destroy(structure);
				}
				return false;
			}
			Lookup.Set(structureElement, structure);
			AllStructures.Add(structure);
			return true;
		}
		if (structure2.IsBeingDestroyed || NetworkManager.IsClient)
		{
			Lookup.Set(grid, structure);
			AllStructures.Remove(structure2);
			AllStructures.Add(structure);
			return true;
		}
		ConsoleWindow.PrintError($"Non-Fatal Error {structure.DisplayName} cannot be registered at {relativePosition}. Grid face may be open.", suppressStacktrace: true);
		if (GameManager.RunSimulation)
		{
			OnServer.Destroy(structure);
		}
		return false;
	}

	public static bool IsInCrewModule(WorldGrid worldGrid, out CrewModule crewModule)
	{
		Cell cell = GridController.World.GetCell(worldGrid);
		if (cell == null)
		{
			crewModule = null;
			return false;
		}
		return IsInCrewModule(cell, out crewModule);
	}

	public static bool IsInCrewModule(Cell cell, out CrewModule crewModule)
	{
		if (cell != null)
		{
			return cell.IsInCrewModule(out crewModule);
		}
		crewModule = null;
		return false;
	}

	public bool IsInCrewModule(out CrewModule crewModule)
	{
		if (Lookup[StructureElement.Center] is CrewModule crewModule2)
		{
			if (crewModule2.RocketNetwork?.Rocket != null)
			{
				RocketState rocketState = crewModule2.RocketNetwork.Rocket.RocketState;
				if (rocketState != RocketState.InSpace && rocketState != RocketState.OnLaunchMount && rocketState != RocketState.Launching && rocketState != RocketState.Landing)
				{
					goto IL_004f;
				}
			}
			crewModule = crewModule2;
			return true;
		}
		goto IL_004f;
		IL_004f:
		crewModule = null;
		return false;
	}

	public bool Add(Structure structure, Grid3 relativePosition)
	{
		switch (structure.StructureCollisionType)
		{
		case CollisionType.BlockCustom:
		{
			Stairs stairs = structure as Stairs;
			if ((bool)stairs)
			{
				Stairs = stairs;
			}
			AllStructures.Add(structure);
			break;
		}
		case CollisionType.BlockGrid:
		case CollisionType.BlockFace:
			if (!AddStructural(structure, relativePosition))
			{
				return false;
			}
			break;
		default:
			AllStructures.Add(structure);
			break;
		}
		return true;
	}

	public bool IsOpen(Vector3 position)
	{
		Grid3 element = position.ToGridPosition() - Grid;
		Structure structure = Lookup[element];
		if ((bool)structure && !structure.CanGravityPass)
		{
			return false;
		}
		return true;
	}

	public bool IsOpenAir(bool allowCrewModules = false)
	{
		Structure structure = Lookup[StructureElement.Center];
		if ((bool)structure && !structure.CanAirPass && (!(structure is CrewModule) || !allowCrewModules))
		{
			return false;
		}
		return AtmosphereHelper.GetWorldVolume(Grid.ToVector3()) > new VolumeLitres(1.0);
	}

	public bool IsOpenAir(Grid3 faceGrid, bool allowCrewModules = false)
	{
		Grid3 element = faceGrid - Grid;
		Structure structure = Lookup[element];
		if ((bool)structure && !structure.CanAirPass && (!(structure is CrewModule) || !allowCrewModules))
		{
			return false;
		}
		return AtmosphereHelper.GetWorldVolume(faceGrid.ToVector3()) > new VolumeLitres(1.0);
	}

	public bool IsOpenLight(Vector3 position)
	{
		Grid3 element = position.ToGridPosition() - Grid;
		Structure structure = Lookup[element];
		if ((bool)structure)
		{
			if ((bool)structure)
			{
				return structure.CanLightPass;
			}
			return false;
		}
		return true;
	}

	public bool IsWalkable(Grid3 worldGrid)
	{
		Grid3 element = worldGrid - Grid;
		Structure structure = Lookup[element];
		Door door = structure as Door;
		if ((bool)structure)
		{
			if ((bool)door)
			{
				return door.CanAirPass;
			}
			return false;
		}
		return true;
	}

	public bool IsValid()
	{
		return AllStructures.Count != 0;
	}

	public Structure GetFirst()
	{
		if (AllStructures.Count != 0)
		{
			return AllStructures[0];
		}
		return null;
	}
}
