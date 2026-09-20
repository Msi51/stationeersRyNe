using Assets.Scripts.Util;

public class FuselageDoorAnimComponent : AssignableBinaryAnimComponent
{
	protected override void TriggerAudio()
	{
		switch (InteractableState)
		{
		case 0:
			parentThing.PlayPooledAudioSound(Defines.Sounds.ApcClose, parentThing.SoundPosition.localPosition);
			break;
		case 1:
			parentThing.PlayPooledAudioSound(Defines.Sounds.ApcOpen, parentThing.SoundPosition.localPosition);
			break;
		}
	}
}
