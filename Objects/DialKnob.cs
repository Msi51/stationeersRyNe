using Assets.Scripts;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;

namespace Objects;

public class DialKnob : BlendedAnimComponent
{
	protected override bool ShouldPlaySound(KeyFrameData newState)
	{
		if (animatedTransform != null)
		{
			return !RocketMath.Approximately(newState.Rotation, animatedTransform.rotation.eulerAngles);
		}
		return false;
	}

	protected override void PlaySound()
	{
		if (!GameManager.IsBatchMode && !(animatedTransform == null) && !(parentThing == null))
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(Defines.Sounds.DialTurn, animatedTransform.position);
		}
	}
}
