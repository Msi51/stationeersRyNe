using Assets.Scripts.Util;

public class ContainerAnimComponent : AssignableBinaryAnimComponent
{
	protected override void OnAnimationStart()
	{
		parentThing.OnAnimationStart();
	}

	protected override void OnAnimationCompleted()
	{
		parentThing.OnAnimationStop();
	}

	protected override void TriggerAudio()
	{
		switch (InteractableState)
		{
		case 0:
			parentThing.PlayPooledAudioSound(Defines.Sounds.EggCartonClose, parentThing.SoundPosition.localPosition);
			break;
		case 1:
			parentThing.PlayPooledAudioSound(Defines.Sounds.EggCartonOpen, parentThing.SoundPosition.localPosition);
			break;
		}
	}
}
