using UnityEngine;

namespace Assets.Scripts.UI;

internal static class PromptUnsubscribeStrings
{
	private static readonly int TitleHash = Animator.StringToHash("PromptUnsubscribeTitle");

	private static readonly int BodyHash = Animator.StringToHash("PromptUnsubscribeBody");

	private static readonly int ButtonHash = Animator.StringToHash("PromptUnsubscribeConfirm");

	public static string Title => Localization.GetInterface(TitleHash);

	public static string Body => Localization.GetInterface(BodyHash);

	public static string Button => Localization.GetInterface(ButtonHash);
}
