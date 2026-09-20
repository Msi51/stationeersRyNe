using System;
using Assets.Scripts.GridSystem;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class GridBounds
{
	public Grid3[] _grids;

	public Grid3[] _gridsSmall;

	public Bounds BoundsSmall;

	public Bounds BoundsBig;

	public bool IsValid()
	{
		if (_grids != null)
		{
			return _gridsSmall != null;
		}
		return false;
	}

	public GridBounds()
	{
	}

	public GridBounds(Structure structure)
	{
		_grids = (Grid3[])structure.GetLocalGridBounds();
		_gridsSmall = (Grid3[])structure.GetLocalSmallGridBounds();
		BoundsSmall = structure.GetSmallGridBounds();
		BoundsBig = new Bounds(Vector3.zero, Vector3.one);
		Grid3[] grids = _grids;
		foreach (Grid3 grid in grids)
		{
			Vector3 vector = grid.ToVector3();
			BoundsBig.Encapsulate(vector + new Vector3(-1f, -1f, -1f));
			BoundsBig.Encapsulate(vector + new Vector3(1f, 1f, 1f));
		}
	}

	public void GetLocalGrids(Vector3 position, Quaternion rotation, Span<Grid3> localGrids)
	{
		for (int i = 0; i < _grids.Length; i++)
		{
			Grid3 grid = _grids[i];
			Vector3 worldPosition = rotation * grid.ToVector3() + position;
			Grid3 grid2 = GridController.World.WorldToLocalGrid(worldPosition);
			localGrids[i] = grid2;
		}
	}

	public object GetLocalSmallGrid(Vector3 position, Quaternion rotation)
	{
		Grid3[] array = new Grid3[_gridsSmall.Length];
		for (int i = 0; i < _gridsSmall.Length; i++)
		{
			Grid3 grid = _gridsSmall[i];
			Vector3 worldPosition = rotation * grid.ToVector3() + position;
			Grid3 grid2 = GridController.World.WorldToLocalGrid(worldPosition, 0.5f, 0.25f);
			array[i] = grid2;
			if (i > 12000)
			{
				break;
			}
		}
		return array;
	}
}
