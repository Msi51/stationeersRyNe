using Assets.Scripts.Util;

public class LockerSmallAnimComponent : LockerAnimComponent
{
	protected override void TriggerAudio()
	{
		PlaySound((InteractableState == 1) ? Defines.Sounds.StorageLockerSmallOpenHash : Defines.Sounds.StorageLockerSmallCloseHash);
	}
}
