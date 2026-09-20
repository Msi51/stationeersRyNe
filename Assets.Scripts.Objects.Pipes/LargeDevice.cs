using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class LargeDevice : Device
{
	public new const float RENDER_DISTANCE = 40f;

	public new const float SHADOW_DISTANCE = 20f;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(40f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(20f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}
}
