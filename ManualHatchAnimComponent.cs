using UnityEngine;

public class ManualHatchAnimComponent : DoorAnimComponent
{
	public static readonly int ManualHatchOpenHash = Animator.StringToHash("ManualHatchOpen");

	public static readonly int ManualHatchCloseHash = Animator.StringToHash("ManualHatchClose");

	public override int OpenSound => ManualHatchOpenHash;

	public override int CloseSound => ManualHatchCloseHash;

	public override int OpenNoPowerSound => ManualHatchOpenHash;

	public override int CloseNoPowerSound => ManualHatchCloseHash;
}
