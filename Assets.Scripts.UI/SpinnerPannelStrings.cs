using UnityEngine;

namespace Assets.Scripts.UI;

internal static class SpinnerPannelStrings
{
	private static readonly int SavingHash = Animator.StringToHash("SpinnerPannelSaving");

	private static readonly int DoneHash = Animator.StringToHash("SpinnerPannelDone");

	public static string Saving => Localization.GetInterface(SavingHash);

	public static string Done => Localization.GetInterface(DoneHash);
}
