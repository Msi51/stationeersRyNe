using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Drill : PowerTool
{
	public static readonly int EquipDrillHash = Animator.StringToHash("EquipDrill");

	public static readonly int UnEquipDrillHash = Animator.StringToHash("UnEquipDrill");

	[SerializeField]
	private Transform drillBit;

	private const float DEGREES_ROTATION_PER_SECOND = 360f;

	public override int EquipSoundHash => EquipDrillHash;

	public override int UnEquipSoundHash => UnEquipDrillHash;

	public override int FinishedConstructingSoundHash => Animator.StringToHash("ScrewdriverFinished");

	public override int FinishedDeconstructingSoundHash => Animator.StringToHash("ScrewdriverFinished");

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (!IsOccluded && Activate != 0)
		{
			drillBit.Rotate(Vector3.right, 360f * GameManager.DeltaTime);
		}
	}

	public override void OnPrimaryUseStart()
	{
		base.OnPrimaryUseStart();
		Thing.Interact(base.InteractActivate, 1);
	}

	public override void OnPrimaryUseEnd()
	{
		base.OnPrimaryUseEnd();
		Thing.Interact(base.InteractActivate, 0);
	}
}
