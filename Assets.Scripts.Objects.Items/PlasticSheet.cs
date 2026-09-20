using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class PlasticSheet : Stackable, IResource, IShowBuildStateTooltip
{
	public override int ConstructingSoundHash => Animator.StringToHash("PlasticSheetsUsing");

	public override int FinishedConstructingSoundHash => Animator.StringToHash("PlasticSheetsDone");

	public override bool UseDefaultUiUsingSounds()
	{
		return false;
	}
}
