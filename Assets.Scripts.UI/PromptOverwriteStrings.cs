using UnityEngine;

namespace Assets.Scripts.UI;

internal static class PromptOverwriteStrings
{
	private static readonly int TitleHash = Animator.StringToHash("PromptOverwriteTitle");

	private static readonly int BodyHash = Animator.StringToHash("PromptOverwriteBody");

	private static readonly int ButtonHash = Animator.StringToHash("PromptOverwriteConfirm");

	public static string Title => Localization.GetInterface(TitleHash);

	public static string Body => Localization.GetInterface(BodyHash);

	public static string Button => Localization.GetInterface(ButtonHash);
}
