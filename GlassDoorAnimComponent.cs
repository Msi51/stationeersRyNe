using UnityEngine;

public class GlassDoorAnimComponent : DoorAnimComponent
{
	public static readonly int GlassDoorOpenHash = Animator.StringToHash("GlassDoorOpen");

	public static readonly int GlassDoorCloseHash = Animator.StringToHash("GlassDoorClose");

	public static readonly int GlassDoorOpenNoPowerHash = Animator.StringToHash("GlassDoorOpenNoPower");

	public static readonly int GlassDoorCloseNoPowerHash = Animator.StringToHash("GlassDoorCloseNoPower");

	public override int OpenSound => GlassDoorOpenHash;

	public override int CloseSound => GlassDoorCloseHash;

	public override int OpenNoPowerSound => GlassDoorOpenNoPowerHash;

	public override int CloseNoPowerSound => GlassDoorCloseNoPowerHash;
}
