using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts.Objects;

public abstract class SmallDevice : Device
{
	public new const float RENDER_DISTANCE = 25f;

	public new const float SHADOW_DISTANCE = 10f;

	public override int WreckageQuantity => 0;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(25f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(10f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}
}
