using Assets.Scripts.Util;

public class ChuteBinAnimComponent : AssignableBinaryAnimComponent
{
	protected override void TriggerAudio()
	{
		PlaySound((InteractableState == 1) ? Defines.Sounds.ChuteBinOpenHash : Defines.Sounds.ChuteBinCloseHash);
	}
}
