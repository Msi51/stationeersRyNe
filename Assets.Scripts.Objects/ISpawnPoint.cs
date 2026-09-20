using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects;

public interface ISpawnPoint : IReferencable, IEvaluable
{
	Thing GetAsThing { get; }

	Transform GetSpawnPointTransform();
}
