using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Crowbar : ManualTool
{
	public float DoorForceOpenDuration = 1f;

	public static readonly int EquipCrowbarHash = Animator.StringToHash("EquipCrowbar");

	public static readonly int UnEquipCrowbarHash = Animator.StringToHash("UnEquipCrowbar");

	public override int EquipSoundHash => EquipCrowbarHash;

	public override int UnEquipSoundHash => UnEquipCrowbarHash;

	public override int FinishedDeconstructingSoundHash => Animator.StringToHash("CrowBarFinished");
}
