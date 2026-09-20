using Assets.Scripts.Util;

public class ModeDialAnimComponent : AssignableBinaryAnimComponent
{
	protected override void TriggerAudio()
	{
		PlaySound(Defines.Sounds.DialTurn);
	}
}
