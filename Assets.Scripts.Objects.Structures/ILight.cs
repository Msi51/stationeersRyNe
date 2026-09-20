using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public interface ILight
{
	Thing GetAsThing { get; }

	Light Light { get; }
}
