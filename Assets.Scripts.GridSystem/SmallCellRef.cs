using System;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Objects.RoboticArm;

namespace Assets.Scripts.GridSystem;

public readonly struct SmallCellRef
{
	public const int BUFFER_SIZE = 32;

	private readonly Grid3 _grid;

	private readonly SmallCellType _smallCellType;

	public SmallCellRef(SmallCell cell, SmallCellType type)
	{
		_grid = cell.SmallGrid;
		_smallCellType = type;
	}

	public SmallCellRef(Grid3 grid, SmallCellType type)
	{
		_grid = grid;
		_smallCellType = type;
	}

	public SmallCell GetCell()
	{
		return GridController.World.GetSmallCell(_grid);
	}

	public ISmallGrid Get()
	{
		SmallCell smallCell = GridController.World.GetSmallCell(_grid);
		if (smallCell == null)
		{
			return null;
		}
		return _smallCellType switch
		{
			SmallCellType.Pipe => smallCell.Pipe, 
			SmallCellType.Device => smallCell.Device, 
			SmallCellType.Cable => smallCell.Cable, 
			SmallCellType.Chute => smallCell.Chute, 
			SmallCellType.Rail => smallCell.Rail, 
			SmallCellType.Other => smallCell.Other, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	public bool TryGet<T>(out T found) where T : ISmallGrid
	{
		found = Get<T>();
		return found != null;
	}

	public T Get<T>() where T : ISmallGrid
	{
		SmallCell smallCell = GridController.World.GetSmallCell(_grid);
		if (smallCell == null)
		{
			return default(T);
		}
		switch (_smallCellType)
		{
		case SmallCellType.Pipe:
		{
			Pipe pipe = smallCell.Pipe;
			if (pipe is T)
			{
				return (T)(object)((pipe is T) ? pipe : null);
			}
			return default(T);
		}
		case SmallCellType.Device:
		{
			Device device = smallCell.Device;
			if (device is T)
			{
				return (T)(object)((device is T) ? device : null);
			}
			return default(T);
		}
		case SmallCellType.Cable:
		{
			Cable cable = smallCell.Cable;
			if (cable is T)
			{
				return (T)(object)((cable is T) ? cable : null);
			}
			return default(T);
		}
		case SmallCellType.Chute:
		{
			Chute chute = smallCell.Chute;
			if (chute is T)
			{
				return (T)(object)((chute is T) ? chute : null);
			}
			return default(T);
		}
		case SmallCellType.Rail:
		{
			IRoboticArmRail rail = smallCell.Rail;
			if (rail is T)
			{
				return (T)rail;
			}
			return default(T);
		}
		case SmallCellType.Other:
		{
			SmallGrid other = smallCell.Other;
			if (other is T)
			{
				return (T)(object)((other is T) ? other : null);
			}
			return default(T);
		}
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public static implicit operator SmallCell(SmallCellRef smallCellRef)
	{
		return smallCellRef.GetCell();
	}
}
