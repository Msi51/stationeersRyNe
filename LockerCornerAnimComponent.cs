using Assets.Scripts.Util;

public class LockerCornerAnimComponent : LockerAnimComponent
{
	protected override void TriggerAudio()
	{
		PlaySound((InteractableState == 1) ? Defines.Sounds.CornerLockerOpenHash : Defines.Sounds.CornerLockerCloseHash);
	}
}
