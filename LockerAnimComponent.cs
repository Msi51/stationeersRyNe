using Assets.Scripts.Util;
using UnityEngine;

public class LockerAnimComponent : OpenAnimationComponent
{
	protected override Vector3 SoundPosition => parentThing?.SoundPosition?.localPosition ?? Vector3.zero;

	protected override void TriggerAudio()
	{
		PlaySound((InteractableState == 1) ? Defines.Sounds.StorageLockerOpenHash : Defines.Sounds.StorageLockerCloseHash);
	}

	protected override void OnAnimationStart()
	{
		parentThing.OnAnimationStart();
	}

	protected override void OnAnimationCompleted()
	{
		parentThing.OnAnimationStop();
	}
}
