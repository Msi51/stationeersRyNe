using System;
using System.Collections.Generic;

namespace Assets.Scripts.Util;

public static class DensePools
{
	private static List<IDensePool> _allPools = new List<IDensePool>(32);

	public static void Register(IDensePool pool)
	{
		_allPools.Add(pool);
	}

	public static void PrintAllPools()
	{
		for (int i = 0; i < _allPools.Count; i++)
		{
			_allPools[i].DrawInList(ref i);
		}
	}

	public static void PrintAllNullsInPools()
	{
		for (int i = 0; i < _allPools.Count; i++)
		{
			_allPools[i].DrawNullCountInList(ref i);
		}
	}

	public static void PrintAllSummarys(string selection)
	{
		for (int i = 0; i < _allPools.Count; i++)
		{
			IDensePool densePool = _allPools[i];
			if (densePool.Name.Contains(selection, StringComparison.OrdinalIgnoreCase))
			{
				densePool.DrawSummary();
				return;
			}
		}
		ConsoleWindow.PrintError("error no pool found with name '" + selection + "'");
	}
}
