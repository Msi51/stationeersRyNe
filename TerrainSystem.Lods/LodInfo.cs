using System.Collections.Generic;

namespace TerrainSystem.Lods;

public struct LodInfo
{
	public List<LodLevel> Levels;

	public bool Expand;

	public LodInfo(int startSize, int radius)
	{
		Expand = false;
		Levels = new List<LodLevel>();
		Levels.Add(new LodLevel(startSize, radius));
	}

	public LodInfo(int startSize, int[] radiusList)
	{
		Expand = true;
		Levels = new List<LodLevel>();
		int num = startSize;
		foreach (int radius in radiusList)
		{
			Levels.Add(new LodLevel(num, radius));
			num *= 2;
		}
	}
}
