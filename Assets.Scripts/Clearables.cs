using System.Collections.Generic;

namespace Assets.Scripts;

public static class Clearables
{
	private static readonly List<IClearable> _clearables = new List<IClearable>();

	public static void Register(IClearable syncList)
	{
		_clearables.Add(syncList);
	}

	public static void ClearAll()
	{
		foreach (IClearable clearable in _clearables)
		{
			clearable.Clear();
		}
	}
}
