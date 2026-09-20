using UnityEngine;

namespace Assets.Scripts.Objects.Entities;

public interface IGenerateMinables
{
	Vector3Int MinablesGenerationRange { get; }

	Vector3 GeneratePosition { get; }

	Transform Transform { get; }

	Vector3 PreviousMinableRequestPosition { get; set; }

	bool ShouldGenerate { get; }
}
