using Assets.Scripts.Util;

public class ApcOnOffAnimComponent : AssignableBinaryAnimComponent
{
	protected override void TriggerAudio()
	{
		switch (InteractableState)
		{
		case 0:
			parentThing.PlayPooledAudioSound(Defines.Sounds.ApcOff, parentThing.SoundPosition.localPosition);
			break;
		case 1:
			parentThing.PlayPooledAudioSound(Defines.Sounds.ApcOn, parentThing.SoundPosition.localPosition);
			break;
		}
	}
}
