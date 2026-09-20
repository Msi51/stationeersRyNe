using Assets.Scripts.GridSystem;
using UnityEngine;

namespace Assets.Scripts.Objects;

public interface IWindowShutter
{
	Grid3[] NeighbourPositions { get; }

	Transform Transform { get; }

	Grid3 RegisteredGridPosition { get; }
}
