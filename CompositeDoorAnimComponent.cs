using UnityEngine;

public class CompositeDoorAnimComponent : DoorAnimComponent
{
	public static readonly int CompositeDoorOpenHash = Animator.StringToHash("CompositeDoorOpen");

	public static readonly int CompositeDoorCloseHash = Animator.StringToHash("CompositeDoorClose");

	public static readonly int CompositeDoorOpenNoPowerHash = Animator.StringToHash("CompositeDoorOpenNoPower");

	public static readonly int CompositeDoorCloseNoPowerHash = Animator.StringToHash("CompositeDoorCloseNoPower");

	public override int OpenSound => CompositeDoorOpenHash;

	public override int CloseSound => CompositeDoorCloseHash;

	public override int OpenNoPowerSound => CompositeDoorOpenNoPowerHash;

	public override int CloseNoPowerSound => CompositeDoorCloseNoPowerHash;
}
