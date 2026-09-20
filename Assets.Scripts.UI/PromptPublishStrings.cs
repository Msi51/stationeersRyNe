using UnityEngine;

namespace Assets.Scripts.UI;

internal static class PromptPublishStrings
{
	private static readonly int TitleHash = Animator.StringToHash("PromptPublishTitle");

	private static readonly int BodyHash = Animator.StringToHash("PromptPublishBody");

	private static readonly int ButtonHash = Animator.StringToHash("PromptPublishConfirm");

	public static string Title => Localization.GetInterface(TitleHash);

	public static string Body => Localization.GetInterface(BodyHash);

	public static string Button => Localization.GetInterface(ButtonHash);
}
