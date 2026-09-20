using Assets.Scripts.Util;

public class LeverAnimationComponent : AssignableBinaryAnimComponent
{
	protected override void TriggerAudio()
	{
		PlaySound((InteractableState == 1) ? Defines.Sounds.LeverDown : Defines.Sounds.LeverUp);
	}
}
