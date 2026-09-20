using Trading;
using UnityEngine;

namespace Objects.Rockets;

public interface ISpaceMapNodeOwner : IReferencable, IEvaluable
{
	SpaceMapNode SpaceMapNode { get; set; }

	Vector3 RocketTransformPosition { get; }

	bool IsOrbital { get; }
}
