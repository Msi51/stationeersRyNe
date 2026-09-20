using UnityEngine;

public class AirlockAnimComponent : DoorAnimComponent
{
	public static readonly int AirlockDoorOpenHash = Animator.StringToHash("AirlockDoorOpen");

	public static readonly int AirlockDoorCloseHash = Animator.StringToHash("AirlockDoorClose");

	public static readonly int AirlockDoorOpenNoPowerHash = Animator.StringToHash("AirlockDoorOpenNoPower");

	public static readonly int AirlockDoorCloseNoPowerHash = Animator.StringToHash("AirlockDoorCloseNoPower");

	public override int OpenSound => AirlockDoorOpenHash;

	public override int CloseSound => AirlockDoorCloseHash;

	public override int OpenNoPowerSound => AirlockDoorOpenNoPowerHash;

	public override int CloseNoPowerSound => AirlockDoorCloseNoPowerHash;
}
