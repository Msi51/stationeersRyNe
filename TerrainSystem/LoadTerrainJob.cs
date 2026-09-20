using System.IO;
using Unity.Collections;
using UnityEngine;

namespace TerrainSystem;

public struct LoadTerrainJob : IThreadable
{
	public int ThreadCost { get; }

	public string Path { get; }

	public ReadonlyVoxelOctree Target { get; }

	public bool CanThread()
	{
		return true;
	}

	public string DebugName()
	{
		return $"TerrainJob_{Target.Index}";
	}

	public LoadTerrainJob(string path, ReadonlyVoxelOctree targetOctree)
	{
		if (!File.Exists(path))
		{
			ThreadCost = 1;
			Target = targetOctree;
			Path = null;
		}
		else
		{
			FileInfo fileInfo = new FileInfo(path);
			ThreadCost = Mathf.Max(1, (int)(fileInfo.Length / 1000));
			Path = path;
			Target = targetOctree;
		}
	}

	public void DoWork(NativeArray<byte> workingArray)
	{
		Target.Deserialize(Path, workingArray);
	}
}
