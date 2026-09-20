using System.Collections.Generic;
using Reagents;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public interface IResourceConsumer : IReferencable, IEvaluable
{
	List<Item> GetResourcesUsed();

	int GetPrefabHash();

	string GetPrefabName();

	Sprite GetThumbnail();

	bool CanProcess(Recipe recipe);

	bool CanProcess(Reagent reagentType);
}
