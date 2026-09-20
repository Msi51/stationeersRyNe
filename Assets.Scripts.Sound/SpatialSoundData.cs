using System;
using UnityEngine;

namespace Assets.Scripts.Sound;

[Serializable]
public class SpatialSoundData
{
	public static AnimationCurve AttenuationCurve = new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f, 0f, 0.2f), new Keyframe(0.2f, 0.2f, -1f, -0.8f, 0.5f, 0.2f), new Keyframe(0.6f, 0.07f, -0.125f, -0.125f, 0.5f, 0.5f), new Keyframe(1f, 0f, 0f, 0f, 0.6f, 0.6f));

	public float DopplerLevel;

	public float Spread;

	public int VolumeRolloffType = 2;

	public float MinDistance = 1f;

	public float MaxDistance = 10f;

	public float ReverbZoneMix = 1f;

	public void Apply(AudioSource audioSource)
	{
		audioSource.dopplerLevel = DopplerLevel;
		audioSource.spread = Spread;
		audioSource.minDistance = MinDistance;
		audioSource.maxDistance = MaxDistance;
		audioSource.rolloffMode = (AudioRolloffMode)VolumeRolloffType;
		audioSource.reverbZoneMix = ReverbZoneMix;
		if (audioSource.rolloffMode == AudioRolloffMode.Custom)
		{
			audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AttenuationCurve);
		}
	}
}
