using UnityEngine;

public class DoorAnimComponent : OpenAnimationComponent
{
	public virtual int OpenSound => 0;

	public virtual int CloseSound => 0;

	public virtual int OpenNoPowerSound => 0;

	public virtual int CloseNoPowerSound => 0;

	protected override Vector3 SoundPosition => parentThing?.SoundPosition?.localPosition ?? Vector3.zero;

	protected override void TriggerAudio()
	{
		if (parentThing.Powered)
		{
			PlaySound((InteractableState == 1) ? OpenSound : CloseSound);
		}
		else
		{
			PlaySound((InteractableState == 1) ? OpenNoPowerSound : CloseNoPowerSound);
		}
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
