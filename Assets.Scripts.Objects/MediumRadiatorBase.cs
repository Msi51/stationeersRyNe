using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts.Objects;

public abstract class MediumRadiatorBase : Radiator
{
	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(20f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}
}
