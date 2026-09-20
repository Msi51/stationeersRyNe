using System.Collections.Generic;

namespace Assets.Scripts.Objects.Items;

public interface ISanitation
{
	static List<ISanitation> AllSanitationPrefabs;

	static ISanitation()
	{
		AllSanitationPrefabs = new List<ISanitation>();
	}
}
