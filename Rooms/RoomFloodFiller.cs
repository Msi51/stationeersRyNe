using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;

namespace Rooms;

public class RoomFloodFiller
{
	public Func<Grid3, bool> EvaluateNeighbour;

	public List<Grid3> ClosedList = new List<Grid3>();

	private Queue<Grid3> _openQueue = new Queue<Grid3>();

	private HashSet<Grid3> _openLookup = new HashSet<Grid3>();

	private HashSet<Grid3> _closedLookup = new HashSet<Grid3>();

	private const int MAX_ITERATIONS = 1200;

	public void Clear()
	{
		ClosedList.Clear();
		_openQueue.Clear();
		_openLookup.Clear();
		_closedLookup.Clear();
	}

	public FillResult Fill(Grid3 start)
	{
		int num = 0;
		Clear();
		_openQueue.Enqueue(start);
		_openLookup.Add(start);
		while (_openQueue.Count > 0 && num < 1200)
		{
			num++;
			Grid3 grid = _openQueue.Dequeue();
			_closedLookup.Add(grid);
			ClosedList.Add(grid);
			_openLookup.Remove(grid);
			Span<Grid3> span = stackalloc Grid3[32];
			int count = 0;
			GridController.World.GetOpenNeighbors(grid, span, ref count);
			Span<Grid3> span2 = span;
			Span<Grid3> span3 = span2.Slice(0, count);
			for (int i = 0; i < span3.Length; i++)
			{
				Grid3 grid2 = span3[i];
				if (!_openLookup.Contains(grid2) && !_closedLookup.Contains(grid2))
				{
					Func<Grid3, bool> evaluateNeighbour = EvaluateNeighbour;
					if (evaluateNeighbour != null && evaluateNeighbour(grid2))
					{
						return FillResult.EarlyExit;
					}
					_openQueue.Enqueue(grid2);
					_openLookup.Add(grid2);
				}
			}
		}
		if (num >= 1200)
		{
			return FillResult.IterationLimit;
		}
		return FillResult.Success;
	}
}
