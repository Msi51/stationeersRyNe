using System.Collections.Generic;
using Assets.Scripts.Objects;
using UnityEngine;
using UnityEngine.Rendering;

public interface IBatchable
{
	Mesh SharedMesh { get; }

	Thing GetParent { get; }

	ShadowCastingMode ShadowCastingMode { get; }

	Dictionary<int, float> FloatProperties { get; }

	Transform GetRendererTransform();

	Material GetMaterial();
}
