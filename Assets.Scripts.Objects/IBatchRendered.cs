using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Objects;

public interface IBatchRendered
{
	Matrix4x4 GetBatchMatrix();

	Material GetMaterial();

	Mesh GetMesh();

	ShadowCastingMode GetShadowMode();
}
