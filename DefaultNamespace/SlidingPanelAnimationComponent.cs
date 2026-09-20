using Assets.Scripts.Util;
using UnityEngine;

namespace DefaultNamespace;

public class SlidingPanelAnimationComponent : AssignableBinaryAnimComponent
{
	protected override Vector3 SoundPosition => Transform.position - parentThing.Position;

	protected override void TriggerAudio()
	{
		PlaySound((InteractableState == 1) ? Defines.Sounds.ApcOpen : Defines.Sounds.ApcClose);
	}

	protected override void OnAnimationStart()
	{
		base.OnAnimationStart();
		if ((bool)parentThing)
		{
			parentThing.OnAnimationStart();
		}
	}

	protected override void OnAnimationCompleted()
	{
		base.OnAnimationCompleted();
		if ((bool)parentThing)
		{
			parentThing.OnAnimationStop();
		}
	}
}
