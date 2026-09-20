using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Screwdriver : ManualTool
{
	public static readonly int EquipScrewdriverHash = Animator.StringToHash("EquipScrewdriver");

	public static readonly int UnEquipScrewdriverHash = Animator.StringToHash("UnEquipScrewdriver");

	public override int EquipSoundHash => EquipScrewdriverHash;

	public override int UnEquipSoundHash => UnEquipScrewdriverHash;

	public override int FinishedConstructingSoundHash => Animator.StringToHash("ScrewdriverFinished");

	public override int FinishedDeconstructingSoundHash => Animator.StringToHash("ScrewdriverFinished");
}
