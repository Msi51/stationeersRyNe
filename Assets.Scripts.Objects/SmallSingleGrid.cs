using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class SmallSingleGrid : SmallGrid
{
	public const float RENDER_DISTANCE = 20f;

	public const float SHADOW_DISTANCE = 6f;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(20f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(6f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}
}
