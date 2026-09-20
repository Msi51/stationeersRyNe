using UnityEngine;

namespace Trading;

public interface INonThingOcclusion
{
	bool CanSetOcclusion();

	float GetRenderMaxDistanceSquared();

	Vector3 GetCachedTransformPosition();
}
