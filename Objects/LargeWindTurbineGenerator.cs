using Assets.Scripts;
using Assets.Scripts.Serialization;
using UnityEngine;

namespace Objects;

public class LargeWindTurbineGenerator : WindTurbineGenerator
{
	private const float AUDIO_DISTANCE = 30f;

	private const float AUDIO_DISTANCE_SQR = 900f;

	public override float MaxPowerOutputStorm => 20000f;

	public override float WeatherUtilisationMultiplier => 20f;

	public override float NoiseIntensity => 25f;

	public override float MAXPowerOutput => 1000f;

	public override float AudioDistanceSquared => 900f;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(20f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}
}
