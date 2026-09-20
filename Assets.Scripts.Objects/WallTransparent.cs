using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class WallTransparent : Wall
{
	private const float GRATING_SHADOW_DISTANCE = 60f;

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(60f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}
}
