using System.Collections.Generic;

namespace Objects.Rockets;

public interface IRocketComponent
{
	static List<IRocketComponent> AllRocketPrefabs;

	static IRocketComponent()
	{
		AllRocketPrefabs = new List<IRocketComponent>();
	}
}
