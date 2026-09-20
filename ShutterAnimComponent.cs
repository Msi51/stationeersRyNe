using Assets.Scripts;
using Assets.Scripts.Util;

public class ShutterAnimComponent : AssignableBinaryAnimComponent
{
	protected override float AudibleSqrDistance => 100f;

	protected override void TriggerAudio()
	{
		base.TriggerAudio();
		PlaySound((InteractableState == 1) ? Defines.Sounds.ShutterOpen : Defines.Sounds.ShutterClose);
	}

	protected override void OnAnimationCompleted()
	{
		base.OnAnimationCompleted();
		if (!GameManager.IsBatchMode)
		{
			PlaySound((InteractableState == 1) ? Defines.Sounds.ShutterOpenStop : Defines.Sounds.ShutterCloseStop);
		}
	}
}
