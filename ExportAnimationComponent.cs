using Assets.Scripts.Util;

public class ExportAnimationComponent : AssignableBinaryAnimComponent
{
	protected override void TriggerAudio()
	{
		if (InteractableState == 1)
		{
			PlaySound(Defines.Sounds.DeviceExportHash);
		}
	}

	protected override void OnAnimationStart()
	{
		base.OnAnimationStart();
	}

	protected override void OnAnimationCompleted()
	{
		base.OnAnimationCompleted();
	}
}
