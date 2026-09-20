using UnityEngine;

namespace Assets.Scripts.UI;

internal static class PromptDeleteStrings
{
	private static readonly int TitleHash = Animator.StringToHash("PromptDeleteTitle");

	private static readonly int BodyHash = Animator.StringToHash("PromptDeleteBody");

	private static readonly int ButtonHash = Animator.StringToHash("PromptDeleteConfirm");

	public static string Title => Localization.GetInterface(TitleHash);

	public static string Body => Localization.GetInterface(BodyHash);

	public static string Button => Localization.GetInterface(ButtonHash);
}
