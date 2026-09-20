using System.Collections.Generic;

namespace ThingImport;

public static class DataResolver
{
	private static readonly List<DataResolutionTask> _dataResolutionTasks = new List<DataResolutionTask>();

	public static void AddResolutionTask(DataResolutionTask resolutionTask)
	{
		_dataResolutionTasks.Add(resolutionTask);
	}

	public static void ResolveAll()
	{
		foreach (DataResolutionTask dataResolutionTask in _dataResolutionTasks)
		{
			dataResolutionTask.Resolve();
		}
		_dataResolutionTasks.Clear();
	}

	public static void Clear()
	{
		_dataResolutionTasks.Clear();
	}
}
