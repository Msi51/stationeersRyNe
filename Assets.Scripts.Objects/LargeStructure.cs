using Assets.Scripts.Serialization;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Objects;

public class LargeStructure : Structure
{
	public const float RENDER_DISTANCE = 100f;

	public const float SHADOW_DISTANCE = 60f;

	protected override float GetRenderMaxDistanceSquared()
	{
		return 10000f;
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(60f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public override ShadowCastingMode GetShadowCastingMode()
	{
		return ShadowCastingMode.On;
	}
}
