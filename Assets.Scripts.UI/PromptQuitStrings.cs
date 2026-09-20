using UnityEngine;

namespace Assets.Scripts.UI;

internal static class PromptQuitStrings
{
	private static readonly int TitleHash = Animator.StringToHash("PromptQuitTitle");

	private static readonly int BodyHash = Animator.StringToHash("PromptQuitBody");

	private static readonly int ButtonHash = Animator.StringToHash("PromptQuitConfirm");

	public static string Title => Localization.GetInterface(TitleHash);

	public static string Body => Localization.GetInterface(BodyHash);

	public static string Button => Localization.GetInterface(ButtonHash);
}
