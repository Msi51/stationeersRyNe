using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Wrench : ManualTool
{
	public static readonly int EquipWrenchHash = Animator.StringToHash("EquipWrench");

	public static readonly int UnEquipWrenchHash = Animator.StringToHash("UnEquipWrench");

	public override int EquipSoundHash => EquipWrenchHash;

	public override int UnEquipSoundHash => UnEquipWrenchHash;

	public override int FinishedConstructingSoundHash => Animator.StringToHash("WrenchFinished");

	public override int FinishedDeconstructingSoundHash => Animator.StringToHash("WrenchFinished");
}
