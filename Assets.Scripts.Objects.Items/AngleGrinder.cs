using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class AngleGrinder : PowerTool, IConstructor
{
	public static readonly int EquipGrinderHash = Animator.StringToHash("EquipGrinder");

	public static readonly int UnEquipGrinderHash = Animator.StringToHash("UnEquipGrinder");

	public override int FinishedDeconstructingSoundHash => Animator.StringToHash("GrinderFinished");

	public override int EquipSoundHash => EquipGrinderHash;

	public override int UnEquipSoundHash => UnEquipGrinderHash;

	public override void OnPrimaryUseStart()
	{
		base.OnPrimaryUseStart();
		Thing.Interact(base.InteractActivate, 1);
	}

	public override void OnPrimaryUseEnd()
	{
		base.OnPrimaryUseEnd();
		PlayNetworkSound(Defines.Sounds.GrinderActiveOff);
		Thing.Interact(base.InteractActivate, 0);
	}
}
