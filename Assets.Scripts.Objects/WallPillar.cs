using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class WallPillar : SmallGrid
{
	private const float SHADOW_DISTANCE = 5f;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(5f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}
}
