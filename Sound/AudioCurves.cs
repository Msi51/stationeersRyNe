using UnityEngine;

namespace Sound;

public class AudioCurves : MonoBehaviour
{
	public static AudioCurves Instance;

	public AnimationCurve DynamicCanisterAirLoud;

	public AnimationCurve DynamicCanisterAirThinVol;

	public AnimationCurve DynamicCanisterAirThinPitch;

	public void Awake()
	{
		Instance = this;
	}
}
