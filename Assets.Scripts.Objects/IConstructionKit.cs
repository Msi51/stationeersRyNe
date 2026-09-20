using System.Collections.Generic;

namespace Assets.Scripts.Objects;

public interface IConstructionKit
{
	string GetPrefabName();

	string GetFallbackName();

	List<Thing> GetConstructedPrefabs();
}
