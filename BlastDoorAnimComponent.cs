using UnityEngine;

public class BlastDoorAnimComponent : DoorAnimComponent
{
	public static readonly int BlastDoorOpenHash = Animator.StringToHash("BlastDoorOpen");

	public static readonly int BlastDoorCloseHash = Animator.StringToHash("BlastDoorClose");

	public static readonly int BlastDoorOpenNoPowerHash = Animator.StringToHash("BlastDoorOpenNoPower");

	public static readonly int BlastDoorCloseNoPowerHash = Animator.StringToHash("BlastDoorCloseNoPower");

	public override int OpenSound => BlastDoorOpenHash;

	public override int CloseSound => BlastDoorCloseHash;

	public override int OpenNoPowerSound => BlastDoorOpenNoPowerHash;

	public override int CloseNoPowerSound => BlastDoorCloseNoPowerHash;
}
