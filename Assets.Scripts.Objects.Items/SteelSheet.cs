using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class SteelSheet : Stackable, IResource, IShowBuildStateTooltip
{
	public override int ConstructingSoundHash => Animator.StringToHash("SteelSheetsUsing");

	public override int FinishedConstructingSoundHash => Animator.StringToHash("SteelSheetsDone");

	public override bool UseDefaultUiUsingSounds()
	{
		return false;
	}
}
