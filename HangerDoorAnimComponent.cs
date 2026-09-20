using UnityEngine;

public class HangerDoorAnimComponent : DoorAnimComponent
{
	public static readonly int HangerDoorOpenHash = Animator.StringToHash("HangerDoorOpen");

	public static readonly int HangerDoorCloseHash = Animator.StringToHash("HangerDoorClose");

	public override int OpenSound => HangerDoorOpenHash;

	public override int CloseSound => HangerDoorCloseHash;

	public override int OpenNoPowerSound => HangerDoorOpenHash;

	public override int CloseNoPowerSound => HangerDoorCloseHash;
}
